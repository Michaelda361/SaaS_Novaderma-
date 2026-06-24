using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TalentManagement.Application.Interfaces;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.ControlDocumental;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/control-documental")]
public class ControlDocumentalController(
    IControlDocumentalService service,
    IHubContext<TalentManagement.Server.Hubs.NotificacionesHub> hub,
    CurrentUserService currentUser,
    IExcelImportService excelImportService,
    IExcelExportService excelExportService,
    ILogger<ControlDocumentalController> logger) : ControllerBase
{
    [HttpGet("listados-maestros")]
    public async Task<IActionResult> GetListadosMaestros()
    {
        var email = currentUser.GetEmail();
        var listados = await service.GetListadosParaUsuarioAsync(email);
        return Ok(listados);
    }

    [HttpGet("listados-maestros/{id:int}")]
    public async Task<IActionResult> GetListadoMaestro(int id)
    {
        try
        {
            var email = currentUser.GetEmail();
            var listado = await service.GetListadoAsync(id, email);
            return listado is null ? NotFound() : Ok(listado);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("listados-maestros/{id:int}/export")]
    public async Task<IActionResult> ExportListadoMaestro(int id)
    {
        try
        {
            var email = currentUser.GetEmail();
            var listado = await service.GetListadoAsync(id, email);
            if (listado is null)
            {
                return NotFound();
            }

            var documentos = await service.GetDocumentosAsync(id, null, null, null, null, null, email);
            var excelBytes = excelExportService.ExportListadoMaestro(listado, documentos);

            var fileName = SanitizeFileName(listado.Nombre) + ".xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [EnableRateLimiting("upload")]
    [HttpPost("listados-maestros/import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10485760)] // 10MB
    public async Task<IActionResult> ImportListadoMaestro()
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            return Forbid();
        }

        var file = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;
        if (file is null)
        {
            return BadRequest("No se encontró un archivo XLSX en la solicitud.");
        }

        if (file.Length == 0)
        {
            return BadRequest("El archivo XLSX está vacío.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" || file.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            return BadRequest("Tipo de archivo no permitido. Solo se aceptan archivos .xlsx.");
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB
        {
            return BadRequest("El archivo supera el tamaño máximo permitido de 10MB.");
        }

        try
        {
            using var stream = file.OpenReadStream();
            var dto = excelImportService.ParseListadoMaestro(stream, file.FileName);
            
            var email = currentUser.GetEmail();
            var created = await service.ImportListadoAsync(dto, email);
            
            return CreatedAtAction(nameof(GetListadoMaestro), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al importar listado maestro.");
            return BadRequest("Ocurrió un error al procesar el archivo Excel. Verifique el formato e intente nuevamente.");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalidChars.Contains(c))).Trim();
    }

    [HttpPost("listados-maestros")]
    public async Task<IActionResult> CreateListadoMaestro([FromBody] CreateListadoMaestroDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = currentUser.GetEmail();
        var created = await service.CreateListadoAsync(dto, email);
        return CreatedAtAction(nameof(GetListadosMaestros), new { id = created.Id }, created);
    }

    [HttpPut("listados-maestros/{id:int}")]
    public async Task<IActionResult> UpdateListadoMaestro(int id, [FromBody] CreateListadoMaestroDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = currentUser.GetEmail();
        var updated = await service.UpdateListadoAsync(id, dto, email);
        return updated is null ? NotFound() : NoContent();
    }

    [HttpDelete("listados-maestros/{id:int}")]
    public async Task<IActionResult> DeleteListadoMaestro(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            return Forbid();
        }

        try
        {
            var deleted = await service.DeleteListadoAsync(id, currentUser.GetEmail());
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
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
        try
        {
            var email = currentUser.GetEmail();
            var documentos = await service.GetDocumentosAsync(listadoMaestroId, areaId, busqueda, codigo, proceso, estado, email);
            return Ok(documentos);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("documentos/{id:int}")]
    public async Task<IActionResult> GetDocumento(int id)
    {
        try
        {
            var email = currentUser.GetEmail();
            var documento = await service.GetDocumentoAsync(id, email);
            return documento is null ? NotFound() : Ok(documento);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("documentos/{id:int}")]
    public async Task<IActionResult> UpdateDocumento(int id, [FromBody] UpdateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            var result = await service.UpdateDocumentoAsync(id, dto, email);
            
            if (result.Exito)
            {
                return NoContent();
            }

            if (result.RequiereSolicitud)
            {
                var created = await service.CreateSolicitudCambioAsync(id, dto, email);
                return CreatedAtAction(nameof(GetSolicitudesPorDocumento), new { documentoId = id }, created);
            }

            return BadRequest(result.MensajeError ?? "No se pudo actualizar el documento.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("documentos/{documentoId:int}/solicitudes")]
    public async Task<IActionResult> CreateSolicitudCambio(int documentoId, [FromBody] UpdateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            var created = await service.CreateSolicitudCambioAsync(documentoId, dto, email);

            // Notify admins in real-time about the new control-documental solicitud
            try
            {
                await hub.Clients.Group("admins").SendAsync("NuevaSolicitudCambio", created);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al enviar la notificación de nueva solicitud de cambio al grupo admins.");
            }

            return CreatedAtAction(nameof(GetSolicitudesPorDocumento), new { documentoId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("solicitudes/pendientes")]
    public async Task<IActionResult> GetSolicitudesPendientes()
    {
        var email = currentUser.GetEmail();
        var solicitudes = await service.GetSolicitudesCambioPendientesAsync(email);
        return Ok(solicitudes);
    }

    [HttpGet("solicitudes/pendientes/count")]
    public async Task<IActionResult> CountSolicitudesPendientes()
    {
        try
        {
            var email = currentUser.GetEmail();
            var count = await service.CountSolicitudesCambioPendientesAsync(email);
            return Ok(count);
        }
        catch
        {
            return Ok(0);
        }
    }

    [HttpPost("solicitudes/{solicitudId:int}/aprobar")]
    public async Task<IActionResult> AprobarSolicitudCambio(int solicitudId, [FromBody] AprobarSolicitudCambioDto? dto)
    {
        try
        {
            var email = currentUser.GetEmail();
            await service.AprobarSolicitudCambioAsync(solicitudId, dto?.Comentarios, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("solicitudes/{solicitudId:int}/rechazar")]
    public async Task<IActionResult> RechazarSolicitudCambio(int solicitudId, [FromBody] RechazarSolicitudCambioDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            await service.RechazarSolicitudCambioAsync(solicitudId, dto.MotivoRechazo, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("solicitudes/{solicitudId:int}/iniciar-revision")]
    public async Task<IActionResult> IniciarRevisionSolicitud(int solicitudId)
    {
        try
        {
            var email = currentUser.GetEmail();
            await service.IniciarRevisionSolicitudAsync(solicitudId, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("solicitudes/{solicitudId:int}/borrador")]
    public async Task<IActionResult> UpdateBorradorDocumento(int solicitudId, [FromBody] UpdateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            await service.UpdateBorradorDocumentoAsync(solicitudId, dto, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("solicitudes/{solicitudId:int}/enviar-aprobacion")]
    public async Task<IActionResult> EnviarAAprobacion(int solicitudId)
    {
        try
        {
            var email = currentUser.GetEmail();
            await service.EnviarAAprobacionAsync(solicitudId, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("documentos/{id:int}/auditoria")]
    public async Task<IActionResult> GetHistorial(int id)
    {
        try
        {
            var email = currentUser.GetEmail();
            var historial = await service.GetHistorialAsync(id, email);
            return Ok(historial);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("documentos/{documentoId:int}/solicitudes")]
    public async Task<IActionResult> GetSolicitudesPorDocumento(int documentoId)
    {
        var email = currentUser.GetEmail();
        var solicitudes = await service.GetSolicitudesCambioPorDocumentoAsync(documentoId, email);
        return Ok(solicitudes);
    }

    // ────── Endpoints de Permisos ──────

    [HttpGet("listados-maestros/{listadoId:int}/permisos")]
    public async Task<IActionResult> GetListadoPermisos(int listadoId)
    {
        try
        {
            var email = currentUser.GetEmail();
            var permisos = await service.GetListadoPermisosAsync(listadoId, email);
            return Ok(permisos);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("listados-maestros/{listadoId:int}/permisos/actual")]
    public async Task<IActionResult> GetListadoPermisosActual(int listadoId)
    {
        try
        {
            var email = currentUser.GetEmail();
            var permiso = await service.GetListadoPermisosActualUsuarioAsync(listadoId, email);
            return permiso is null ? NotFound() : Ok(permiso);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("listados-maestros/{listadoId:int}/permisos")]
    public async Task<IActionResult> UpdateListadoPermisos(int listadoId, [FromBody] List<ListadoMaestroPermisoUpdateDto>? permisos)
    {
        try
        {
            var email = currentUser.GetEmail();
            permisos = permisos ?? new List<ListadoMaestroPermisoUpdateDto>();
            await service.UpdateListadoPermisosAsync(listadoId, permisos, email);
            return Ok(new { mensaje = "Permisos actualizados correctamente." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
