using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.Colaboradores;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ColaboradoresController(
    ColaboradorService service,
    CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await service.GetAllAsync());

    [HttpGet("inactivos")]
    public async Task<IActionResult> GetInactivos()
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        return Ok(await service.GetInactivosAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateColaboradorDto dto)
    {
        try
        {
            var created = await service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateColaboradorDto dto)
    {
        try
        {
            var result = await service.UpdateAsync(id, dto);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPatch("{id:int}/restaurar")]
    public async Task<IActionResult> Restaurar(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        return await service.RestaurarAsync(id) ? NoContent() : NotFound();
    }

    [HttpPut("{id:int}/rol")]
    public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolDto dto)
    {
        try
        {
            var result = await service.CambiarRolAsync(id, dto.Rol);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}
