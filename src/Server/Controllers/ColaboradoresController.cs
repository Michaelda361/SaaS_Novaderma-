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
    ColaboradorCampoService campoService,
    CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await service.GetAllAsync();
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            foreach (var c in list) c.SueldoBasico = null;
        }
        return Ok(list);
    }

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
        if (result is null) return NotFound();

        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            var miEmail = currentUser.GetEmail();
            if (!string.Equals(result.Email, miEmail, StringComparison.OrdinalIgnoreCase))
            {
                result.SueldoBasico = null;
            }
        }
        return Ok(result);
    }

    [HttpGet("campos")]
    public async Task<IActionResult> GetCampos() =>
        Ok(await campoService.GetAllAsync());

    [HttpPost("campos")]
    public async Task<IActionResult> CreateCampo([FromBody] CreateColaboradorCampoDto dto)
    {
        if (!await currentUser.EsAdminAsync()) return Forbid();
        return Ok(await campoService.CreateAsync(dto));
    }

    [HttpPut("campos/{id:int}")]
    public async Task<IActionResult> UpdateCampo(int id, [FromBody] UpdateColaboradorCampoDto dto)
    {
        if (!await currentUser.EsAdminAsync()) return Forbid();
        var result = await campoService.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("campos/{id:int}")]
    public async Task<IActionResult> DeleteCampo(int id)
    {
        if (!await currentUser.EsAdminAsync()) return Forbid();
        await campoService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateColaboradorDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
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
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
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

    [HttpPut("{id:int}/rol")]
    public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolDto dto)
    {
        if (!await currentUser.EsAdminAsync()) return Forbid();
        try
        {
            var result = await service.CambiarRolAsync(id, dto.Rol);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}
