namespace SecureFix.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using SecureFix.Core.Services;
using Xunit;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

#pragma warning disable OPENAI001

[AttributeUsage(AttributeTargets.Method)]
public sealed class FoundryIntegrationFactAttribute : FactAttribute
{
    public FoundryIntegrationFactAttribute()
    {
        DotEnvEnvironment.Load();

        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_FOUNDRY_INTEGRATION_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_FOUNDRY_INTEGRATION_TESTS=true in .env to run the Azure AI Foundry integration test.";
        }
    }
}

public class AzureAIFoundryEnvironmentIntegrationTests
{
    [FoundryIntegrationFact]
    [Trait("Category", "Integration")]
    [Trait("Provider", "AzureAIFoundry")]
    public async Task FoundryEnvironment_CheckUsingKey()
    {
        DotEnvEnvironment.Load();

        string deploymentName = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_MODEL_ID") ?? "gpt-4o";
        string endpoint = "https://securefix-ai-resource.services.ai.azure.com/openai/v1";
        string apiKey = RequireEnvironmentVariable("AZURE_AI_FOUNDRY_KEY");
        
        ResponsesClient client = new(
            credential: new ApiKeyCredential(apiKey),
            options: new ResponsesClientOptions()
            {
                Endpoint = new Uri(endpoint),
            });

        CreateResponseOptions options = new()
        {
            Model = deploymentName,
            InputItems =
            {
                ResponseItem.CreateUserMessageItem("What's the weather like today for my current location?"),
            },
        };


        ResponseResult response = await client.CreateResponseAsync(options);
        string outputText = response.GetOutputText();
        Console.WriteLine($"[ASSISTANT]: {outputText}");
        Assert.False(string.IsNullOrWhiteSpace(outputText));
    }

    [FoundryIntegrationFact]
    [Trait("Category", "Integration")]
    [Trait("Provider", "AzureAIFoundry")]
    public async Task FoundryEnvironment_IsValidAndProviderIsHealthy()
    {
        DotEnvEnvironment.Load();

        var providerName = RequireEnvironmentVariable("AI_PROVIDER");
        var endpoint = RequireEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT");
        _ = RequireEnvironmentVariable("AZURE_AI_FOUNDRY_KEY");
        _ = RequireEnvironmentVariable("AZURE_AI_FOUNDRY_MODEL_ID");

        Assert.Equal("azure-ai-foundry", providerName, ignoreCase: true);
        Assert.True(Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri));
        Assert.Equal(Uri.UriSchemeHttps, endpointUri!.Scheme);
        Assert.EndsWith(".services.ai.azure.com", endpointUri.Host, StringComparison.OrdinalIgnoreCase);

        var path = endpointUri.AbsolutePath.TrimEnd('/');
        Assert.True(
            path is "" or "/models" or "/openai" or "/openai/v1",
            $"Unexpected Azure AI Foundry endpoint path: '{path}'.");

        var provider = new AzureAIFoundryRecommendationProvider(
            NullLogger<AzureAIFoundryRecommendationProvider>.Instance);

        Assert.True(await provider.IsHealthyAsync());
    }

    private static string RequireEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.False(string.IsNullOrWhiteSpace(value), $"Required environment variable {name} is not configured.");
        return value!;
    }
}

internal static class DotEnvEnvironment
{
    private static readonly object SyncRoot = new();
    private static bool _loaded;

    public static void Load()
    {
        lock (SyncRoot)
        {
            if (_loaded)
            {
                return;
            }

            var path = FindDotEnvFile();
            if (path is not null)
            {
                foreach (var rawLine in File.ReadLines(path))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    {
                        continue;
                    }

                    if (line.StartsWith("export ", StringComparison.Ordinal))
                    {
                        line = line[7..].TrimStart();
                    }

                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    var name = line[..separatorIndex].Trim();
                    var value = TrimQuotes(line[(separatorIndex + 1)..].Trim());
                    if (IsValidVariableName(name) && Environment.GetEnvironmentVariable(name) is null)
                    {
                        Environment.SetEnvironmentVariable(name, value);
                    }
                }
            }

            _loaded = true;
        }
    }

    private static string? FindDotEnvFile()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string TrimQuotes(string value)
    {
        return value.Length >= 2 && value[0] == value[^1] && (value[0] == '\'' || value[0] == '"')
            ? value[1..^1]
            : value;
    }

    private static bool IsValidVariableName(string name)
    {
        return name.Length > 0
            && (char.IsLetter(name[0]) || name[0] == '_')
            && name.All(character => char.IsLetterOrDigit(character) || character == '_');
    }
}