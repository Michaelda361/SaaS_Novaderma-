using TalentManagement.Application.Interfaces;

namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Genera el PDF del certificado desde una plantilla DOCX o PPTX.
/// </summary>
public class CertificadoPdfService(PdfGeneratorService pdfGenerator) : ICertificadoPdfService
{
    private const string MimeDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string MimePptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

    public byte[] GenerarPdf(byte[] archivoBytes, Dictionary<string, string> variables,
        string mimeType = MimeDocx)
    {
        if (mimeType == MimePptx)
            return pdfGenerator.GenerarPdfDesdePptx(archivoBytes, variables);

        // DOCX: flujo existente
        var payload = new { DocxBase64 = Convert.ToBase64String(archivoBytes), Variables = variables };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return pdfGenerator.GenerarPdfDesdeDocx(json);
    }
}