using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Interfaces;
using TalentManagement.Application.Services;
using TalentManagement.Shared.DTOs.Certificados;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CertificadosController(
    CertificadoService service,
    TalentManagement.Server.Services.CurrentUserService currentUser,
    ICertificatePdfGenerator certificatePdfGenerator,
    ICertificadoPdfService certificadoPdfService) : ControllerBase
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

        await service.RegistrarEventoAsync(id, "Download", $"PDF descargado por {currentUser.GetEmail()}");
        return File(pdf, "application/pdf", $"certificado_{id}.pdf");
    }

    /// <summary>
    /// Devuelve el PPTX con las variables aplicadas para que el usuario lo abra en PowerPoint
    /// y exporte a PDF con máxima fidelidad.
    /// </summary>
    [HttpGet("{id:int}/pptx")]
    public async Task<IActionResult> DescargarPptxAplicado(int id,
        [FromServices] TalentManagement.Application.Interfaces.ICertificadoPdfService pdfService,
        [FromServices] TalentManagement.Application.Interfaces.ICapacitacionRepository capRepo,
        [FromServices] TalentManagement.Application.Interfaces.IColaboradorRepository colRepo)
    {
        try
        {
            var cert = await service.GetCertificadoEntityAsync(id);
            if (cert is null) return NotFound();
            if (!cert.CapacitacionId.HasValue)
                return BadRequest(new { message = "El certificado no tiene capacitacion asociada." });

            var cap = await capRepo.GetByIdAsync(cert.CapacitacionId.Value);
            if (cap?.ArchivoDocxCertificado is not { Length: > 0 })
                return BadRequest(new { message = "La capacitacion no tiene plantilla DOCX/PPTX configurada." });

            var col = await colRepo.GetByIdAsync(cert.ColaboradorId);
            var vars = new Dictionary<string, string>
            {
                ["{{nombre_completo}}"] = col is not null ? $"{col.Nombre} {col.Apellido}" : "",
                ["{{cargo}}"]           = col?.Cargo?.Nombre ?? "",
                ["{{area}}"]            = col?.Area?.Nombre ?? "",
                ["{{capacitacion}}"]    = cap.Nombre,
                ["{{fecha_emision}}"]   = cert.FechaEmision.ToString("dd/MM/yyyy"),
                ["{{puntaje}}"]         = ""
            };

            var mimeType = cap.TipoArchivoCertificado
                ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            if (mimeType == "application/vnd.openxmlformats-officedocument.presentationml.presentation")
            {
                var pptx = pdfService.GenerarPptxAplicado(cap.ArchivoDocxCertificado, vars);
                return File(pptx, "application/vnd.openxmlformats-officedocument.presentationml.presentation", $"certificado_{id}.pptx");
            }

            if (mimeType == "application/pdf")
            {
                var pdf = pdfService.GenerarPdf(cap.ArchivoDocxCertificado, vars, mimeType);
                return File(pdf, "application/pdf", $"certificado_{id}.pdf");
            }

            return BadRequest(new { message = "La plantilla no es PPTX ni PDF." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al generar el PPTX aplicado.", detail = ex.Message });
        }
    }

    [HttpPost("{id:int}/regenerar-pdf")]
    public async Task<IActionResult> RegenerarPdf(
        int id,
        [FromServices] ICapacitacionRepository capRepo,
        [FromServices] IColaboradorRepository colRepo)
    {
        try
        {
            var cert = await service.GetCertificadoEntityAsync(id);
            if (cert is null) return NotFound();
            if (!cert.CapacitacionId.HasValue)
                return BadRequest(new { message = "El certificado no tiene capacitacion asociada." });

            var cap = await capRepo.GetByIdAsync(cert.CapacitacionId.Value);
            if (cap is null)
                return NotFound(new { message = "La capacitación no existe." });

            var col = await colRepo.GetByIdAsync(cert.ColaboradorId);
            var participantName = col is not null ? $"{col.Nombre} {col.Apellido}" : "N/A";
            var trainingName = cap.NombreCertificado ?? cap.PlantillaNombreCertificado ?? cap.Nombre;

            var colaborador = await colRepo.GetByIdAsync(cert.ColaboradorId);
            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{nombre_completo}}"] = participantName,
                ["{{cargo}}"] = colaborador?.Cargo?.Nombre ?? string.Empty,
                ["{{area}}"] = colaborador?.Area?.Nombre ?? string.Empty,
                ["{{capacitacion}}"] = trainingName,
                ["{{fecha_emision}}"] = cert.FechaEmision.ToString("dd/MM/yyyy"),
                ["{{puntaje}}"] = string.Empty
            };

            byte[] pdf;
            if (cap.ArchivoDocxCertificado is { Length: > 0 })
            {
                var mimeType = cap.TipoArchivoCertificado
                    ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

                // Usa la plantilla cargada para preservar el diseño del certificado.
                pdf = certificadoPdfService.GenerarPdf(cap.ArchivoDocxCertificado, variables, mimeType);
            }
            else
            {
                var data = new CertificatePdfDataDto
                {
                    ParticipantName = participantName,
                    TrainingName = trainingName,
                    IssuedDate = cert.FechaEmision,
                    DurationHours = cap.DuracionHoras,
                    CertificateCode = cert.CertificateCode ?? $"C-{id}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                };

                pdf = certificatePdfGenerator.Generate(data);
            }

            await service.ActualizarPdfAsync(id, pdf, currentUser.GetEmail());
            await service.RegistrarEventoAsync(id, "Generation", $"PDF regenerado por {currentUser.GetEmail()}");
            return Ok(new { message = "PDF regenerado correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al regenerar el PDF.", detail = ex.Message });
        }
    }
}
