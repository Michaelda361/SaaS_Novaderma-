using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace TalentManagement.Server.Services;

/// <summary>
/// Resuelve el email del usuario actual.
/// En Development, si DevSettings:ImpersonateEmail está configurado, lo usa en vez del token.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration config, IWebHostEnvironment env)
{
    public string GetEmail()
    {
        // Override de dev — permite probar distintos roles sin cambiar de cuenta
        if (env.IsDevelopment())
        {
            var impersonate = config["DevSettings:ImpersonateEmail"];
            if (!string.IsNullOrWhiteSpace(impersonate))
                return impersonate;
        }

        var user = httpContextAccessor.HttpContext?.User;
        return user?.FindFirstValue("preferred_username")
            ?? user?.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("No se pudo obtener el email del token");
    }
}
