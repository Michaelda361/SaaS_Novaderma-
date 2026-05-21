using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Interfaces;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.ControlDocumental;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/control-documental")]
public class ControlDocumentalController(
    IControlDocumentalService service,
    CurrentUserService currentUser) : ControllerBase
{
    [HttpGet("listados-maestros")]
    public async Task<IActionResult> GetListadosMaestros()
    {
        var listados = await service.GetListadosAsync();
        return Ok(listados);
    }

    [HttpPost("listados-maestros")]
    public async Task<IActionResult> CreateListadoMaestro([FromBody] CreateListadoMaestroDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await service.CreateListadoAsync(dto);
        return CreatedAtAction(nameof(GetListadosMaestros), new { id = created.Id }, created);
    }

    [HttpGet("documentos")]
    public async Task<IActionResult> GetDocumentos(
        [FromQuery] int listadoMaestroId,
        [FromQuery] int? areaId,
        [FromQuery] string? busqueda,
        [FromQuery] string? codigo,
        [FromQuery] string? proceso,
        [FromQuery] string? estado)
    {
        var documentos = await service.GetDocumentosAsync(listadoMaestroId, areaId, busqueda, codigo, proceso, estado);
        return Ok(documentos);
    }

    [HttpGet("documentos/{id:int}")]
    public async Task<IActionResult> GetDocumento(int id)
    {
        var documento = await service.GetDocumentoAsync(id);
        return documento is null ? NotFound() : Ok(documento);
    }

    [HttpPost("documentos")]
    public async Task<IActionResult> CreateDocumento([FromBody] CreateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            var document = await service.CreateDocumentoAsync(dto, email);
            return CreatedAtAction(nameof(GetDocumento), new { id = document.Id }, document);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("documentos/{id:int}")]
    public async Task<IActionResult> UpdateDocumento(int id, [FromBody] UpdateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = currentUser.GetEmail();
        var updated = await service.UpdateDocumentoAsync(id, dto, email);
        return updated is null ? NotFound() : NoContent();
    }

    [HttpGet("documentos/{id:int}/auditoria")]
    public async Task<IActionResult> GetHistorial(int id)
    {
        var historial = await service.GetHistorialAsync(id);
        return Ok(historial);
    }
}
