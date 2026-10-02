using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SecureFix.Api;

public class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string DefaultScheme = "SecureFixDemo";

    public DemoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid bearer token."));
        }

        var token = authorization["Bearer ".Length..].Trim();
        var developerToken = Environment.GetEnvironmentVariable("SECUREFIX_DEMO_TOKEN");
        var reviewerToken = Environment.GetEnvironmentVariable("SECUREFIX_DEMO_REVIEWER_TOKEN");
        var isDeveloper = !string.IsNullOrWhiteSpace(developerToken)
            && string.Equals(token, developerToken, StringComparison.Ordinal);
        var isReviewer = !string.IsNullOrWhiteSpace(reviewerToken)
            && string.Equals(token, reviewerToken, StringComparison.Ordinal);

        if ((!isDeveloper && !isReviewer) || (isDeveloper && isReviewer))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid or misconfigured demo credentials."));
        }

        var userId = isReviewer ? "demo-security-reviewer" : "demo-developer";
        var role = isReviewer ? "SecurityReviewer" : "Developer";

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
