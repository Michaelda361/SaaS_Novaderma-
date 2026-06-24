using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TalentManagement.Application.Services;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class DocumentosController(DocumentoService service, CurrentUserService currentUser) : ControllerBase
{
    // ── Listado ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? tipo,
        [FromQuery] string? estado,
        [FromQuery] int? areaId,
        [FromQuery] string? busqueda)
    {
        var esAdmin = await currentUser.PuedeGestionarPlantillasAsync();
        var result = await service.GetAllAsync(tipo, estado, areaId, busqueda, esAdmin);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var esAdmin = await currentUser.PuedeGestionarPlantillasAsync();
        var result = await service.GetByIdAsync(id, esAdmin);
        return result is null ? NotFound() : Ok(result);
    }

    // ── CRUD Admin ───────────────────────────────────────────────────────────

    [EnableRateLimiting("upload")]
    [HttpPost]
    [RequestSizeLimit(10485760)] // 10MB
    public async Task<IActionResult> Create([FromForm] CreateDocumentoDto dto,
        IFormFile? archivo)
    {
        try
        {
            Stream? stream = null;
            string? nombre = null;
            if (archivo is not null && archivo.Length > 0)
            {
                if (!ValidarArchivo(archivo, out var error))
                    return BadRequest(error);

                stream = archivo.OpenReadStream();
                nombre = archivo.FileName;
            }
            var result = await service.CreateAsync(dto, stream, nombre);
            stream?.Dispose();
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPut("{id:int}/metadatos")]
    public async Task<IActionResult> UpdateMetadatos(int id, [FromBody] UpdateDocumentoDto dto)
    {
        try
        {
            var result = await service.UpdateMetadatosAsync(id, dto);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return UnprocessableEntity(ex.Message); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [EnableRateLimiting("upload")]
    [HttpPost("{id:int}/version")]
    [RequestSizeLimit(10485760)] // 10MB
    public async Task<IActionResult> SubirVersion(int id, IFormFile archivo,
        [FromQuery] bool incrementoMayor = false)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest("El archivo es obligatorio");

        if (!ValidarArchivo(archivo, out var error))
            return BadRequest(error);

        using var stream = archivo.OpenReadStream();
        var result = await service.SubirNuevaVersionAsync(id, stream, archivo.FileName, incrementoMayor);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:int}/avanzar-estado")]
    public async Task<IActionResult> AvanzarEstado(int id)
    {
        try
        {
            if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();

            int colaboradorId = 0;
            string colaboradorNombre = "Sistema";
            try
            {
                var col = await GetColaboradorActualAsync();
                colaboradorId = col.Id;
                colaboradorNombre = $"{col.Nombre} {col.Apellido}";
            }
            catch { }
            var result = await service.AvanzarEstadoAsync(id, colaboradorId, colaboradorNombre);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return UnprocessableEntity(ex.Message); }
    }

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> ObtenerUrlEdicion(int id)
    {
        var url = await service.ObtenerUrlEdicionAsync(id);
        return url is null ? NotFound() : Ok(url);
    }

    [HttpGet("{id:int}/auditoria")]
    public async Task<IActionResult> GetAuditLog(int id)
    {
        var logs = await service.GetAuditLogAsync(id);
        return Ok(logs);
    }

    // ── Propuestas ───────────────────────────────────────────────────────────

    [EnableRateLimiting("upload")]
    [HttpPost("{id:int}/propuestas")]
    [RequestSizeLimit(10485760)] // 10MB
    public async Task<IActionResult> CrearPropuesta(int id,
        [FromForm] CreatePropuestaDto dto, IFormFile? archivo)
    {
        try
        {
            var email = GetEmail();
            Stream? stream = null;
            string? nombre = null;
            if (archivo is not null && archivo.Length > 0)
            {
                if (!ValidarArchivo(archivo, out var error))
                    return BadRequest(error);

                stream = archivo.OpenReadStream();
                nombre = archivo.FileName;
            }

            var result = await service.CrearPropuestaAsync(id, dto, stream, nombre, email);
            stream?.Dispose();
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return UnprocessableEntity(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
    }

    [HttpGet("propuestas/pendientes")]
    public async Task<IActionResult> GetPropuestasPendientes()
    {
        var email = GetEmail();
        var result = await service.GetPropuestasPendientesAsync(email);
        return Ok(result);
    }

    [HttpGet("propuestas/pendientes/count")]
    public async Task<IActionResult> CountPropuestasPendientes()
    {
        try
        {
            var email = GetEmail();
            var count = await service.CountPropuestasPendientesAsync(email);
            return Ok(count);
        }
        catch { return Ok(0); }
    }

    [HttpPost("propuestas/{propuestaId:int}/aprobar")]
    public async Task<IActionResult> AprobarPropuesta(int propuestaId)
    {
        try
        {
            var email = GetEmail();
            await service.AprobarPropuestaAsync(propuestaId, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPost("propuestas/{propuestaId:int}/rechazar")]
    public async Task<IActionResult> RechazarPropuesta(int propuestaId,
        [FromBody] RechazarPropuestaDto dto)
    {
        try
        {
            var email = GetEmail();
            await service.RechazarPropuestaAsync(propuestaId, dto.MotivoRechazo, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetEmail() => currentUser.GetEmail();

    private async Task<Domain.Entities.Colaborador> GetColaboradorActualAsync() =>
        await service.ResolverColaboradorAsync(GetEmail());

    private bool ValidarArchivo(IFormFile archivo, out string error)
    {
        var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".pptx" };
        var allowedTypes = new[] 
        { 
            "application/pdf", 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        };

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext) || !allowedTypes.Contains(archivo.ContentType))
        {
            error = "Tipo de archivo no permitido. Solo se aceptan archivos .pdf, .docx, .xlsx y .pptx.";
            return false;
        }

        if (archivo.Length > 10 * 1024 * 1024) // 10MB
        {
            error = "El archivo supera el tamaño máximo permitido de 10MB.";
            return false;
        }

        error = "";
        return true;
    }
}
