using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TalentManagement.Application.Services;
using TalentManagement.Domain.Entities;
using PdfDocument = QuestPDF.Fluent.Document;

namespace TalentManagement.Infrastructure.Services;

public class PdfGeneratorService
{
    public PdfGeneratorService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Genera un PDF a partir de la plantilla (HTML o DOCX) con variables ya reemplazadas.
    /// </summary>
    public byte[] Generar(string contenidoResuelto, PlantillaDocumento plantilla)
    {
        if (plantilla.TipoPlantilla == "docx")
        {
            // contenidoResuelto es un JSON DocxReemplazoPayload
            var payload = System.Text.Json.JsonSerializer.Deserialize<DocxReemplazoPayload>(contenidoResuelto)!;
            var docxBytes = Convert.FromBase64String(payload.DocxBase64);
            var docxModificado = AplicarVariablesEnDocx(docxBytes, payload.Variables);
            return GenerarDesdeDocx(docxModificado, plantilla);
        }
        return GenerarDesdeHtml(contenidoResuelto, plantilla);
    }

    // ── HTML → PDF ────────────────────────────────────────────────────────────

    private static byte[] GenerarDesdeHtml(string html, PlantillaDocumento plantilla)
    {
        return PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    RenderHtmlSimple(col, html);
                    col.Item().PaddingTop(40);
                    RenderFirma(col, plantilla);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ").FontSize(9).FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    t.Span(" de ").FontSize(9).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    // ── DOCX → extraer texto → PDF ────────────────────────────────────────────

    private static byte[] GenerarDesdeDocx(byte[] docxBytes, PlantillaDocumento plantilla)
    {
        // Extraer párrafos del docx con variables ya reemplazadas en el contenido
        var parrafos = ExtraerParrafosDeDocx(docxBytes);

        return PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    foreach (var (texto, negrita, centrado) in parrafos)
                    {
                        if (string.IsNullOrWhiteSpace(texto))
                        {
                            col.Item().PaddingTop(6);
                            continue;
                        }

                        col.Item().AlignLeft().Text(t =>
                        {
                            var span = t.Span(texto).FontSize(11);
                            if (negrita) span.Bold();
                        });
                    }

                    col.Item().PaddingTop(40);
                    RenderFirma(col, plantilla);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ").FontSize(9).FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    t.Span(" de ").FontSize(9).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static byte[] AplicarVariablesEnDocx(byte[] docxBytes, Dictionary<string, string> variables)
    {
        using var ms = new MemoryStream();
        ms.Write(docxBytes, 0, docxBytes.Length);
        ms.Position = 0;

        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body is not null)
            {
                foreach (var text in body.Descendants<Text>())
                    foreach (var (key, value) in variables)
                        if (text.Text.Contains(key))
                            text.Text = text.Text.Replace(key, value);

                doc.MainDocumentPart!.Document.Save();
            }
        }

        return ms.ToArray();
    }

    private static List<(string texto, bool negrita, bool centrado)> ExtraerParrafosDeDocx(
        byte[] docxBytes)
    {
        var resultado = new List<(string, bool, bool)>();

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return resultado;

        foreach (var para in body.Elements<Paragraph>())
        {
            var texto = string.Concat(para.Descendants<Text>().Select(t => t.Text));
            var negrita = para.Descendants<Bold>().Any();
            var justificacion = para.ParagraphProperties?.Justification?.Val;
            var centrado = justificacion?.Value == JustificationValues.Center;
            resultado.Add((texto, negrita, centrado));
        }

        return resultado;
    }

    private static string AplicarReemplazos(string texto, string contenidoResuelto) => texto;

    // ── Helpers compartidos ───────────────────────────────────────────────────

    private static void RenderFirma(ColumnDescriptor col, PlantillaDocumento plantilla)
    {
        if (string.IsNullOrWhiteSpace(plantilla.NombreFirmante)) return;

        col.Item().Column(firma =>
        {
            if (!string.IsNullOrWhiteSpace(plantilla.FirmaImagenBase64))
            {
                try
                {
                    var b64 = plantilla.FirmaImagenBase64
                        .Replace("data:image/png;base64,", "")
                        .Replace("data:image/jpeg;base64,", "")
                        .Replace("data:image/jpg;base64,", "");
                    var bytes = Convert.FromBase64String(b64);
                    firma.Item().Width(120).Image(bytes).FitWidth();
                }
                catch { /* imagen inválida — omitir */ }
            }

            firma.Item().BorderTop(1).BorderColor(Colors.Grey.Medium)
                .PaddingTop(4).Width(200);
            firma.Item().Text(plantilla.NombreFirmante).Bold().FontSize(10);

            if (!string.IsNullOrWhiteSpace(plantilla.CargoFirmante))
                firma.Item().Text(plantilla.CargoFirmante)
                    .FontSize(10).FontColor(Colors.Grey.Darken2);
        });
    }

    private static void RenderHtmlSimple(ColumnDescriptor col, string html)
    {
        // Normalizar saltos de línea
        var normalizado = html
            .Replace("\r\n", "\n").Replace("\r", "\n")
            .Replace("<br/>", "\n").Replace("<br />", "\n").Replace("<br>", "\n")
            .Replace("</p>", "\n").Replace("</div>", "\n")
            .Replace("&nbsp;", " ").Replace("&amp;", "&")
            .Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"");

        // Extraer bloques de texto preservando negrita/cursiva/subrayado
        var bloques = ParsearBloques(normalizado);

        foreach (var (texto, negrita, cursiva, subrayado) in bloques)
        {
            var trimmed = texto.Trim('\n');
            if (string.IsNullOrEmpty(trimmed))
            {
                col.Item().PaddingTop(4);
                continue;
            }
            foreach (var linea in trimmed.Split('\n'))
            {
                var l = linea.Trim();
                if (string.IsNullOrEmpty(l)) { col.Item().PaddingTop(4); continue; }
                col.Item().Text(t =>
                {
                    var span = t.Span(l).FontSize(11);
                    if (negrita) span.Bold();
                    if (cursiva) span.Italic();
                    if (subrayado) span.Underline();
                });
            }
        }
    }

    private static List<(string texto, bool negrita, bool cursiva, bool subrayado)> ParsearBloques(string html)
    {
        var result = new List<(string, bool, bool, bool)>();
        var regex = new System.Text.RegularExpressions.Regex(
            @"<(b|strong|i|em|u|/b|/strong|/i|/em|/u)>|<[^>]+>|([^<]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        bool negrita = false, cursiva = false, subrayado = false;
        foreach (System.Text.RegularExpressions.Match m in regex.Matches(html))
        {
            if (m.Groups[2].Success) // texto plano
            {
                var txt = m.Groups[2].Value;
                if (!string.IsNullOrEmpty(txt))
                    result.Add((txt, negrita, cursiva, subrayado));
            }
            else if (m.Groups[1].Success) // etiqueta de formato
            {
                var tag = m.Groups[1].Value.ToLower();
                switch (tag)
                {
                    case "b": case "strong": negrita = true; break;
                    case "/b": case "/strong": negrita = false; break;
                    case "i": case "em": cursiva = true; break;
                    case "/i": case "/em": cursiva = false; break;
                    case "u": subrayado = true; break;
                    case "/u": subrayado = false; break;
                }
            }
        }
        return result;
    }
}
