using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Shared.DTOs.Certificados;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CertificadosController(CertificadoService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await service.GetAllAsync());

    [HttpGet("colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> GetByColaborador(int colaboradorId) =>
        Ok(await service.GetByColaboradorAsync(colaboradorId));

    /// <summary>Devuelve los certificados del colaborador autenticado.</summary>
    [HttpGet("mis")]
    public async Task<IActionResult> GetMios(
        [FromServices] TalentManagement.Server.Services.CurrentUserService currentUser,
        [FromServices] TalentManagement.Application.Interfaces.IColaboradorRepository colaboradorRepo)
    {
        try
        {
            var email = currentUser.GetEmail();
            var colaborador = await colaboradorRepo.GetByEmailAsync(email);
            if (colaborador is null) return Ok(new List<CertificadoDto>());
            return Ok(await service.GetByColaboradorAsync(colaborador.Id));
        }
        catch { return Ok(new List<CertificadoDto>()); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("vencidos")]
    public async Task<IActionResult> GetVencidos() =>
        Ok(await service.GetVencidosAsync());

    [HttpGet("proximos-a-vencer")]
    public async Task<IActionResult> GetProximosAVencer([FromQuery] int dias = 30) =>
        Ok(await service.GetProximosAVencerAsync(dias));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCertificadoDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCertificadoDto dto)
    {
        var result = await service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var pdf = await service.GetPdfAsync(id);
        if (pdf is null || pdf.Length == 0) return NotFound();
        return File(pdf, "application/pdf", $"certificado_{id}.pdf");
    }
}
