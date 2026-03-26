using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Infrastructure.Services;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class PlantillasDocumentoController(
    PlantillaDocumentoService service,
    PdfGeneratorService pdfGenerator,
    CurrentUserService currentUser) : ControllerBase
{
    // ── Admin: CRUD ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlantillaDocumentoDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePlantillaDocumentoDto dto)
    {
        var result = await service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await service.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }

    // ── Colaborador: disponibles para su área ─────────────────────────────────

    [HttpGet("disponibles")]
    public async Task<IActionResult> GetDisponibles()
    {
        try
        {
            var email = currentUser.GetEmail();
            var result = await service.GetDisponiblesParaColaboradorAsync(email);
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Ok(new List<object>()); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    // ── Generación de PDF ─────────────────────────────────────────────────────

    [HttpPost("{id:int}/generar")]
    public async Task<IActionResult> GenerarPdf(int id)
    {
        try
        {
            var email = currentUser.GetEmail();
            var (htmlResuelto, plantilla, _) = await service.ResolverPlantillaAsync(id, email);
            var pdfBytes = pdfGenerator.Generar(htmlResuelto, plantilla);
            var nombreArchivo = $"{plantilla.Nombre.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
    }

    // ── Historial del colaborador ─────────────────────────────────────────────

    [HttpGet("mis-solicitudes")]
    public async Task<IActionResult> GetMisSolicitudes()
    {
        try
        {
            var email = currentUser.GetEmail();
            var result = await service.GetSolicitudesAsync(email);
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Ok(new List<object>()); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }
}
