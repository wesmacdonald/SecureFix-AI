using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace SecureFix.Api;

/// <summary>
/// Configures authentication for the API based on the AUTH_MODE setting.
///
/// - "entra" (recommended, required in Production): validates Microsoft Entra ID issued
///   JWTs via Microsoft.Identity.Web. App roles (Admin, SecurityReviewer, Developer, Viewer)
///   are read from the token's "roles" claim and mapped to ClaimTypes.Role automatically.
/// - "demo" (local/dev only): uses a static bearer token + X-User-Role header. This mode
///   is intentionally weak and must never be enabled in Production.
/// </summary>
public static class AuthenticationConfigurator
{
    public const string EntraMode = "entra";
    public const string DemoMode = "demo";

    public static string ResolveAuthMode(IConfiguration configuration, IHostEnvironment environment)
    {
        var mode = (configuration["AUTH_MODE"] ?? Environment.GetEnvironmentVariable("AUTH_MODE"))
            ?.Trim()
            .ToLowerInvariant();

        if (string.IsNullOrEmpty(mode))
        {
            mode = DemoMode;
        }

        if (mode != EntraMode && mode != DemoMode)
        {
            throw new InvalidOperationException(
                $"Invalid AUTH_MODE '{mode}'. Supported values are '{EntraMode}' or '{DemoMode}'.");
        }

        // Fail closed: the demo authentication handler must never protect a Production deployment.
        if (environment.IsProduction() && mode != EntraMode)
        {
            throw new InvalidOperationException(
                "AUTH_MODE must be set to 'entra' when ASPNETCORE_ENVIRONMENT is Production. " +
                "The demo authentication handler is not safe for production use.");
        }

        return mode;
    }

    public static void AddSecureFixAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var mode = ResolveAuthMode(configuration, environment);

        if (mode == EntraMode)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));
        }
        else
        {
            services
                .AddAuthentication(DemoAuthenticationHandler.DefaultScheme)
                .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(
                    DemoAuthenticationHandler.DefaultScheme, _ => { });
        }

        services.AddAuthorization();
    }
}
