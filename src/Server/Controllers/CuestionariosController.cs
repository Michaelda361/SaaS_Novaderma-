using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.Cuestionarios;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CuestionariosController(
    CuestionarioService service,
    CurrentUserService currentUser) : ControllerBase
{

    [HttpGet("capacitacion/{capacitacionId:int}")]
    public async Task<IActionResult> GetByCapacitacion(int capacitacionId)
    {
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
        var resultado = await service.ResponderAsync(dto);
        return Ok(resultado);
    }

    [HttpGet("{cuestionarioId:int}/resultado/{inscripcionId:int}")]
    public async Task<IActionResult> GetResultado(int cuestionarioId, int inscripcionId)
    {
        var resultado = await service.GetResultadoAsync(cuestionarioId, inscripcionId);
        return resultado is null ? NotFound() : Ok(resultado);
    }
}
