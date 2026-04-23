using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.Inscripciones;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class InscripcionesController(
    InscripcionService service,
    CurrentUserService currentUser) : ControllerBase
{
    [HttpGet("capacitacion/{capacitacionId:int}")]
    public async Task<IActionResult> GetByCapacitacion(int capacitacionId)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
            return StatusCode(403, new { message = "No tienes permiso para ver los inscritos." });
        return Ok(await service.GetByCapacitacionAsync(capacitacionId));
    }

    [HttpGet("colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> GetByColaborador(int colaboradorId)
    {
        // Jefe/Admin pueden ver inscripciones de cualquier colaborador
        if (await currentUser.PuedeGestionarPlantillasAsync())
            return Ok(await service.GetByColaboradorAsync(colaboradorId));

        // Colaborador solo puede ver sus propias inscripciones
        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Forbid();
        if (miId != colaboradorId) return Forbid();

        return Ok(await service.GetByColaboradorAsync(colaboradorId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInscripcionDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var (result, error) = await service.CreateAsync(dto);
        if (error is not null) return Conflict(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInscripcionDto dto)
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
}
