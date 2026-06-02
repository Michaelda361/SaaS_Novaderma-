using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TalentManagement.Application.Services;
using TalentManagement.Server.Hubs;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.Cuestionarios;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CuestionariosController(
    CuestionarioService service,
    InscripcionService inscripcionService,
    CurrentUserService currentUser,
    IHubContext<NotificacionesHub> hub) : ControllerBase
{
    [HttpGet("capacitacion/{capacitacionId:int}")]
    public async Task<IActionResult> GetByCapacitacion(int capacitacionId)
    {
        if (await currentUser.EsAdminAsync())
        {
            var r = await service.GetByCapacitacionAsync(capacitacionId);
            return r is null ? NotFound() : Ok(r);
        }

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Forbid();
        if (!await inscripcionService.ExisteInscripcionAsync(capacitacionId, miId.Value)) return Forbid();

        var result = await service.GetByCapacitacionAsync(capacitacionId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCuestionarioDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetByCapacitacion),
            new { capacitacionId = created.CapacitacionId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCuestionarioDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var result = await service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("responder")]
    public async Task<IActionResult> Responder([FromBody] ResponderCuestionarioDto dto)
    {
        try
        {
            // Validar que la inscripción pertenece al colaborador autenticado
            var miColaboradorId = await currentUser.GetColaboradorIdAsync();
            if (miColaboradorId is null)
                return Forbid();

            var inscripcion = await inscripcionService.GetByIdAsync(dto.InscripcionId);
            if (inscripcion is null)
                return NotFound(new { message = "Inscripción no encontrada." });

            if (inscripcion.ColaboradorId != miColaboradorId)
                return Forbid();

            var resultado = await service.ResponderAsync(dto);

            // Construir las notificaciones dentro del scope del request (service es scoped)
            var email = currentUser.GetEmail();
            var notif = await service.BuildNotificacionAsync(dto.CuestionarioId, email, resultado);

            CertificadoEmitidoDto? certNotif = null;
            if (resultado.CertificadoEmitido && !string.IsNullOrWhiteSpace(resultado.NombreCertificado))
            {
                certNotif = new CertificadoEmitidoDto
                {
                    NombreCertificado = resultado.NombreCertificado,
                    CapacitacionNombre = notif?.CapacitacionNombre ?? "",
                    CapacitacionId = notif?.CapacitacionId ?? 0,
                    Puntaje = resultado.Puntaje,
                    FechaEmision = DateTime.Today
                };
            }

            // Enviar via SignalR en background — IHubContext es singleton, es seguro usarlo fuera del scope
            var colaboradorConnId = NotificacionesHub.GetConnectionId(miColaboradorId.Value);
            _ = Task.Run(async () =>
            {
                if (notif is not null)
                    await hub.Clients.Group("admins").SendAsync("CuestionarioRespondido", notif);

                if (certNotif is not null && colaboradorConnId is not null)
                    await hub.Clients.Client(colaboradorConnId).SendAsync("CertificadoEmitido", certNotif);
            });

            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno al procesar el cuestionario.", detail = ex.Message });
        }
    }

    [HttpGet("{cuestionarioId:int}/resultado/{inscripcionId:int}")]
    public async Task<IActionResult> GetResultado(int cuestionarioId, int inscripcionId)
    {
        var resultado = await service.GetResultadoAsync(cuestionarioId, inscripcionId);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    /// <summary>
    /// Devuelve los IDs de capacitaciones aprobadas por el colaborador en una sola query.
    /// Reemplaza el N+1 de CargarAprobadas en Capacitaciones.razor.
    /// </summary>
    [HttpGet("capacitaciones-aprobadas/{colaboradorId:int}")]
    public async Task<IActionResult> GetCapacitacionesAprobadas(int colaboradorId)
    {
        var miId = await currentUser.GetColaboradorIdAsync();
        if (!await currentUser.PuedeGestionarPlantillasAsync() && miId != colaboradorId)
            return Forbid();

        var ids = await service.GetCapacitacionesAprobadasAsync(colaboradorId);
        return Ok(new TalentManagement.Shared.DTOs.Cuestionarios.CapacitacionesAprobadasDto
        {
            CapacitacionIds = ids
        });
    }
}
