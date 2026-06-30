using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#if DEBUG
namespace TalentManagement.Server;

/// <summary>
/// Solo activo en Development. Autentica el request si:
/// - El header X-Dev-User tiene un email válido, O
/// - DevUserStore tiene un usuario activo (permite requests sin token desde el cliente Blazor)
/// </summary>
public class DevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TalentManagement.Server.Services.DevUserStore devStore)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Si hay un Bearer token real, ceder al esquema JWT — no interferir
        if (Request.Headers.TryGetValue("Authorization", out var authHeader) &&
            authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        // Prioridad 1: header explícito X-Dev-User
        if (Request.Headers.TryGetValue("X-Dev-User", out var emailValues))
        {
            var email = emailValues.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(email))
                return Task.FromResult(AuthenticateResult.Success(BuildTicket(email)));
        }

        // Prioridad 2: usuario activo en el store (cliente sin token MSAL)
        if (devStore.ActiveEmail is { Length: > 0 } storeEmail)
            return Task.FromResult(AuthenticateResult.Success(BuildTicket(storeEmail)));

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    private AuthenticationTicket BuildTicket(string email)
    {
        var claims = new[]
        {
            new Claim("preferred_username", email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email),
        };
        var identity  = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, Scheme.Name);
    }
}
#endif
