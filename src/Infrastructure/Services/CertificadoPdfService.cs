using TalentManagement.Application.Interfaces;

namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Genera el PDF del certificado desde una plantilla DOCX o PPTX.
/// </summary>
public class CertificadoPdfService(PdfGeneratorService pdfGenerator) : ICertificadoPdfService
{
    private const string MimeDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string MimePptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    private const string MimePdf = "application/pdf";

    public byte[] GenerarPdf(byte[] archivoBytes, Dictionary<string, string> variables,
        string mimeType = MimeDocx,
        byte[]? firmaBytes = null, double? firmaX = null, double? firmaY = null, double? firmaAncho = null, double? firmaAlto = null)
    {
        if (mimeType == MimePptx)
            return pdfGenerator.GenerarPdfDesdePptx(archivoBytes, variables, firmaBytes, firmaX, firmaY, firmaAncho, firmaAlto);

        if (mimeType == MimePdf)
            return pdfGenerator.GenerarPdfDesdePdf(archivoBytes, variables);

        // DOCX: flujo existente
        var payload = new { DocxBase64 = Convert.ToBase64String(archivoBytes), Variables = variables };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return pdfGenerator.GenerarPdfDesdeDocx(json);
    }

    /// <summary>
    /// Devuelve el PPTX con las variables aplicadas (sin convertir).
    /// </summary>
    public byte[] GenerarPptxAplicado(byte[] archivoBytes, Dictionary<string, string> variables,
        byte[]? firmaBytes = null, double? firmaX = null, double? firmaY = null, double? firmaAncho = null, double? firmaAlto = null)
    {
        return pdfGenerator.GenerarPptxAplicado(archivoBytes, variables, firmaBytes, firmaX, firmaY, firmaAncho, firmaAlto);
    }

    public byte[]? GenerarPreviewCertificado(byte[] archivoBytes)
    {
        return pdfGenerator.GenerarPreviewCertificado(archivoBytes);
    }
}
