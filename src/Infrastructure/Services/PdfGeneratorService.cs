using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TalentManagement.Application.Services;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using PdfDocument = QuestPDF.Fluent.Document;

namespace TalentManagement.Infrastructure.Services;

public class PdfGeneratorService
{
    public PdfGeneratorService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generar(string contenidoResuelto, PlantillaDocumento plantilla)
    {
        if (plantilla.TipoPlantilla == TipoPlantilla.Docx)
        {
            var payload = System.Text.Json.JsonSerializer.Deserialize<DocxReemplazoPayload>(contenidoResuelto)!;
            var docxBytes = Convert.FromBase64String(payload.DocxBase64);
            var docxModificado = AplicarVariablesEnDocx(docxBytes, payload.Variables);
            return GenerarDesdeDocx(docxModificado, plantilla);
        }
        return GenerarDesdeHtml(contenidoResuelto, plantilla);
    }

    // ── HTML → PDF ────────────────────────────────────────────────────────────
    // Parser HTML fiel: soporta bold, italic, underline, listas, alineación

    private static byte[] GenerarDesdeHtml(string html, PlantillaDocumento plantilla)
    {
        var bloques = ParsearHtml(html);

        return PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    foreach (var bloque in bloques)
                        RenderBloque(col, bloque);

                    if (!string.IsNullOrWhiteSpace(plantilla.NombreFirmante))
                    {
                        col.Item().PaddingTop(40);
                        RenderFirma(col, plantilla);
                    }
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

    private static void RenderBloque(ColumnDescriptor col, HtmlBloque bloque)
    {
        if (bloque.EsLinea)
        {
            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            return;
        }

        if (bloque.EsEspaciado)
        {
            col.Item().PaddingTop(bloque.Espaciado);
            return;
        }

        if (bloque.EsLista)
        {
            foreach (var (item, orden) in bloque.Items.Select((x, i) => (x, i + 1)))
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(20).Text(bloque.EsOrdenada ? $"{orden}." : "•")
                        .FontSize(11).FontFamily("Arial");
                    row.RelativeItem().Text(t =>
                    {
                        foreach (var span in item)
                            AplicarSpan(t, span);
                    });
                });
            }
            col.Item().PaddingTop(4);
            return;
        }

        if (!bloque.Spans.Any()) return;

        var item2 = col.Item();
        if (bloque.Alineacion == "center") item2 = item2.AlignCenter();
        else if (bloque.Alineacion == "right") item2 = item2.AlignRight();

        item2.Text(t =>
        {
            foreach (var span in bloque.Spans)
                AplicarSpan(t, span);
        });
    }

    private static void AplicarSpan(TextDescriptor t, HtmlSpan span)
    {
        if (string.IsNullOrEmpty(span.Texto)) return;
        var s = t.Span(span.Texto).FontSize(11).FontFamily("Arial");
        if (span.Negrita) s.Bold();
        if (span.Cursiva) s.Italic();
        if (span.Subrayado) s.Underline();
    }

    // ── DOCX → PDF ────────────────────────────────────────────────────────────

    private static byte[] GenerarDesdeDocx(byte[] docxBytes, PlantillaDocumento plantilla)
    {
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
                    foreach (var (texto, negrita, cursiva, centrado) in parrafos)
                    {
                        if (string.IsNullOrWhiteSpace(texto))
                        {
                            col.Item().PaddingTop(6);
                            continue;
                        }

                        var item = col.Item();
                        if (centrado) item = item.AlignCenter();

                        item.Text(t =>
                        {
                            var span = t.Span(texto).FontSize(11);
                            if (negrita) span.Bold();
                            if (cursiva) span.Italic();
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(plantilla.NombreFirmante))
                    {
                        col.Item().PaddingTop(40);
                        RenderFirma(col, plantilla);
                    }
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
                // Consolidar runs fragmentados antes de reemplazar para mayor fidelidad
                ConsolidarRuns(body);

                foreach (var text in body.Descendants<Text>())
                    foreach (var (key, value) in variables)
                        if (text.Text.Contains(key))
                            text.Text = text.Text.Replace(key, value);

                doc.MainDocumentPart!.Document.Save();
            }
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Word a veces fragmenta una variable en múltiples runs (ej: {{nombre}} → {{, nombre, }}).
    /// Este método consolida runs consecutivos con el mismo formato para que el reemplazo funcione.
    /// </summary>
    private static void ConsolidarRuns(Body body)
    {
        foreach (var para in body.Descendants<Paragraph>())
        {
            var runs = para.Elements<Run>().ToList();
            if (runs.Count < 2) continue;

            for (int i = runs.Count - 1; i > 0; i--)
            {
                var prev = runs[i - 1];
                var curr = runs[i];

                // Solo consolidar si tienen el mismo formato (o ambos sin formato)
                var prevProps = prev.RunProperties?.OuterXml ?? string.Empty;
                var currProps = curr.RunProperties?.OuterXml ?? string.Empty;

                if (prevProps == currProps)
                {
                    var prevText = prev.GetFirstChild<Text>();
                    var currText = curr.GetFirstChild<Text>();
                    if (prevText is not null && currText is not null)
                    {
                        prevText.Text += currText.Text;
                        prevText.Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;
                        curr.Remove();
                    }
                }
            }
        }
    }

    private static List<(string texto, bool negrita, bool cursiva, bool centrado)>
        ExtraerParrafosDeDocx(byte[] docxBytes)
    {
        var resultado = new List<(string, bool, bool, bool)>();

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return resultado;

        foreach (var para in body.Elements<Paragraph>())
        {
            var texto = string.Concat(para.Descendants<Text>().Select(t => t.Text));
            var negrita = para.Descendants<Bold>().Any();
            var cursiva = para.Descendants<Italic>().Any();
            var justificacion = para.ParagraphProperties?.Justification?.Val;
            var centrado = justificacion?.Value == JustificationValues.Center;
            resultado.Add((texto, negrita, cursiva, centrado));
        }

        return resultado;
    }

    // ── Parser HTML completo ──────────────────────────────────────────────────

    private static List<HtmlBloque> ParsearHtml(string html)
    {
        var bloques = new List<HtmlBloque>();
        // Normalizar
        html = html
            .Replace("\r\n", "\n").Replace("\r", "\n")
            .Replace("&nbsp;", " ").Replace("&amp;", "&")
            .Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"");

        // Tokenizar por etiquetas de bloque
        var tokenRegex = new System.Text.RegularExpressions.Regex(
            @"<(/?)(p|div|br|ul|ol|li|h[1-6]|hr)([^>]*)>|([^<]+)|<[^>]+>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        var inlineStack = new Stack<string>();
        var currentSpans = new List<HtmlSpan>();
        string currentAlineacion = "left";
        bool enLista = false;
        bool esOrdenada = false;
        var itemsLista = new List<List<HtmlSpan>>();
        var currentItemSpans = new List<HtmlSpan>();

        void FlushBloque()
        {
            if (currentSpans.Any(s => !string.IsNullOrEmpty(s.Texto)))
            {
                bloques.Add(new HtmlBloque { Spans = new(currentSpans), Alineacion = currentAlineacion });
                bloques.Add(new HtmlBloque { EsEspaciado = true, Espaciado = 3 });
            }
            currentSpans.Clear();
        }

        void FlushLista()
        {
            if (currentItemSpans.Any(s => !string.IsNullOrEmpty(s.Texto)))
                itemsLista.Add(new(currentItemSpans));
            currentItemSpans.Clear();

            if (itemsLista.Any())
            {
                bloques.Add(new HtmlBloque { EsLista = true, EsOrdenada = esOrdenada, Items = new(itemsLista) });
                bloques.Add(new HtmlBloque { EsEspaciado = true, Espaciado = 4 });
            }
            itemsLista.Clear();
        }

        bool Negrita() => inlineStack.Contains("b") || inlineStack.Contains("strong");
        bool Cursiva() => inlineStack.Contains("i") || inlineStack.Contains("em");
        bool Subrayado() => inlineStack.Contains("u");

        foreach (System.Text.RegularExpressions.Match m in tokenRegex.Matches(html))
        {
            var tagName = m.Groups[2].Value.ToLower();
            var isClose = m.Groups[1].Value == "/";
            var attrs = m.Groups[3].Value;
            var texto = m.Groups[4].Value;

            if (!string.IsNullOrEmpty(texto))
            {
                var span = new HtmlSpan { Texto = texto, Negrita = Negrita(), Cursiva = Cursiva(), Subrayado = Subrayado() };
                if (enLista) currentItemSpans.Add(span);
                else currentSpans.Add(span);
                continue;
            }

            if (string.IsNullOrEmpty(tagName))
            {
                // Etiqueta inline (b, i, u, strong, em, span, etc.)
                var inlineMatch = System.Text.RegularExpressions.Regex.Match(m.Value, @"<(/?)(b|strong|i|em|u)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (inlineMatch.Success)
                {
                    var tag = inlineMatch.Groups[2].Value.ToLower();
                    if (inlineMatch.Groups[1].Value == "/")
                    {
                        var tmp = new Stack<string>();
                        while (inlineStack.Count > 0 && inlineStack.Peek() != tag) tmp.Push(inlineStack.Pop());
                        if (inlineStack.Count > 0) inlineStack.Pop();
                        while (tmp.Count > 0) inlineStack.Push(tmp.Pop());
                    }
                    else inlineStack.Push(tag);
                }
                continue;
            }

            switch (tagName)
            {
                case "br":
                    if (enLista) currentItemSpans.Add(new HtmlSpan { Texto = "\n" });
                    else { currentSpans.Add(new HtmlSpan { Texto = "\n" }); FlushBloque(); }
                    break;

                case "hr":
                    FlushBloque();
                    bloques.Add(new HtmlBloque { EsLinea = true });
                    break;

                case "p":
                case "div":
                case "h1": case "h2": case "h3": case "h4": case "h5": case "h6":
                    if (!isClose)
                    {
                        // Detectar alineación en style o align
                        var styleMatch = System.Text.RegularExpressions.Regex.Match(attrs, @"text-align\s*:\s*(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        var alignMatch = System.Text.RegularExpressions.Regex.Match(attrs, @"align\s*=\s*[""']?(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        currentAlineacion = styleMatch.Success ? styleMatch.Groups[1].Value.ToLower()
                            : alignMatch.Success ? alignMatch.Groups[1].Value.ToLower()
                            : "left";
                        if (tagName.StartsWith("h")) inlineStack.Push("b");
                    }
                    else
                    {
                        if (tagName.StartsWith("h") && inlineStack.Contains("b"))
                        {
                            var tmp = new Stack<string>();
                            while (inlineStack.Count > 0 && inlineStack.Peek() != "b") tmp.Push(inlineStack.Pop());
                            if (inlineStack.Count > 0) inlineStack.Pop();
                            while (tmp.Count > 0) inlineStack.Push(tmp.Pop());
                        }
                        FlushBloque();
                        currentAlineacion = "left";
                    }
                    break;

                case "ul":
                    if (!isClose) { enLista = true; esOrdenada = false; }
                    else { FlushLista(); enLista = false; }
                    break;

                case "ol":
                    if (!isClose) { enLista = true; esOrdenada = true; }
                    else { FlushLista(); enLista = false; }
                    break;

                case "li":
                    if (!isClose)
                    {
                        if (currentItemSpans.Any(s => !string.IsNullOrEmpty(s.Texto)))
                            itemsLista.Add(new(currentItemSpans));
                        currentItemSpans.Clear();
                    }
                    break;
            }
        }

        FlushBloque();
        return bloques;
    }

    // ── Firma compartida ──────────────────────────────────────────────────────
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
                        .Replace("data:image/jpg;base64,", "")
                        .Replace("data:image/webp;base64,", "");
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
}

// ── Tipos de soporte para el parser HTML ─────────────────────────────────────

internal class HtmlSpan
{
    public string Texto { get; set; } = string.Empty;
    public bool Negrita { get; set; }
    public bool Cursiva { get; set; }
    public bool Subrayado { get; set; }
}

internal class HtmlBloque
{
    public List<HtmlSpan> Spans { get; set; } = [];
    public string Alineacion { get; set; } = "left";

    // Lista
    public bool EsLista { get; set; }
    public bool EsOrdenada { get; set; }
    public List<List<HtmlSpan>> Items { get; set; } = [];

    // Espaciado
    public bool EsEspaciado { get; set; }
    public float Espaciado { get; set; }

    // Línea horizontal
    public bool EsLinea { get; set; }
}
