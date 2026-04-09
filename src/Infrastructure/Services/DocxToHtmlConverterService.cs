namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Convierte DOCX a HTML editable usando Mammoth.
/// Preserva negrita, cursiva, subrayado, listas, tablas y alineación.
/// </summary>
public class DocxToHtmlConverterService
{
    public string ConvertirAHtml(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes);
        var converter = new Mammoth.DocumentConverter();
        var result = converter.ConvertToHtml(ms);
        return result.Value ?? string.Empty;
    }
}
