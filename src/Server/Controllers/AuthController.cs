using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TalentManagement.Application.Interfaces;
using TalentManagement.Server.Services;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    CurrentUserService currentUser,
    IColaboradorRepository colaboradorRepo,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("es-microsoft")]
    public IActionResult EsMicrosoft()
    {
        return Ok(new { esMicrosoft = true });
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> GetPerfil()
    {
        try
        {
            var esMicrosoft = currentUser.EsMicrosoftUser();
            var email = currentUser.GetEmail();
            var colaborador = await colaboradorRepo.GetByEmailAsync(email);
            var esJefe = colaborador is not null &&
                         await colaboradorRepo.EsJefeDeAreaAsync(colaborador.Id);
            var puedeResolver = await currentUser.PuedeResolverSolicitudesAsync();

            // Nombre: usar el de la BD si existe, si no el del token
            var nombreMostrar = colaborador is not null
                ? $"{colaborador.Nombre} {colaborador.Apellido}"
                : User.FindFirst("name")?.Value
                  ?? User.FindFirst("preferred_username")?.Value
                  ?? email;

            return Ok(new
            {
                email,
                nombre = nombreMostrar,
                esColaborador = colaborador is not null,
                esJefe,
                colaboradorId = colaborador?.Id,
                areaId = colaborador?.AreaId,
                esDevUser = !esMicrosoft,
                // Si no tiene colaborador en BD: mínimo privilegio (Colaborador), no Admin
                rol = colaborador?.Rol.ToString() ?? "Colaborador",
                puedeResolverSolicitudes = puedeResolver,
            });
        }
        catch (Exception ex)
        {
            var email = "Desconocido";
            try { email = currentUser.GetEmail(); } catch { }
            logger.LogError(ex, "Error al obtener perfil del usuario {Email}", email);
            return Ok(new
            {
                email = "",
                nombre = "Usuario",
                esColaborador = false,
                esJefe = false,
                colaboradorId = (int?)null,
                areaId = (int?)null,
                esDevUser = false,
                rol = "Colaborador",
                puedeResolverSolicitudes = false
            });
        }
    }
}
