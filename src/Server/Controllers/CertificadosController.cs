using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentManagement.Application.Interfaces;
using TalentManagement.Application.Services;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
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
    public async Task<IActionResult> GetAll()
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        return Ok(await service.GetAllAsync());
    }

    [HttpGet("colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> GetByColaborador(int colaboradorId)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            var miId = await currentUser.GetColaboradorIdAsync();
            if (miId is null || miId.Value != colaboradorId) return Forbid();
        }
        return Ok(await service.GetByColaboradorAsync(colaboradorId));
    }

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
        if (result is null) return NotFound();
        if (await currentUser.PuedeGestionarPlantillasAsync()) return Ok(result);

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null || miId.Value != result.ColaboradorId) return Forbid();
        return Ok(result);
    }

    [HttpGet("vencidos")]
    public async Task<IActionResult> GetVencidos()
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        return Ok(await service.GetVencidosAsync());
    }

    [HttpGet("proximos-a-vencer")]
    public async Task<IActionResult> GetProximosAVencer([FromQuery] int dias = 30)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        return Ok(await service.GetProximosAVencerAsync(dias));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCertificadoDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        try
        {
            var created = await service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCertificadoDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        try
        {
            var result = await service.UpdateAsync(id, dto);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var certEntity = await service.GetCertificadoEntityAsync(id);
        if (certEntity is null) return NotFound();
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            var miId = await currentUser.GetColaboradorIdAsync();
            if (miId is null || miId.Value != certEntity.ColaboradorId) return Forbid();
        }

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
        [FromServices] TalentManagement.Application.Interfaces.IColaboradorRepository colRepo,
        [FromServices] TalentManagement.Application.Interfaces.IInscripcionRepository inscRepo,
        [FromServices] TalentManagement.Application.Interfaces.ICuestionarioRepository questRepo)
    {
        try
        {
            var cert = await service.GetCertificadoEntityAsync(id);
            if (cert is null) return NotFound();

            if (!await currentUser.PuedeGestionarPlantillasAsync())
            {
                var miId = await currentUser.GetColaboradorIdAsync();
                if (miId is null || miId.Value != cert.ColaboradorId) return Forbid();
            }
            if (!cert.CapacitacionId.HasValue)
                return BadRequest(new { message = "El certificado no tiene capacitacion asociada." });

            var cap = await capRepo.GetByIdAsync(cert.CapacitacionId.Value);
            if (cap?.ArchivoDocxCertificado is not { Length: > 0 })
                return BadRequest(new { message = "La capacitacion no tiene plantilla DOCX/PPTX configurada." });

            var col = await colRepo.GetByIdAsync(cert.ColaboradorId);
            string puntajeStr = string.Empty;
            try
            {
                var inscs = await inscRepo.GetByCapacitacionAsync(cert.CapacitacionId.Value);
                var insc = inscs.FirstOrDefault(i => i.ColaboradorId == cert.ColaboradorId);
                if (insc != null)
                {
                    var cuestionario = await questRepo.GetByCapacitacionAsync(cert.CapacitacionId.Value);
                    if (cuestionario != null)
                    {
                        var respuestas = await questRepo.GetRespuestasAsync(cuestionario.Id, insc.Id);
                        if (respuestas.Any())
                        {
                            var mejorRespuesta = respuestas.FirstOrDefault(r => r.Aprobado) 
                                ?? respuestas.OrderByDescending(r => r.FechaRespuesta).First();
                            puntajeStr = $"{mejorRespuesta.Puntaje:0.#}%";
                        }
                    }
                }
            }
            catch { }

            Dictionary<string, string?>? camposAdicionales = null;
            if (col is not null)
            {
                camposAdicionales = await colRepo.GetValoresPorColaboradorAsync(col.Id);
            }
            var vars = ConstruirVariablesCertificado(col, cap, cert.FechaEmision, puntajeStr, camposAdicionales);

            var mimeType = cap.TipoArchivoCertificado
                ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            if (mimeType == "application/vnd.openxmlformats-officedocument.presentationml.presentation")
            {
                var pptx = pdfService.GenerarPptxAplicado(
                    cap.ArchivoDocxCertificado,
                    vars,
                    cap.ArchivoFirmaCertificado,
                    cap.FirmaX,
                    cap.FirmaY,
                    cap.FirmaAncho,
                    cap.FirmaAlto);
                return File(pptx, "application/vnd.openxmlformats-officedocument.presentationml.presentation", $"certificado_{id}.pptx");
            }

            if (mimeType == "application/pdf")
            {
                var pdf = pdfService.GenerarPdf(
                    cap.ArchivoDocxCertificado,
                    vars,
                    mimeType,
                    cap.ArchivoFirmaCertificado,
                    cap.FirmaX,
                    cap.FirmaY,
                    cap.FirmaAncho,
                    cap.FirmaAlto);
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
        [FromServices] IColaboradorRepository colRepo,
        [FromServices] IInscripcionRepository inscRepo,
        [FromServices] ICuestionarioRepository questRepo)
    {
        try
        {
            var cert = await service.GetCertificadoEntityAsync(id);
            if (cert is null) return NotFound();

            if (!await currentUser.PuedeGestionarPlantillasAsync())
            {
                var miId = await currentUser.GetColaboradorIdAsync();
                if (miId is null || miId.Value != cert.ColaboradorId) return Forbid();
            }
            if (!cert.CapacitacionId.HasValue)
                return BadRequest(new { message = "El certificado no tiene capacitacion asociada." });

            var cap = await capRepo.GetByIdAsync(cert.CapacitacionId.Value);
            if (cap is null)
                return NotFound(new { message = "La capacitación no existe." });

            var col = await colRepo.GetByIdAsync(cert.ColaboradorId);
            string puntajeStr = string.Empty;
            try
            {
                var inscs = await inscRepo.GetByCapacitacionAsync(cert.CapacitacionId.Value);
                var insc = inscs.FirstOrDefault(i => i.ColaboradorId == cert.ColaboradorId);
                if (insc != null)
                {
                    var cuestionario = await questRepo.GetByCapacitacionAsync(cert.CapacitacionId.Value);
                    if (cuestionario != null)
                    {
                        var respuestas = await questRepo.GetRespuestasAsync(cuestionario.Id, insc.Id);
                        if (respuestas.Any())
                        {
                            var mejorRespuesta = respuestas.FirstOrDefault(r => r.Aprobado) 
                                ?? respuestas.OrderByDescending(r => r.FechaRespuesta).First();
                            puntajeStr = $"{mejorRespuesta.Puntaje:0.#}%";
                        }
                    }
                }
            }
            catch { }

            Dictionary<string, string?>? camposAdicionales = null;
            if (col is not null)
            {
                camposAdicionales = await colRepo.GetValoresPorColaboradorAsync(col.Id);
            }
            var variables = ConstruirVariablesCertificado(col, cap, cert.FechaEmision, puntajeStr, camposAdicionales);

            byte[] pdf;
            if (cap.ArchivoDocxCertificado is { Length: > 0 })
            {
                var mimeType = cap.TipoArchivoCertificado
                    ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

                // Usa la plantilla cargada para preservar el diseño del certificado.
                pdf = certificadoPdfService.GenerarPdf(
                    cap.ArchivoDocxCertificado,
                    variables,
                    mimeType,
                    cap.ArchivoFirmaCertificado,
                    cap.FirmaX,
                    cap.FirmaY,
                    cap.FirmaAncho,
                    cap.FirmaAlto);
            }
            else
            {
                string nombreCert = cap.Nombre;
                if (!string.IsNullOrWhiteSpace(cap.PlantillaNombreCertificado) && col is not null)
                {
                    var tempName = cap.PlantillaNombreCertificado;
                    foreach (var (k, v) in variables)
                    {
                        tempName = tempName.Replace(k, v, StringComparison.OrdinalIgnoreCase);
                    }
                    nombreCert = tempName;
                }
                else
                {
                    nombreCert = !string.IsNullOrWhiteSpace(cap.NombreCertificado)
                        ? cap.NombreCertificado
                        : cap.Nombre;
                }

                var data = new CertificatePdfDataDto
                {
                    ParticipantName = col is not null ? $"{col.Nombre} {col.Apellido}" : "N/A",
                    TrainingName = nombreCert,
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

    private static Dictionary<string, string> ConstruirVariablesCertificado(
        Colaborador? col, Capacitacion cap, DateTime fechaEmision, string puntajeStr, Dictionary<string, string?>? camposAdicionales = null)
    {
        var cultura = new System.Globalization.CultureInfo("es-CO");
        var hoy = DateTime.Today;
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["{{nombre_completo}}"]  = col != null ? $"{col.Nombre} {col.Apellido}" : string.Empty,
            ["{{nombre}}"]           = col?.Nombre ?? string.Empty,
            ["{{apellido}}"]         = col?.Apellido ?? string.Empty,
            ["{{email}}"]            = col?.Email ?? string.Empty,
            ["{{telefono}}"]         = col?.Telefono ?? string.Empty,
            ["{{cedula}}"]           = col?.Cedula ?? string.Empty,
            ["{{cargo}}"]            = col?.Cargo?.Nombre ?? string.Empty,
            ["{{area}}"]             = col?.Area?.Nombre ?? string.Empty,
            ["{{fecha_ingreso}}"]    = col != null ? col.FechaIngreso.ToString("d 'de' MMMM 'de' yyyy", cultura) : string.Empty,
            ["{{tipo_contrato}}"]    = col?.TipoContrato ?? string.Empty,
            ["{{sueldo_basico}}"]    = col?.SueldoBasico.HasValue == true ? col.SueldoBasico.Value.ToString("C0", cultura) : string.Empty,
            ["{{ciudad}}"]           = col?.Ciudad ?? string.Empty,
            ["{{fecha_emision}}"]    = fechaEmision.ToString("dd/MM/yyyy"),
            ["{{fecha_expedicion}}"] = fechaEmision.ToString("d 'de' MMMM 'de' yyyy", cultura),
            ["{{fecha_actual}}"]     = hoy.ToString("dd/MM/yyyy"),
            ["{{genero}}"]           = col != null ? (col.Genero switch
            {
                GeneroColaborador.Masculino => "el señor",
                GeneroColaborador.Femenino  => "la señora",
                _                           => "el(la) señor(a)",
            }) : "el(la) señor(a)",
            ["{{capacitacion}}"]     = cap.NombreCertificado ?? cap.PlantillaNombreCertificado ?? cap.Nombre,
            ["{{puntaje}}"]          = puntajeStr
        };

        if (camposAdicionales is { Count: > 0 })
        {
            foreach (var (key, value) in camposAdicionales)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    variables[$"{{{{{key}}}}}"] = value ?? string.Empty;
                }
            }
        }

        return variables;
    }
}
