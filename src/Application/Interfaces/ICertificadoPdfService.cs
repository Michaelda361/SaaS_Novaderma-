namespace TalentManagement.Application.Interfaces;

/// <summary>
/// Genera el PDF de un certificado a partir de una plantilla DOCX o PPTX con variables resueltas.
/// </summary>
public interface ICertificadoPdfService
{
    /// <summary>
    /// Aplica las variables al archivo de plantilla (DOCX o PPTX) y genera un PDF.
    /// Variables soportadas: {{nombre_completo}}, {{cargo}}, {{area}},
    /// {{capacitacion}}, {{fecha_emision}}, {{puntaje}}.
    /// </summary>
    /// <param name="archivoBytes">Bytes del archivo DOCX o PPTX.</param>
    /// <param name="variables">Variables a reemplazar en el documento.</param>
    /// <param name="mimeType">MIME del archivo. Default DOCX para compatibilidad.</param>
    byte[] GenerarPdf(byte[] archivoBytes, Dictionary<string, string> variables,
        string mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

    /// <summary>
    /// Aplica las variables a un archivo PPTX y devuelve el PPTX resultante (no convertido).
    /// </summary>
    byte[] GenerarPptxAplicado(byte[] archivoBytes, Dictionary<string, string> variables);
}