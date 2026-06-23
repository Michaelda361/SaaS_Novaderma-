using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TalentManagement.Application.Interfaces;

namespace TalentManagement.Server.Services;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IColaboradorRepository colaboradorRepo,
    ILogger<CurrentUserService> logger)
{
    public string GetEmail()
    {
        var ctx = httpContextAccessor.HttpContext;
        var user = ctx?.User;
        return user?.FindFirstValue("preferred_username")
            ?? user?.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("No se pudo obtener el email del usuario");
    }

    /// <summary>True si el usuario se autenticó con Bearer JWT de Azure.</summary>
    public bool EsMicrosoftUser()
    {
        var ctx = httpContextAccessor.HttpContext;
        return ctx?.User.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// True si puede aprobar/rechazar solicitudes: rol Jefe o Admin en BD.
    /// </summary>
    public async Task<bool> PuedeResolverSolicitudesAsync()
    {
        try
        {
            var email = GetEmail();
            var colaborador = await colaboradorRepo.GetByEmailAsync(email);
            if (colaborador is null) return false;
            return colaborador.Rol == Domain.Enums.RolUsuario.Jefe
                || colaborador.Rol == Domain.Enums.RolUsuario.Admin;
        }
        catch (Exception ex) { logger.LogWarning(ex, "Error al verificar permisos de resolucion para usuario"); return false; }
    }

    /// <summary>
    /// True si puede crear/editar/eliminar plantillas y gestionar capacitaciones:
    /// rol Jefe o Admin en BD.
    /// </summary>
    public async Task<bool> PuedeGestionarPlantillasAsync()
    {
        try
        {
            var email = GetEmail();
            var colaborador = await colaboradorRepo.GetByEmailAsync(email);
            return colaborador?.Rol == Domain.Enums.RolUsuario.Jefe
                || colaborador?.Rol == Domain.Enums.RolUsuario.Admin;
        }
        catch (Exception ex) { logger.LogWarning(ex, "Error al verificar permisos de gestion para usuario"); return false; }
    }

    /// <summary>True si el usuario tiene rol Admin en BD.</summary>
    public async Task<bool> EsAdminAsync()
    {
        try
        {
            var email = GetEmail();
            var colaborador = await colaboradorRepo.GetByEmailAsync(email);
            return colaborador?.Rol == Domain.Enums.RolUsuario.Admin;
        }
        catch (Exception ex) { logger.LogWarning(ex, "Error al verificar rol Admin para usuario"); return false; }
    }

    /// <summary>
    /// Devuelve el Id del colaborador autenticado, o null si no tiene registro en BD.
    /// </summary>
    public async Task<int?> GetColaboradorIdAsync()
    {
        try
        {
            var email = GetEmail();
            var colaborador = await colaboradorRepo.GetByEmailAsync(email);
            return colaborador?.Id;
        }
        catch (Exception ex) { logger.LogWarning(ex, "Error al obtener ColaboradorId para usuario"); return null; }
    }
}
