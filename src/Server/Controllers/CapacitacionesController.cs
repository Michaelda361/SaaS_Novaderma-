using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentManagement.Application.Interfaces;
using TalentManagement.Application.Services;
using TalentManagement.Server.Hubs;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.Capacitaciones;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CapacitacionesController(
    CapacitacionService service,
    InscripcionService inscripcionService,
    CurrentUserService currentUser,
    IHubContext<NotificacionesHub> hub,
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<CapacitacionesController> logger) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (await currentUser.PuedeGestionarPlantillasAsync())
            return Ok(await service.GetActivasAsync());

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Ok(new List<TalentManagement.Shared.DTOs.Capacitaciones.CapacitacionDto>());

        // Obtener las inscripciones del colaborador y devolver las capacitaciones asociadas
        var inscripciones = await inscripcionService.GetByColaboradorAsync(miId.Value);
        var lista = new List<CapacitacionDto>();
        foreach (var inscripcion in inscripciones)
        {
            var cap = await service.GetByIdAsync(inscripcion.CapacitacionId);
            if (cap is not null && !lista.Any(c => c.Id == cap.Id))
                lista.Add(cap);
        }
        return Ok(lista);
    }

    [HttpGet("finalizadas")]
    public async Task<IActionResult> GetFinalizadas()
    {
        if (!await currentUser.EsAdminAsync()) return Forbid();
        return Ok(await service.GetFinalizadasAsync());
    }

    [HttpGet("area/{areaId:int}")]
    public async Task<IActionResult> GetByArea(int areaId)
    {
        if (await currentUser.PuedeGestionarPlantillasAsync())
            return Ok(await service.GetByAreaAsync(areaId));

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Ok(new List<TalentManagement.Shared.DTOs.Capacitaciones.CapacitacionDto>());

        var inscripciones = await inscripcionService.GetByColaboradorAsync(miId.Value);
        var lista = new List<CapacitacionDto>();
        foreach (var inscripcion in inscripciones)
        {
            var cap = await service.GetByIdAsync(inscripcion.CapacitacionId);
            if (cap is not null && cap.AreaId == areaId && !lista.Any(c => c.Id == cap.Id))
                lista.Add(cap);
        }
        return Ok(lista);
    }

    [HttpGet("colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> GetByColaborador(int colaboradorId)
    {
        var miId = await currentUser.GetColaboradorIdAsync();
        if (!await currentUser.EsAdminAsync() && miId != colaboradorId) return Forbid();

        // Devolver las capacitaciones en las que el colaborador está inscrito
        var inscripciones = await inscripcionService.GetByColaboradorAsync(colaboradorId);
        var lista = new List<CapacitacionDto>();
        foreach (var inscripcion in inscripciones)
        {
            var cap = await service.GetByIdAsync(inscripcion.CapacitacionId);
            if (cap is not null && !lista.Any(c => c.Id == cap.Id))
                lista.Add(cap);
        }
        return Ok(lista);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (await currentUser.PuedeGestionarPlantillasAsync())
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Forbid();

        if (!await inscripcionService.ExisteInscripcionAsync(id, miId.Value))
            return Forbid();

        var result2 = await service.GetByIdAsync(id);
        return result2 is null ? NotFound() : Ok(result2);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCapacitacionDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        try
        {
            var created = await service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCapacitacionDto dto)
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

    [HttpPatch("{id:int}/restaurar")]
    public async Task<IActionResult> Restaurar(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        return await service.RestaurarAsync(id) ? NoContent() : NotFound();
    }

    [HttpPatch("{id:int}/publicar")]
    public async Task<IActionResult> Publicar(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var result = await service.PublicarAsync(id);
        if (result is null) return NotFound();

        // Notificar a todos los colaboradores inscritos
        var inscritos = await inscripcionService.GetByCapacitacionAsync(id);
        var notif = new CapacitacionPublicadaDto
        {
            CapacitacionId = result.Id,
            CapacitacionNombre = result.Nombre,
            Descripcion = result.Descripcion,
        };
        foreach (var inscripcion in inscritos)
        {
            var connId = NotificacionesHub.GetConnectionId(inscripcion.ColaboradorId);
            if (connId is not null)
            {
                try
                {
                    await hub.Clients.Client(connId).SendAsync("CapacitacionPublicada", notif);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error al enviar notificación de capacitación publicada al colaborador {ColId}.", inscripcion.ColaboradorId);
                }
            }
        }

        // Enviar correos de notificación en segundo plano de forma no bloqueante y tolerante a fallos
        var listaEmails = inscritos.Select(i => new { i.ColaboradorEmail, i.ColaboradorNombre }).ToList();
        var clientBaseUrl = config["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var bgEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var bgLogger = scope.ServiceProvider.GetRequiredService<ILogger<CapacitacionesController>>();

                foreach (var inscrito in listaEmails)
                {
                    if (string.IsNullOrWhiteSpace(inscrito.ColaboradorEmail)) continue;

                    try
                    {
                        var link = $"{clientBaseUrl.TrimEnd('/')}/capacitaciones/{result.Id}";
                        var subject = $"Nueva Capacitación Disponible: {result.Nombre}";
                        var body = BuildEmailBody(
                            inscrito.ColaboradorNombre,
                            result.Nombre,
                            result.Descripcion,
                            result.DuracionHoras,
                            result.FechaInicio,
                            result.FechaFin,
                            link);

                        await bgEmailService.SendEmailAsync(inscrito.ColaboradorEmail, subject, body, isHtml: true);
                    }
                    catch (Exception ex)
                    {
                        bgLogger.LogError(ex, "Error al enviar correo de notificación al colaborador {Email} para la capacitación {CapId}.", inscrito.ColaboradorEmail, result.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error crítico en el proceso de segundo plano para el envío de correos de la capacitación {CapId}.", result.Id);
            }
        });

        return Ok(result);
    }

    private static string BuildEmailBody(
        string colaboradorNombre,
        string capacitacionNombre,
        string descripcion,
        int duracionHoras,
        DateTime fechaInicio,
        DateTime fechaFin,
        string link)
    {
        var desc = string.IsNullOrWhiteSpace(descripcion) ? "Sin descripción disponible." : descripcion;
        var anoActual = DateTime.Today.Year;
        
        return $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1), 0 2px 4px -1px rgba(0,0,0,0.06); background-color: #ffffff;"">
    <div style=""background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 30px; text-align: center;"">
        <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: 600; letter-spacing: -0.025em;"">NovaHub</h1>
        <p style=""color: #94a3b8; margin: 5px 0 0 0; font-size: 14px;"">Plataforma de Gestión de Talento</p>
    </div>
    
    <div style=""padding: 30px; color: #334155; line-height: 1.6;"">
        <h2 style=""color: #0f172a; margin-top: 0; font-size: 20px; font-weight: 600;"">¡Hola, {colaboradorNombre}!</h2>
        <p style=""font-size: 15px; margin-bottom: 24px;"">Te informamos que has sido inscrito en una nueva capacitación que ya se encuentra disponible para ti. A continuación encontrarás los detalles de la misma:</p>
        
        <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; margin-bottom: 24px;"">
            <h3 style=""color: #0f172a; margin-top: 0; margin-bottom: 10px; font-size: 17px; font-weight: 600;"">{capacitacionNombre}</h3>
            <p style=""margin: 0 0 15px 0; font-size: 14px; color: #475569;"">{desc}</p>
            
            <table style=""width: 100%; border-collapse: collapse; font-size: 14px;"">
                <tr>
                    <td style=""padding: 6px 0; color: #64748b; width: 40%;""><strong>Duración:</strong></td>
                    <td style=""padding: 6px 0; color: #0f172a;"">{duracionHoras} horas</td>
                </tr>
                <tr>
                    <td style=""padding: 6px 0; color: #64748b;""><strong>Fecha de Inicio:</strong></td>
                    <td style=""padding: 6px 0; color: #0f172a;"">{fechaInicio:dd/MM/yyyy}</td>
                </tr>
                <tr>
                    <td style=""padding: 6px 0; color: #64748b;""><strong>Fecha Límite:</strong></td>
                    <td style=""padding: 6px 0; font-weight: 600; color: #dc2626;"">{fechaFin:dd/MM/yyyy}</td>
                </tr>
            </table>
        </div>
        
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{link}"" style=""background-color: #2563eb; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; display: inline-block; font-size: 15px; box-shadow: 0 2px 4px rgba(37, 99, 235, 0.2);"">Acceder a la capacitación</a>
        </div>
        
        <p style=""font-size: 13px; color: #64748b; margin-top: 24px; border-top: 1px dashed #e2e8f0; padding-top: 15px; font-style: italic;"">
            * Recuerda que es indispensable revisar todo el material y recursos del curso antes de poder realizar el cuestionario evaluativo.
        </p>
    </div>
    
    <div style=""background-color: #f1f5f9; padding: 20px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0;"">
        Este es un correo automático de NovaHub. Por favor, no respondas a este mensaje.<br>
        &copy; {anoActual} Novaderma. Todos los derechos reservados.
    </div>
</div>";
    }

    [HttpPatch("{id:int}/despublicar")]
    public async Task<IActionResult> Despublicar(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var result = await service.DespublicarAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Configura el certificado de una capacitación existente sin editar todos sus campos.</summary>
    [HttpPatch("{id:int}/certificado")]
    public async Task<IActionResult> ConfigurarCertificado(int id, [FromBody] ConfigurarCertificadoDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var result = await service.ConfigurarCertificadoAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }
}
