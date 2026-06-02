using System.IO;
using Microsoft.Extensions.Configuration;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TalentManagement.Application.Interfaces;
using TalentManagement.Shared.DTOs.Certificados;

namespace TalentManagement.Infrastructure.Services;

public sealed class CertificatePdfGenerator : ICertificatePdfGenerator
{
    private readonly PdfGeneratorService _pdfGenerator;
    private readonly string _templatePath;

    public CertificatePdfGenerator(
        PdfGeneratorService pdfGenerator,
        IConfiguration configuration)
    {
        _pdfGenerator = pdfGenerator;
        var configuredPath = configuration["CertificateTemplate:Path"];
        _templatePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "Templates", "CertificateTemplate.pdf")
            : configuredPath;
    }

    public byte[] Generate(CertificatePdfDataDto data)
    {
        var template = LoadTemplateOrFallback(data);
        if (template is null)
            return BuildFallbackCertificate(data);

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ParticipantName"] = data.ParticipantName,
            ["TrainingName"] = data.TrainingName,
            ["IssuedDate"] = data.IssuedDate.ToString("dd/MM/yyyy"),
            ["DurationHours"] = data.DurationHours.ToString(),
            ["CertificateCode"] = data.CertificateCode
        };

        var filled = _pdfGenerator.GenerarPdfDesdePdf(template, variables);
        return FlattenPdf(filled);
    }

    private byte[]? LoadTemplateOrFallback(CertificatePdfDataDto data)
    {
        if (File.Exists(_templatePath))
            return File.ReadAllBytes(_templatePath);

        return null;
    }

    private static byte[] FlattenPdf(byte[] pdfBytes)
    {
        using var input = new MemoryStream(pdfBytes);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        var acroForm = document.AcroForm;
        if (acroForm is not null && acroForm.Fields is not null)
        {
            SetFieldsReadOnly(acroForm.Fields);
            acroForm.Elements.SetBoolean("/NeedAppearances", false);
        }

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }

    private static void SetFieldsReadOnly(dynamic fields)
    {
        foreach (var field in fields)
        {
            if (field is PdfTextField textField)
                textField.ReadOnly = true;

            var childFields = field.Fields;
            if (childFields is not null)
                SetFieldsReadOnly(childFields);
        }
    }

    private static byte[] BuildFallbackCertificate(CertificatePdfDataDto data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Black));

                page.Content().Padding(20).Column(column =>
                {
                    column.Spacing(15);

                    column.Item().AlignCenter().Text("CERTIFICADO").FontSize(32).SemiBold().FontColor(Colors.Blue.Medium);
                    column.Item().AlignCenter().Text("Se certifica que").FontSize(14).FontColor(Colors.Grey.Darken2);
                    column.Item().AlignCenter().Text(data.ParticipantName).FontSize(26).Bold().FontColor(Colors.Black);
                    column.Item().AlignCenter().Text("ha completado satisfactoriamente").FontSize(14).FontColor(Colors.Grey.Darken2);
                    column.Item().AlignCenter().Text(data.TrainingName).FontSize(20).SemiBold().FontColor(Colors.Black);

                    column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Fecha de emisión").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(data.IssuedDate.ToString("dd/MM/yyyy")).FontSize(14).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Duración").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{data.DurationHours} horas").FontSize(14).Bold();
                        });
                    });

                    column.Item().AlignCenter().PaddingTop(10).Text("Código de certificado").FontSize(9).FontColor(Colors.Grey.Darken1);
                    column.Item().AlignCenter().Text(data.CertificateCode).FontSize(16).Bold().FontColor(Colors.Black);

                    column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Firma autorizada").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text("Departamento de Capacitación").FontSize(12).SemiBold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Generado el").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(data.IssuedDate.ToString("dd/MM/yyyy")).FontSize(12).SemiBold();
                        });
                    });
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }
}
