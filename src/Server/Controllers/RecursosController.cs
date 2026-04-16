using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.Recursos;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class RecursosController(
    RecursoService service,
    CurrentUserService currentUser) : ControllerBase
{
    [HttpGet("capacitacion/{capacitacionId:int}")]
    public async Task<IActionResult> GetByCapacitacion(int capacitacionId) =>
        Ok(await service.GetByCapacitacionAsync(capacitacionId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecursoDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetByCapacitacion),
            new { capacitacionId = created.CapacitacionId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateRecursoDto dto)
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
