using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace TalentManagement.Server.Services;

/// <summary>
/// Resuelve el email del usuario actual.
/// En Development, si DevUserStore tiene un email activo, lo usa en vez del token.
/// </summary>
public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment env,
    DevUserStore? devStore = null)
{
    public string GetEmail()
    {
        if (env.IsDevelopment() && devStore?.ActiveEmail is { Length: > 0 } devEmail)
            return devEmail;

        var user = httpContextAccessor.HttpContext?.User;
        return user?.FindFirstValue("preferred_username")
            ?? user?.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("No se pudo obtener el email del token");
    }
}
