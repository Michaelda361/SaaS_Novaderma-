using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
    IHubContext<NotificacionesHub> hub) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await service.GetAllAsync());

    [HttpGet("area/{areaId:int}")]
    public async Task<IActionResult> GetByArea(int areaId) =>
        Ok(await service.GetByAreaAsync(areaId));

    [HttpGet("colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> GetByColaborador(int colaboradorId) =>
        Ok(await service.GetByColaboradorAsync(colaboradorId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCapacitacionDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCapacitacionDto dto)
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
                await hub.Clients.Client(connId).SendAsync("CapacitacionPublicada", notif);
        }

        return Ok(result);
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
