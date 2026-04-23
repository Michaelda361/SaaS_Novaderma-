namespace TalentManagement.Application.Interfaces;

/// <summary>
/// Genera el PDF de un certificado a partir de una plantilla DOCX con variables resueltas.
/// </summary>
public interface ICertificadoPdfService
{
    /// <summary>
    /// Aplica las variables al DOCX y genera un PDF.
    /// Variables soportadas: {{nombre_completo}}, {{cargo}}, {{area}},
    /// {{capacitacion}}, {{fecha_emision}}, {{puntaje}}.
    /// </summary>
    byte[] GenerarPdf(byte[] docxBytes, Dictionary<string, string> variables);
}
