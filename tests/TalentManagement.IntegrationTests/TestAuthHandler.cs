using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TalentManagement.IntegrationTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestAuth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue("X-Test-Email", out var emailValues))
        {
            var email = emailValues.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var claims = new[]
                {
                    new Claim("preferred_username", email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, email),
                };
                var identity = new ClaimsIdentity(claims, AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
