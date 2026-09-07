using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SecureFix.Api;

namespace SecureFix.Tests;

public class AuthenticationConfiguratorTests
{
    [Fact]
    public void ResolveAuthMode_DefaultsToDemo_WhenNotConfigured()
    {
        var configuration = BuildConfiguration();
        var environment = BuildEnvironment(Environments.Development);

        var mode = AuthenticationConfigurator.ResolveAuthMode(configuration, environment);

        Assert.Equal("demo", mode);
    }

    [Fact]
    public void ResolveAuthMode_AllowsEntra_InAnyEnvironment()
    {
        var configuration = BuildConfiguration(("AUTH_MODE", "entra"));
        var environment = BuildEnvironment(Environments.Development);

        var mode = AuthenticationConfigurator.ResolveAuthMode(configuration, environment);

        Assert.Equal("entra", mode);
    }

    [Fact]
    public void ResolveAuthMode_ThrowsInProduction_WhenDemoModeRequested()
    {
        var configuration = BuildConfiguration(("AUTH_MODE", "demo"));
        var environment = BuildEnvironment(Environments.Production);

        Assert.Throws<InvalidOperationException>(
            () => AuthenticationConfigurator.ResolveAuthMode(configuration, environment));
    }

    [Fact]
    public void ResolveAuthMode_ThrowsInProduction_WhenAuthModeUnset()
    {
        var configuration = BuildConfiguration();
        var environment = BuildEnvironment(Environments.Production);

        Assert.Throws<InvalidOperationException>(
            () => AuthenticationConfigurator.ResolveAuthMode(configuration, environment));
    }

    [Fact]
    public void ResolveAuthMode_AllowsEntra_InProduction()
    {
        var configuration = BuildConfiguration(("AUTH_MODE", "entra"));
        var environment = BuildEnvironment(Environments.Production);

        var mode = AuthenticationConfigurator.ResolveAuthMode(configuration, environment);

        Assert.Equal("entra", mode);
    }

    [Fact]
    public void ResolveAuthMode_ThrowsForUnsupportedValue()
    {
        var configuration = BuildConfiguration(("AUTH_MODE", "azure-ad-b2c"));
        var environment = BuildEnvironment(Environments.Development);

        Assert.Throws<InvalidOperationException>(
            () => AuthenticationConfigurator.ResolveAuthMode(configuration, environment));
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
    }

    private static IHostEnvironment BuildEnvironment(string environmentName)
    {
        return new FakeHostEnvironment(environmentName);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "SecureFix.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
