using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Shared.DTOs.Certificados;

namespace TalentManagement.Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CertificadosController(CertificadoService service) : ControllerBase
{
    [HttpGet("colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> GetByColaborador(int colaboradorId) =>
        Ok(await service.GetByColaboradorAsync(colaboradorId));

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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
