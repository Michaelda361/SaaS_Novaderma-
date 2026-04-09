using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TalentManagement.Application.Services;
using TalentManagement.Infrastructure.Services;
using TalentManagement.Server.Hubs;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class PlantillasDocumentoController(
    PlantillaDocumentoService service,
    PdfGeneratorService pdfGenerator,
    DocxToHtmlConverterService docxConverter,
    IHubContext<NotificacionesHub> hub,
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

    // ── Previsualización (HTML resuelto, sin registrar solicitud) ────────────

    [HttpPost("{id:int}/previsualizar")]
    public async Task<IActionResult> Previsualizar(int id, [FromBody] GenerarPdfDto? dto = null)
    {
        try
        {
            var email = currentUser.GetEmail();
            var (htmlResuelto, plantilla, valoresPerfil) = await service.PrevisualizarAsync(id, email, dto?.Extras);
            return Ok(new
            {
                html = htmlResuelto,
                nombreFirmante = plantilla.NombreFirmante,
                cargoFirmante = plantilla.CargoFirmante,
                firmaImagenBase64 = plantilla.FirmaImagenBase64,
                tipoPlantilla = plantilla.TipoPlantilla == TalentManagement.Domain.Enums.TipoPlantilla.Docx ? "docx" : "html",
                valoresPerfil,
            });
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    // ── Edición libre: HTML editable con variables aplicadas ─────────────────

    [HttpPost("{id:int}/editar")]
    public async Task<IActionResult> ObtenerHtmlEditable(int id, [FromBody] GenerarPdfDto? dto = null)
    {
        try
        {
            var email = currentUser.GetEmail();
            var (htmlResuelto, plantilla, _) = await service.PrevisualizarAsync(id, email, dto?.Extras);

            string htmlEditable;
            if (plantilla.TipoPlantilla == TalentManagement.Domain.Enums.TipoPlantilla.Docx)
            {
                var docxBytes = pdfGenerator.GenerarDocx(htmlResuelto);
                htmlEditable = docxConverter.ConvertirAHtml(docxBytes);
            }
            else
            {
                htmlEditable = htmlResuelto;
            }

            return Ok(new
            {
                html = htmlEditable,
                nombreFirmante = plantilla.NombreFirmante,
                cargoFirmante = plantilla.CargoFirmante,
                firmaImagenBase64 = plantilla.FirmaImagenBase64,
            });
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    // ── Generar PDF desde HTML editado libremente por el colaborador ──────────

    [HttpPost("{id:int}/generar-desde-html")]
    public async Task<IActionResult> GenerarDesdeHtmlEditado(int id, [FromBody] GenerarDesdeHtmlDto dto)
    {
        try
        {
            var email = currentUser.GetEmail();
            var plantilla = await service.GetByIdAsync(id);
            if (plantilla is null) return NotFound();

            await service.RegistrarSolicitudAsync(id, email);

            var entidad = new TalentManagement.Domain.Entities.PlantillaDocumento
            {
                NombreFirmante = dto.NombreFirmante,
                CargoFirmante = dto.CargoFirmante,
                FirmaImagenBase64 = dto.FirmaImagenBase64,
            };
            var pdfBytes = pdfGenerator.GenerarPdfDesdeHtml(dto.Html, entidad);
            var nombre = $"{plantilla.Nombre.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombre);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    // ── Editor amigable DOCX: campos editables con texto resuelto ────────────

    [HttpGet("{id:int}/campos-editables")]
    public async Task<IActionResult> GetCamposEditables(int id)
    {
        try
        {
            var email = currentUser.GetEmail();
            var (docxBytes, plantilla, variables) = await service.ObtenerDocxConVariablesAsync(id, email);
            var parrafos = pdfGenerator.ExtraerParrafosEditables(docxBytes, variables);

            return Ok(new EditorDocxDto
            {
                NombreFirmante = plantilla.NombreFirmante,
                CargoFirmante = plantilla.CargoFirmante,
                FirmaImagenBase64 = plantilla.FirmaImagenBase64,
                Parrafos = parrafos.Select(p => new ParrafoEditableDto
                {
                    Indice = p.indice,
                    TextoResuelto = p.textoResuelto,
                    Contexto = p.contexto,
                }).ToList(),
            });
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    // ── Generar PDF con párrafos editados (fidelidad total del DOCX original) ─

    [HttpPost("{id:int}/generar-con-edicion")]
    public async Task<IActionResult> GenerarConEdicion(int id, [FromBody] GenerarConEdicionDto dto)
    {
        try
        {
            var email = currentUser.GetEmail();
            var (docxBytes, plantilla, variables) = await service.ObtenerDocxConVariablesAsync(id, email);

            // Aplicar variables automáticas al DOCX
            var payload = System.Text.Json.JsonSerializer.Serialize(new DocxReemplazoPayload
            {
                DocxBase64 = Convert.ToBase64String(docxBytes),
                Variables = variables,
            });
            var docxConVariables = pdfGenerator.GenerarDocx(payload);

            // Aplicar los párrafos editados por el colaborador encima
            var docxFinal = dto.ParrafosEditados.Count > 0
                ? pdfGenerator.AplicarEdicionEnDocx(docxConVariables, dto.ParrafosEditados)
                : docxConVariables;

            // Convertir a PDF con LibreOffice — fidelidad total del documento original
            var payloadFinal = System.Text.Json.JsonSerializer.Serialize(new DocxReemplazoPayload
            {
                DocxBase64 = Convert.ToBase64String(docxFinal),
                Variables = [],
            });
            var pdfBytes = pdfGenerator.GenerarPdfDesdeDocx(payloadFinal);

            await service.RegistrarSolicitudAsync(id, email);

            var nombre = $"{plantilla.Nombre.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombre);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    // ── Previsualización DOCX → PDF (sin registrar solicitud) ────────────────

    [HttpPost("{id:int}/previsualizar-docx")]
    public async Task<IActionResult> PrevisualizarDocx(int id, [FromBody] GenerarPdfDto? dto = null)
    {
        try
        {
            var email = currentUser.GetEmail();
            var (htmlResuelto, plantilla, _) = await service.PrevisualizarAsync(id, email, dto?.Extras);
            if (plantilla.TipoPlantilla != TalentManagement.Domain.Enums.TipoPlantilla.Docx)
                return BadRequest("Esta plantilla no es de tipo DOCX.");
            var pdfBytes = pdfGenerator.GenerarPdfDesdeDocx(htmlResuelto);
            return File(pdfBytes, "application/pdf");
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    // ── Generación de documento ───────────────────────────────────────────────

    [HttpPost("{id:int}/generar")]
    public async Task<IActionResult> GenerarDocumento(int id, [FromBody] GenerarPdfDto? dto = null)
    {
        try
        {
            var email = currentUser.GetEmail();
            var extras = dto?.Extras;
            var (htmlResuelto, plantilla, _) = await service.ResolverPlantillaAsync(id, email, extras);

            if (plantilla.TipoPlantilla == TalentManagement.Domain.Enums.TipoPlantilla.Docx)
            {
                var docxBytes = pdfGenerator.GenerarDocx(htmlResuelto);
                var nombreDocx = $"{plantilla.Nombre.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}.docx";
                return File(docxBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    nombreDocx);
            }
            else
            {
                var pdfBytes = pdfGenerator.GenerarPdfDesdeHtml(htmlResuelto, plantilla);
                var nombrePdf = $"{plantilla.Nombre.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", nombrePdf);
            }
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
    }

    // ── Flujo de solicitud y aprobación ──────────────────────────────────────

    [HttpPost("{id:int}/solicitar")]
    public async Task<IActionResult> Solicitar(int id, [FromBody] EnviarSolicitudDto dto)
    {
        try
        {
            var email = currentUser.GetEmail();
            var (docxBytes, plantilla, variables) = await service.ObtenerDocxConVariablesAsync(id, email);

            var payload = System.Text.Json.JsonSerializer.Serialize(new DocxReemplazoPayload
            {
                DocxBase64 = Convert.ToBase64String(docxBytes),
                Variables = variables,
            });
            var docxConVariables = pdfGenerator.GenerarDocx(payload);
            var docxFinal = dto.ParrafosEditados.Count > 0
                ? pdfGenerator.AplicarEdicionEnDocx(docxConVariables, dto.ParrafosEditados)
                : docxConVariables;
            var payloadFinal = System.Text.Json.JsonSerializer.Serialize(new DocxReemplazoPayload
            {
                DocxBase64 = Convert.ToBase64String(docxFinal),
                Variables = [],
            });
            var pdfBytes = pdfGenerator.GenerarPdfDesdeDocx(payloadFinal);

            var solicitud = await service.EnviarSolicitudAsync(id, email, pdfBytes);
            await hub.Clients.Group("admins").SendAsync("NuevaSolicitud", solicitud);
            return Ok(solicitud);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("solicitudes/{solicitudId:int}/aprobar")]
    public async Task<IActionResult> Aprobar(int solicitudId, [FromBody] ResolverSolicitudDto dto)
    {
        try
        {
            var resultado = await service.AprobarSolicitudAsync(solicitudId, dto.Comentario);
            if (resultado is null) return NotFound();
            await hub.Clients.Group($"user:{resultado.ColaboradorEmail}")
                .SendAsync("SolicitudResuelta", resultado);
            return Ok(resultado);
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("solicitudes/{solicitudId:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int solicitudId, [FromBody] ResolverSolicitudDto dto)
    {
        try
        {
            var resultado = await service.RechazarSolicitudAsync(solicitudId, dto.Comentario);
            if (resultado is null) return NotFound();
            await hub.Clients.Group($"user:{resultado.ColaboradorEmail}")
                .SendAsync("SolicitudResuelta", resultado);
            return Ok(resultado);
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpGet("solicitudes/{solicitudId:int}/descargar")]
    public async Task<IActionResult> DescargarAprobada(int solicitudId)
    {
        try
        {
            var email = currentUser.GetEmail();
            var pdf = await service.DescargarSolicitudAprobadaAsync(solicitudId, email);
            if (pdf is null) return NotFound();
            return File(pdf, "application/pdf", $"carta_{solicitudId}.pdf");
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpGet("solicitudes/{solicitudId:int}/pdf")]
    public async Task<IActionResult> VerPdfSolicitud(int solicitudId)
    {
        try
        {
            var solicitud = await service.GetSolicitudEntityAsync(solicitudId);
            if (solicitud?.PdfBytes is null) return NotFound();
            return File(solicitud.PdfBytes, "application/pdf");
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

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

    [HttpGet("solicitudes")]
    public async Task<IActionResult> GetTodasSolicitudes()
    {
        var result = await service.GetTodasSolicitudesAsync();
        return Ok(result);
    }

    [HttpGet("solicitudes/pendientes")]
    public async Task<IActionResult> GetPendientes()
    {
        var result = await service.GetPendientesAsync();
        return Ok(result);
    }

    [HttpGet("solicitudes/count-pendientes")]
    public async Task<IActionResult> CountPendientes() =>
        Ok(new { count = await service.CountPendientesAsync() });
}
