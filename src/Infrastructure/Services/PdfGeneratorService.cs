using System.Reflection;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using TalentManagement.Application.Services;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using PdfDocument = QuestPDF.Fluent.Document;

namespace TalentManagement.Infrastructure.Services;

public class PdfGeneratorService(LibreOfficeConverterService libreOffice)
{
    private readonly LibreOfficeConverterService _libreOffice = libreOffice;

    static PdfGeneratorService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Genera PDF desde HTML. Solo para plantillas de tipo Html.</summary>
    public byte[] GenerarPdfDesdeHtml(string htmlResuelto, PlantillaDocumento plantilla) =>
        GenerarDesdeHtml(htmlResuelto, plantilla);

    /// <summary>
    /// Aplica variables al .docx y devuelve los bytes del archivo Word modificado.
    /// El formato original se preserva completamente — no se convierte a PDF.
    /// </summary>
    public byte[] GenerarDocx(string contenidoResuelto)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize<DocxReemplazoPayload>(contenidoResuelto)!;
        var docxBytes = Convert.FromBase64String(payload.DocxBase64);
        return AplicarVariablesEnDocx(docxBytes, payload.Variables);
    }

    /// <summary>
    /// Aplica variables al .docx y convierte a PDF.
    /// Usa LibreOffice si está disponible (máxima fidelidad).
    /// Cae en Syncfusion DocIORenderer como fallback.
    /// </summary>
    public byte[] GenerarPdfDesdeDocx(string contenidoResuelto)
    {
        var docxBytes = GenerarDocx(contenidoResuelto);

        if (_libreOffice.EstaDisponible())
        {
            var pdf = _libreOffice.ConvertirDocxAPdf(docxBytes);
            if (pdf is not null) return pdf;
        }

        // Fallback: Syncfusion DocIORenderer
        using var inputStream = new MemoryStream(docxBytes);
        using var wordDoc = new Syncfusion.DocIO.DLS.WordDocument(inputStream, Syncfusion.DocIO.FormatType.Docx);
        using var renderer = new Syncfusion.DocIORenderer.DocIORenderer();
        using var pdfDoc = renderer.ConvertToPDF(wordDoc);
        using var outputStream = new MemoryStream();
        pdfDoc.Save(outputStream);
        return outputStream.ToArray();
    }

    public byte[] GenerarPdfDesdePdf(byte[] pdfBytes, Dictionary<string, string> variables)
    {
        using var input = new MemoryStream(pdfBytes);
        using var document = new PdfLoadedDocument(input);

        var normalizedVariables = BuildPdfReplacementDictionary(variables);

        if (document.Form is PdfLoadedForm form && form.Fields is PdfLoadedFormFieldCollection fields && fields.Count > 0)
        {
            var normalizedFields = normalizedVariables
                .Select(kvp => new KeyValuePair<string, string>(NormalizeFieldName(kvp.Key), kvp.Value))
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            FillPdfFields(fields, normalizedFields);
            form.Flatten = true;

            using var output = new MemoryStream();
            document.Save(output);
            return output.ToArray();
        }

        return ReemplazarVariablesEnPdf(pdfBytes, normalizedVariables);
    }

    private static byte[] ReemplazarVariablesEnPdf(byte[] pdfBytes, Dictionary<string, string> variables)
    {
        using var input = new MemoryStream(pdfBytes);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        var contentWriterType = typeof(PdfDocument).Assembly.GetType("PdfSharpCore.Pdf.Content.ContentWriter");
        var writeMethod = contentWriterType?.GetMethod(
            "WriteContent",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(PdfSharpCore.Pdf.PdfPage), typeof(CObject) },
            null);

        foreach (var page in document.Pages)
        {
            var content = ContentReader.ReadContent(page);
            if (!ReplaceVariablesInContent(content, variables))
                continue;

            page.Contents.Elements.Clear();
            if (writeMethod is not null)
            {
                writeMethod.Invoke(null, new object[] { page, content });
            }
        }

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }

    private static void FillPdfFields(PdfLoadedFormFieldCollection fields, Dictionary<string, string> normalizedFields)
    {
        foreach (PdfLoadedField field in fields)
        {
            var fieldName = NormalizeFieldName(field.Name ?? string.Empty);
            if (!normalizedFields.TryGetValue(fieldName, out var value))
            {
                continue;
            }

            switch (field)
            {
                case PdfLoadedTextBoxField textBox:
                    textBox.Text = value;
                    break;
                case PdfLoadedComboBoxField comboBox:
                    comboBox.SelectedValue = value;
                    break;
                case PdfLoadedListBoxField listBox:
                    listBox.SelectedValue = new[] { value };
                    break;
                case PdfLoadedCheckBoxField checkBox:
                    checkBox.Checked = value.Equals("true", StringComparison.OrdinalIgnoreCase)
                                       || value == "1" || value.Equals("x", StringComparison.OrdinalIgnoreCase);
                    break;
                default:
                    field.SetValue("V", value);
                    break;
            }
        }
    }

    private static string NormalizeFieldName(string key)
        => key.Trim().TrimStart('{').TrimEnd('}').Trim().ToLowerInvariant();

    private static Dictionary<string, string> BuildPdfReplacementDictionary(Dictionary<string, string> variables)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in variables)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            replacements[key.Trim()] = value;

            var keyWithoutBraces = key.Trim().TrimStart('{').TrimEnd('}').Trim();
            if (!string.IsNullOrWhiteSpace(keyWithoutBraces) && !replacements.ContainsKey(keyWithoutBraces))
            {
                replacements[keyWithoutBraces] = value;
            }
        }

        return replacements;
    }

    private static bool ReplaceVariablesInContent(CObject? content, Dictionary<string, string> variables)
    {
        if (content is COperator op)
        {
            if (op.OpCode.Name is "Tj" && op.Operands.Count == 1 && op.Operands[0] is CString str)
            {
                return ReplaceInString(str, variables);
            }

            if (op.OpCode.Name is "TJ" && op.Operands.Count == 1 && op.Operands[0] is CArray array)
            {
                var changed = false;
                foreach (var element in array)
                {
                    if (element is CString item)
                        changed |= ReplaceInString(item, variables);
                }
                return changed;
            }
        }

        if (content is CSequence seq)
        {
            var changed = false;
            foreach (var item in seq)
                changed |= ReplaceVariablesInContent(item, variables);
            return changed;
        }

        if (content is CArray arr)
        {
            var changed = false;
            foreach (var item in arr)
                changed |= ReplaceVariablesInContent(item, variables);
            return changed;
        }

        return false;
    }

    private static bool ReplaceInString(CString textObject, Dictionary<string, string> variables)
    {
        var original = textObject.Value;
        var replaced = original;

        foreach (var (key, value) in variables)
        {
            if (string.IsNullOrEmpty(key)) continue;
            replaced = replaced.Replace(key, value, StringComparison.OrdinalIgnoreCase);
        }

        if (replaced != original)
        {
            textObject.Value = replaced;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extrae TODOS los párrafos del DOCX con el texto ya resuelto (variables aplicadas).
    /// Permite al colaborador editar cualquier parte del documento.
    /// </summary>
    public List<(int indice, string textoResuelto, string? contexto)>
        ExtraerParrafosEditables(byte[] docxBytes, Dictionary<string, string> variables)
    {
        var resultado = new List<(int, string, string?)>();
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return resultado;

        var parrafos = body.Descendants<Paragraph>().ToList();
        for (int i = 0; i < parrafos.Count; i++)
        {
            var texto = string.Concat(parrafos[i].Descendants<Text>().Select(t => t.Text));
            var textoResuelto = texto;
            foreach (var (key, value) in variables)
                textoResuelto = textoResuelto.Replace(key, value);
            resultado.Add((i, textoResuelto, null));
        }
        return resultado;
    }

    /// <summary>
    /// Aplica los textos editados por el colaborador a los párrafos del DOCX,
    /// preservando el formato (negrita, cursiva, fuente) del primer run de cada párrafo.
    /// </summary>
    public byte[] AplicarEdicionEnDocx(byte[] docxBytes, Dictionary<int, string> parrafosEditados)
    {
        using var ms = new MemoryStream();
        ms.Write(docxBytes, 0, docxBytes.Length);
        ms.Position = 0;

        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body is null) return docxBytes;

            var parrafos = body.Descendants<Paragraph>().ToList();
            foreach (var (indice, textoEditado) in parrafosEditados)
            {
                if (indice < 0 || indice >= parrafos.Count) continue;
                var para = parrafos[indice];
                var runs = para.Elements<Run>().ToList();
                if (!runs.Any()) continue;

                var primerRun = runs[0];
                var primerText = primerRun.GetFirstChild<Text>();
                if (primerText is null) continue;

                primerText.Text = textoEditado;
                primerText.Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;
                foreach (var r in runs.Skip(1)) r.Remove();
            }
            if (doc.MainDocumentPart?.Document != null)
            {
                doc.MainDocumentPart.Document.Save();
            }
        }
        return ms.ToArray();
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
            return;
        }

        if (!bloque.Spans.Any()) return;

        var item2 = col.Item().PaddingBottom(4);
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

    // ── DOCX: aplicar variables ───────────────────────────────────────────────

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
                ConsolidarRuns(body);
                ReemplazarEnParrafos(body, variables);
                if (doc.MainDocumentPart?.Document != null)
                {
                    doc.MainDocumentPart.Document.Save();
                }
            }
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Reemplaza variables en runs individuales. Si una variable queda fragmentada
    /// entre varios runs (Word la partió con formatos distintos), reconstruye el párrafo
    /// usando el formato del primer run como base.
    /// </summary>
    private static void ReemplazarEnParrafos(Body body, Dictionary<string, string> variables)
    {
        foreach (var para in body.Descendants<Paragraph>())
        {
            // Paso 1: reemplazo run a run (caso simple — variable en un solo run)
            foreach (var run in para.Descendants<Run>())
            {
                var textEl = run.GetFirstChild<Text>();
                if (textEl is null) continue;

                var original = textEl.Text;
                var reemplazado = original;
                foreach (var (key, value) in variables)
                    reemplazado = reemplazado.Replace(key, value);

                if (reemplazado == original) continue;

                textEl.Text = reemplazado;
                textEl.Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;
                NormalizarFuenteRun(run);
            }

            // Paso 2: si quedan variables fragmentadas entre runs, reconstruir el párrafo
            var textoParrafo = string.Concat(para.Descendants<Text>().Select(t => t.Text));
            if (!variables.Keys.Any(k => textoParrafo.Contains(k))) continue;

            var textoFinal = textoParrafo;
            foreach (var (key, value) in variables)
                textoFinal = textoFinal.Replace(key, value);

            var runs = para.Elements<Run>().ToList();
            if (!runs.Any()) continue;

            // Usar el run más largo como base de formato (es el texto "normal" del párrafo)
            var runBase = runs.OrderByDescending(r =>
                r.GetFirstChild<Text>()?.Text?.Length ?? 0).First();

            var primerRun = runs[0];
            var primerText = primerRun.GetFirstChild<Text>();
            if (primerText is null) continue;

            // Copiar propiedades del run base si es distinto al primero
            if (runBase != primerRun && runBase.RunProperties is not null)
            {
                primerRun.RunProperties?.Remove();
                primerRun.InsertBefore(
                    (RunProperties)runBase.RunProperties.CloneNode(true),
                    primerRun.GetFirstChild<Text>());
            }

            primerText.Text = textoFinal;
            primerText.Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;
            NormalizarFuenteRun(primerRun);

            foreach (var r in runs.Skip(1))
                r.Remove();
        }
    }

    /// <summary>
    /// Normaliza el run reemplazado para que herede el formato del párrafo:
    /// quita overrides de fuente, tamaño y color que venían del placeholder.
    /// Preserva negrita, cursiva y subrayado intencionales.
    /// </summary>
    private static void NormalizarFuenteRun(Run run)
    {
        var rPr = run.RunProperties;
        if (rPr is null) return;

        // Quitar override de fuente
        rPr.RemoveAllChildren<RunFonts>();
        // Quitar override de tamaño (sz = half-points, szCs = complex script)
        rPr.RemoveAllChildren<FontSize>();
        rPr.RemoveAllChildren<FontSizeComplexScript>();
        // Quitar override de color
        rPr.RemoveAllChildren<DocumentFormat.OpenXml.Wordprocessing.Color>();
        // Quitar highlight y shading que pudiera tener el placeholder
        rPr.RemoveAllChildren<Highlight>();
        rPr.RemoveAllChildren<Shading>();
        // Quitar spacing de caracteres y kern
        rPr.RemoveAllChildren<Spacing>();
        rPr.RemoveAllChildren<Kern>();

        if (!rPr.HasChildren)
            rPr.Remove();
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
                var inlineMatch = System.Text.RegularExpressions.Regex.Match(m.Value, @"<(/?)(b|strong|i|em|u|span)\b([^>]*)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (inlineMatch.Success)
                {
                    var tag = inlineMatch.Groups[2].Value.ToLower();
                    var isClosingTag = inlineMatch.Groups[1].Value == "/";

                    if (tag == "span")
                    {
                        // Los <span> del editor pueden tener style="font-size:..." pero
                        // los ignoramos como contenedores transparentes — el tamaño base
                        // del documento es uniforme (11pt definido en QuestPDF).
                    }
                    else if (isClosingTag)
                    {
                        var tmp = new Stack<string>();
                        while (inlineStack.Count > 0 && inlineStack.Peek() != tag) tmp.Push(inlineStack.Pop());
                        if (inlineStack.Count > 0) inlineStack.Pop();
                        while (tmp.Count > 0) inlineStack.Push(tmp.Pop());
                    }
                    else
                    {
                        inlineStack.Push(tag);
                    }
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

    // ── PPTX: generar PDF y aplicar variables ───────────────────────────────────

    /// <summary>
    /// Aplica variables a un .pptx y convierte a PDF usando LibreOffice.
    /// </summary>
    public byte[] GenerarPdfDesdePptx(byte[] pptxBytes, Dictionary<string, string> variables)
    {
        var pptxConVariables = AplicarVariablesEnPptx(pptxBytes, variables);

        if (_libreOffice.EstaDisponible())
        {
            var pdf = _libreOffice.ConvertirAPdf(pptxConVariables,
                "application/vnd.openxmlformats-officedocument.presentationml.presentation");
            if (pdf is not null) return pdf;
        }

        throw new InvalidOperationException(
            "LibreOffice no esta disponible. Instalalo para convertir plantillas PPTX a PDF.");
    }

    /// <summary>
    /// Aplica las variables a un PPTX y devuelve el PPTX resultante (sin convertir a PDF).
    /// Útil para entregar al usuario y dejar que PowerPoint exporte a PDF.
    /// </summary>
    public byte[] GenerarPptxAplicado(byte[] pptxBytes, Dictionary<string, string> variables)
    {
        return AplicarVariablesEnPptx(pptxBytes, variables);
    }

    private static byte[] AplicarVariablesEnPptx(byte[] pptxBytes, Dictionary<string, string> variables)
    {
        using var ms = new MemoryStream();
        ms.Write(pptxBytes, 0, pptxBytes.Length);
        ms.Position = 0;

        using (var pptx = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(ms, true))
        {
            var presentationPart = pptx.PresentationPart;
            if (presentationPart is null) return pptxBytes;

            foreach (var slidePart in presentationPart.SlideParts)
            {
                var slide = slidePart.Slide;
                if (slide is null) continue;

                foreach (var paragraph in slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                {
                    var textElements = paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>().ToList();
                    if (!textElements.Any()) continue;

                    var paragraphText = string.Concat(textElements.Select(t => t.Text ?? string.Empty));
                    if (string.IsNullOrEmpty(paragraphText)) continue;

                    var replacements = ObtenerReemplazos(paragraphText, variables);
                    if (replacements.Count == 0) continue;

                    AplicarReemplazosEnTexto(textElements, replacements);
                }

                slide.Save();
            }
        }

        return ms.ToArray();
    }

    private static List<(int Start, int Length, string Replacement)> ObtenerReemplazos(
        string text,
        Dictionary<string, string> variables)
    {
        var replacements = new List<(int Start, int Length, string Replacement)>();

        foreach (var (key, value) in variables)
        {
            if (string.IsNullOrEmpty(key)) continue;
            var index = 0;
            while (true)
            {
                index = text.IndexOf(key, index, StringComparison.Ordinal);
                if (index < 0) break;
                replacements.Add((index, key.Length, value));
                index += key.Length;
            }
        }

        replacements.Sort((a, b) => a.Start != b.Start ? b.Start - a.Start : b.Length - a.Length);
        return replacements;
    }

    private static void AplicarReemplazosEnTexto(
        List<DocumentFormat.OpenXml.Drawing.Text> textElements,
        List<(int Start, int Length, string Replacement)> replacements)
    {
        var spans = new List<(DocumentFormat.OpenXml.Drawing.Text Text, int Start, int End)>();
        var position = 0;

        foreach (var textElement in textElements)
        {
            var textValue = textElement.Text ?? string.Empty;
            spans.Add((textElement, position, position + textValue.Length));
            position += textValue.Length;
        }

        foreach (var (start, length, replacement) in replacements)
        {
            if (length <= 0) continue;
            var end = start + length;

            var firstIndex = spans.FindIndex(span => start < span.End && end > span.Start);
            if (firstIndex < 0) continue;
            var lastIndex = spans.FindLastIndex(span => start < span.End && end > span.Start);
            if (lastIndex < 0) continue;

            var firstSpan = spans[firstIndex];
            var lastSpan = spans[lastIndex];

            var firstText = firstSpan.Text.Text ?? string.Empty;
            var firstOffset = Math.Max(0, start - firstSpan.Start);
            var firstPrefix = firstText[..firstOffset];

            if (firstIndex == lastIndex)
            {
                var suffix = firstText[(end - firstSpan.Start)..];
                firstSpan.Text.Text = firstPrefix + replacement + suffix;
            }
            else
            {
                firstSpan.Text.Text = firstPrefix + replacement;

                for (int i = firstIndex + 1; i < lastIndex; i++)
                    spans[i].Text.Text = string.Empty;

                var lastText = lastSpan.Text.Text ?? string.Empty;
                var lastOffset = Math.Max(0, end - lastSpan.Start);
                var suffix = lastOffset < lastText.Length ? lastText[lastOffset..] : string.Empty;
                lastSpan.Text.Text = suffix;
            }

            // Actualizar posiciones de spans posteriores para el siguiente reemplazo.
            var delta = replacement.Length - length;
            if (delta != 0)
            {
                for (int i = lastIndex + 1; i < spans.Count; i++)
                    spans[i] = (spans[i].Text, spans[i].Start + delta, spans[i].End + delta);
            }
        }
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
