using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TalentManagement.Server;

/// <summary>
/// Solo activo en Development. Si el request trae el header X-Dev-User con un email,
/// crea un ClaimsPrincipal autenticado con ese email — sin validar ningún token.
/// </summary>
public class DevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Dev-User", out var emailValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var email = emailValues.ToString().Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim("preferred_username", email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email),
        };
        var identity  = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
