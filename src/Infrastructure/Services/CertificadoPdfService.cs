using TalentManagement.Application.Interfaces;

namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Implementacion que reutiliza PdfGeneratorService para generar el PDF del certificado.
/// </summary>
public class CertificadoPdfService(PdfGeneratorService pdfGenerator) : ICertificadoPdfService
{
    public byte[] GenerarPdf(byte[] docxBytes, Dictionary<string, string> variables)
    {
        // Construir el payload que espera PdfGeneratorService.GenerarPdfDesdeDocx
        var payload = new
        {
            DocxBase64 = Convert.ToBase64String(docxBytes),
            Variables = variables
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return pdfGenerator.GenerarPdfDesdeDocx(json);
    }
}
