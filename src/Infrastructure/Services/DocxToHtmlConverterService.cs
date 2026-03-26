using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TalentManagement.Infrastructure.Services;

public class DocxToHtmlConverterService
{
    public string ConvertToHtml(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return string.Empty;

        var html = new System.Text.StringBuilder();
        html.AppendLine("<div class=\"docx-content\">");

        var parrafos = body.Elements<Paragraph>().ToList();
        
        foreach (var para in parrafos)
        {
            var htmlPara = ConvertParagraphToHtml(para);
            if (!string.IsNullOrWhiteSpace(htmlPara))
            {
                html.AppendLine(htmlPara);
            }
        }

        html.AppendLine("</div>");
        return html.ToString();
    }

    private string ConvertParagraphToHtml(Paragraph para)
    {
        if (para is null) return string.Empty;

        var estilos = GetParagraphStyles(para);
        var contenido = GetParagraphContent(para);

        if (string.IsNullOrWhiteSpace(contenido))
            return string.Empty;

        var tag = estilos.Lista ? "li" : "p";
        var style = estilos.Estilo;

        return $"<{tag}{style}>{contenido}</{tag}>";
    }

    private string GetParagraphContent(Paragraph para)
    {
        var runs = para.Elements<Run>().ToList();
        if (!runs.Any())
        {
            var text = para.InnerText;
            return string.IsNullOrWhiteSpace(text) ? string.Empty : EscapeHtml(text);
        }

        var sb = new System.Text.StringBuilder();
        bool enNegrita = false;
        bool enItalic = false;
        bool enSubrayado = false;

        foreach (var run in runs)
        {
            var runProps = run.RunProperties;
            bool tieneNegrita = runProps?.Bold is not null;
            bool tieneItalic = runProps?.Italic is not null;
            bool tieneSubrayado = runProps?.Underline is not null;

            var text = string.Concat(run.Descendants<Text>().Select(t => t.Text));
            if (string.IsNullOrWhiteSpace(text)) continue;

            text = EscapeHtml(text);

            var segmentos = new List<string>();
            int i = 0;
            
            while (i < text.Length)
            {
                bool abreNegrita = tieneNegrita && !enNegrita;
                bool cierraNegrita = enNegrita && !tieneNegrita;
                bool abreItalic = tieneItalic && !enItalic;
                bool cierraItalic = enItalic && !tieneItalic;
                bool abreSubrayado = tieneSubrayado && !enSubrayado;
                bool cierraSubrayado = enSubrayado && !tieneSubrayado;

                if (abreNegrita) segmentos.Add("<strong>");
                if (abreItalic) segmentos.Add("<em>");
                if (abreSubrayado) segmentos.Add("<u>");

                segmentos.Add(text[i].ToString());

                if (cierraSubrayado) segmentos.Add("</u>");
                if (cierraItalic) segmentos.Add("</em>");
                if (cierraNegrita) segmentos.Add("</strong>");

                if (tieneNegrita) enNegrita = true;
                if (tieneItalic) enItalic = true;
                if (tieneSubrayado) enSubrayado = true;

                i++;
            }

            if (!tieneNegrita) enNegrita = false;
            if (!tieneItalic) enItalic = false;
            if (!tieneSubrayado) enSubrayado = false;

            sb.Append(string.Concat(segmentos));
        }

        return sb.ToString();
    }

    private (bool Lista, string Estilo) GetParagraphStyles(Paragraph para)
    {
        var style = new System.Text.StringBuilder();
        bool esLista = false;

        var props = para.ParagraphProperties;
        if (props is null)
            return (false, string.Empty);

        var just = props.Justification?.Val;
        if (just is not null)
        {
            var justStr = just.Value.ToString();
            if (justStr == "Center" || justStr == "Distributed")
                style.Append("text-align:center;");
            else if (justStr == "Right")
                style.Append("text-align:right;");
        }

        var spacing = props.SpacingBetweenLines;
        if (spacing is not null)
        {
            if (!string.IsNullOrEmpty(spacing.Before?.Value))
                style.Append($"margin-top:10pt;");
            if (!string.IsNullOrEmpty(spacing.After?.Value))
                style.Append($"margin-bottom:10pt;");
        }

        var numbering = props.NumberingProperties;
        if (numbering is not null)
            esLista = true;

        var indent = props.Indentation;
        if (indent is not null)
        {
            if (!string.IsNullOrEmpty(indent.Left?.Value))
                style.Append($"margin-left:20pt;");
        }

        return (esLista, style.Length > 0 ? $" style=\"{style}\"" : string.Empty);
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}