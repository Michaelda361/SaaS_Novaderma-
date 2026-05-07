using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Services;
using TalentManagement.Server.Services;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class AuditoriaController(
    AuditoriaService service,
    CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? entidadTipo,
        [FromQuery] string? accion,
        [FromQuery] int? colaboradorId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 25)
    {
        if (!await currentUser.EsAdminAsync()) return Forbid();

        var result = await service.GetPagedAsync(
            entidadTipo, accion, colaboradorId, desde, hasta, pagina, tamano);
        return Ok(result);
    }

    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar(
        [FromQuery] string? entidadTipo,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        if (!await currentUser.EsAdminAsync()) return Forbid();

        var bytes = await service.ExportarCsvAsync(entidadTipo, desde, hasta);
        return File(bytes, "text/csv", $"auditoria_{DateTime.Today:yyyyMMdd}.csv");
    }
}
