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
    CurrentUserService currentUser,
    TalentManagement.Application.Interfaces.IColaboradorRepository colaboradorRepo) : ControllerBase
{
    private bool EsMicrosoftUser =>
        !User.Identities.Any(i => i.AuthenticationType == "DevUser");

    // Devuelve true si el usuario actual es un colaborador sin rol de jefe
    private async Task<bool> EsSoloColaboradorAsync()
    {
        if (EsMicrosoftUser) return false; // Microsoft siempre puede ver todo
        var email = currentUser.GetEmail();
        var colaborador = await colaboradorRepo.GetByEmailAsync(email);
        if (colaborador is null) return false;
        return !await colaboradorRepo.EsJefeDeAreaAsync(colaborador.Id);
    }

    private IActionResult SoloMicrosoft() =>
        StatusCode(403, new { message = "Solo usuarios con cuenta Microsoft pueden realizar esta acción." });

    [HttpGet("capacitacion/{capacitacionId:int}")]
    public async Task<IActionResult> GetByCapacitacion(int capacitacionId)
    {
        if (await EsSoloColaboradorAsync())
            return StatusCode(403, new { message = "No tienes permiso para ver los inscritos." });
        return Ok(await service.GetByCapacitacionAsync(capacitacionId));
    }

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
    public async Task<IActionResult> Create([FromBody] CreateInscripcionDto dto)
    {
        if (!EsMicrosoftUser) return SoloMicrosoft();
        var (result, error) = await service.CreateAsync(dto);
        if (error is not null) return Conflict(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInscripcionDto dto)
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
