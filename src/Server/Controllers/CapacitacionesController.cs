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
    public async Task<IActionResult> GetAll()
    {
        if (await currentUser.EsAdminAsync())
            return Ok(await service.GetAllAsync());

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Ok(new List<TalentManagement.Shared.DTOs.Capacitaciones.CapacitacionDto>());

        // Obtener las inscripciones del colaborador y devolver las capacitaciones asociadas
        var inscripciones = await inscripcionService.GetByColaboradorAsync(miId.Value);
        var tareas = inscripciones.Select(i => service.GetByIdAsync(i.CapacitacionId));
        var caps = await Task.WhenAll(tareas);
        var lista = caps.Where(c => c is not null).GroupBy(c => c!.Id).Select(g => g.First()!).ToList();
        return Ok(lista);
    }

    [HttpGet("area/{areaId:int}")]
    public async Task<IActionResult> GetByArea(int areaId)
    {
        if (await currentUser.EsAdminAsync())
            return Ok(await service.GetByAreaAsync(areaId));

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Ok(new List<TalentManagement.Shared.DTOs.Capacitaciones.CapacitacionDto>());

        var inscripciones = await inscripcionService.GetByColaboradorAsync(miId.Value);
        var tareas = inscripciones.Select(i => service.GetByIdAsync(i.CapacitacionId));
        var caps = await Task.WhenAll(tareas);
        var lista = caps.Where(c => c is not null && c.AreaId == areaId).GroupBy(c => c!.Id).Select(g => g.First()!).ToList();
        return Ok(lista);
    }

    [HttpGet("colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> GetByColaborador(int colaboradorId)
    {
        var miId = await currentUser.GetColaboradorIdAsync();
        if (!await currentUser.EsAdminAsync() && miId != colaboradorId) return Forbid();

        // Devolver las capacitaciones en las que el colaborador está inscrito
        var inscripciones = await inscripcionService.GetByColaboradorAsync(colaboradorId);
        var tareas = inscripciones.Select(i => service.GetByIdAsync(i.CapacitacionId));
        var caps = await Task.WhenAll(tareas);
        var lista = caps.Where(c => c is not null).GroupBy(c => c!.Id).Select(g => g.First()!).ToList();
        return Ok(lista);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (await currentUser.EsAdminAsync())
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
