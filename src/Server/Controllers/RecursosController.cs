using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Shared.DTOs.Recursos;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class RecursosController(RecursoService service) : ControllerBase
{
    private bool EsMicrosoftUser =>
        !User.Identities.Any(i => i.AuthenticationType == "DevUser");

    private IActionResult SoloMicrosoft() =>
        StatusCode(403, new { message = "Solo usuarios con cuenta Microsoft pueden realizar esta acción." });

    [HttpGet("capacitacion/{capacitacionId:int}")]
    public async Task<IActionResult> GetByCapacitacion(int capacitacionId) =>
        Ok(await service.GetByCapacitacionAsync(capacitacionId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecursoDto dto)
    {
        if (!EsMicrosoftUser) return SoloMicrosoft();
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetByCapacitacion),
            new { capacitacionId = created.CapacitacionId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateRecursoDto dto)
    {
        if (!EsMicrosoftUser) return SoloMicrosoft();
        var result = await service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!EsMicrosoftUser) return SoloMicrosoft();
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
