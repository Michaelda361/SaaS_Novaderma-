using System.Security.Claims;
using TalentManagement.Application.Interfaces;

namespace TalentManagement.Server.Services;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment env,
    IColaboradorRepository colaboradorRepo,
    DevUserStore? devStore = null)
{
    public string GetEmail()
    {
        var ctx = httpContextAccessor.HttpContext;

        if (env.IsDevelopment() || devStore is not null)
        {
            if (ctx?.Request.Headers.TryGetValue("X-Dev-User", out var headerEmail) == true
                && !string.IsNullOrWhiteSpace(headerEmail.ToString()))
                return headerEmail.ToString().Trim();

            if (devStore?.ActiveEmail is { Length: > 0 } storeEmail)
                return storeEmail;
        }

        var user = ctx?.User;
        return user?.FindFirstValue("preferred_username")
            ?? user?.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("No se pudo obtener el email del usuario");
    }

    /// <summary>True si el usuario se autenticó con Bearer JWT de Azure (no dev user).</summary>
    public bool EsMicrosoftUser()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return false;
        if (ctx.Request.Headers.ContainsKey("X-Dev-User")) return false;
        if (ctx.Request.Headers.TryGetValue("Authorization", out var auth)
            && auth.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>
    /// True si puede aprobar/rechazar solicitudes: Microsoft (admin) O rol Jefe/Admin en BD.
    /// </summary>
    public async Task<bool> PuedeResolverSolicitudesAsync()
    {
        if (EsMicrosoftUser()) return true;
        try
        {
            var email = GetEmail();
            var colaborador = await colaboradorRepo.GetByEmailAsync(email);
            if (colaborador is null) return false;
            return colaborador.Rol == Domain.Enums.RolUsuario.Jefe
                || colaborador.Rol == Domain.Enums.RolUsuario.Admin;
        }
        catch { return false; }
    }
}
