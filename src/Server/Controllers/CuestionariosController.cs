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
    IHubContext<NotificacionesHub> hub,
    ILogger<CuestionariosController> logger) : ControllerBase
{
    [HttpGet("capacitacion/{capacitacionId:int}")]
    public async Task<IActionResult> GetByCapacitacion(int capacitacionId)
    {
        if (await currentUser.PuedeGestionarPlantillasAsync())
        {
            var r = await service.GetByCapacitacionAsync(capacitacionId);
            return r is null ? NoContent() : Ok(r);
        }

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Forbid();
        if (!await inscripcionService.ExisteInscripcionAsync(capacitacionId, miId.Value)) return Forbid();

        var result = await service.GetByCapacitacionAsync(capacitacionId);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCuestionarioDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        try
        {
            var created = await service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByCapacitacion),
                new { capacitacionId = created.CapacitacionId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCuestionarioDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        try
        {
            var result = await service.UpdateAsync(id, dto);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
            if (dto is null)
            {
                Console.WriteLine("[DEBUG Responder] Error: DTO nulo");
                return BadRequest(new { message = "Payload inválido." });
            }

            dto.Respuestas ??= new();

            Console.WriteLine($"[DEBUG Responder] Recibido DTO: CuestionarioId={dto.CuestionarioId}, InscripcionId={dto.InscripcionId}, Respuestas={dto.Respuestas.Count}");
            if (dto.CuestionarioId <= 0 || dto.InscripcionId <= 0)
            {
                Console.WriteLine($"[DEBUG Responder] Error: Identificadores inválidos. CuestionarioId={dto.CuestionarioId}, InscripcionId={dto.InscripcionId}");
                return BadRequest(new { message = "CuestionarioId e InscripcionId deben ser valores válidos." });
            }

            // Validar que la inscripción pertenece al colaborador autenticado
            var miColaboradorId = await currentUser.GetColaboradorIdAsync();
            if (miColaboradorId is null)
            {
                Console.WriteLine("[DEBUG Responder] Error: No se encontró colaboradorId autenticado");
                return Forbid();
            }

            Console.WriteLine($"[DEBUG Responder] ColaboradorId autenticado: {miColaboradorId}");

            var inscripcion = await inscripcionService.GetByIdAsync(dto.InscripcionId);
            Console.WriteLine($"[DEBUG Responder] Inscripción buscada con ID {dto.InscripcionId}: {(inscripcion is null ? "NO ENCONTRADA" : $"Encontrada, ColaboradorId={inscripcion.ColaboradorId}")}");
            if (inscripcion is null)
                return NotFound(new { message = "Inscripción no encontrada." });

            if (inscripcion.ColaboradorId != miColaboradorId)
            {
                Console.WriteLine($"[DEBUG Responder] Error: ColaboradorId de inscripción ({inscripcion.ColaboradorId}) no coincide con el autenticado ({miColaboradorId})");
                return Forbid();
            }

            var resultado = await service.ResponderAsync(dto);
            Console.WriteLine($"[DEBUG Responder] Resultado calculado: CuestionarioId={dto.CuestionarioId}, InscripcionId={dto.InscripcionId}, Puntaje={resultado.Puntaje:0.##}, Correctas={resultado.Correctas}/{resultado.TotalPreguntas}, Aprobado={resultado.Aprobado}, CertificadoEmitido={resultado.CertificadoEmitido}, NombreCertificado={resultado.NombreCertificado ?? "(ninguno)"}");

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
                try
                {
                    if (notif is not null)
                        await hub.Clients.Group("admins").SendAsync("CuestionarioRespondido", notif);

                    if (certNotif is not null && colaboradorConnId is not null)
                        await hub.Clients.Client(colaboradorConnId).SendAsync("CertificadoEmitido", certNotif);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error al enviar notificaciones en segundo plano tras responder cuestionario.");
                }
            });

            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[DEBUG Responder] InvalidOperationException: {ex.Message}");
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG Responder] Exception: {ex.GetType().Name}: {ex.Message}");
            return StatusCode(500, new { message = "Error interno al procesar el cuestionario.", detail = ex.Message });
        }
    }

    [HttpGet("{cuestionarioId:int}/resultado/{inscripcionId:int}")]
    public async Task<IActionResult> GetResultado(int cuestionarioId, int inscripcionId)
    {
        Console.WriteLine($"[DEBUG GetResultado] Buscando resultado para CuestionarioId={cuestionarioId}, InscripcionId={inscripcionId}");
        if (cuestionarioId <= 0 || inscripcionId <= 0)
        {
            Console.WriteLine($"[DEBUG GetResultado] Identificadores inválidos. CuestionarioId={cuestionarioId}, InscripcionId={inscripcionId}");
            return BadRequest(new { message = "CuestionarioId e InscripcionId deben ser valores válidos." });
        }

        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            var inscripcion = await inscripcionService.GetByIdAsync(inscripcionId);
            if (inscripcion is null) return NotFound();
            
            var miColaboradorId = await currentUser.GetColaboradorIdAsync();
            if (miColaboradorId is null || miColaboradorId != inscripcion.ColaboradorId)
                return Forbid();
        }

        var resultado = await service.GetResultadoAsync(cuestionarioId, inscripcionId);
        if (resultado is null)
        {
            Console.WriteLine($"[DEBUG GetResultado] Sin respuestas previas para CuestionarioId={cuestionarioId}, InscripcionId={inscripcionId}");
            return NotFound();
        }
        Console.WriteLine($"[DEBUG GetResultado] Resultado encontrado: Puntaje={resultado.Puntaje:0.##}, Aprobado={resultado.Aprobado}, Correctas={resultado.Correctas}/{resultado.TotalPreguntas}");
        return Ok(resultado);
    }

    /// <summary>
    /// Devuelve los IDs de capacitaciones completadas por el colaborador en una sola query.
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
