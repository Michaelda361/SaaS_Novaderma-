using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using TalentManagement.Application.Interfaces;
using TalentManagement.Shared.DTOs.ControlDocumental;

namespace TalentManagement.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportListadoMaestro(ListadoMaestroDto listado, List<DocumentoControlDto> documentos)
    {
        using var workbook = new XLWorkbook();
        var sheetName = SanitizeSheetName(listado.Nombre);
        var documentosSheet = workbook.AddWorksheet(sheetName);
        
        var camposOrdenados = listado.Campos.OrderBy(c => c.Orden).ToList();
        
        // Estructura visual para encabezados
        documentosSheet.Row(1).Height = 28;
        for (int colIndex = 0; colIndex < camposOrdenados.Count; colIndex++)
        {
            var cell = documentosSheet.Cell(1, colIndex + 1);
            cell.Value = camposOrdenados[colIndex].Nombre;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11.5; // Resaltado de tamaño
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5"); // Color corporativo primario
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            
            // Separador inferior grueso para diferenciar los encabezados
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#312e81");
        }

        var docRow = 2;
        foreach (var doc in documentos)
        {
            documentosSheet.Row(docRow).Height = 20; // Altura cómoda para lectura de filas de datos
            for (int colIndex = 0; colIndex < camposOrdenados.Count; colIndex++)
            {
                var campo = camposOrdenados[colIndex];
                var value = GetValorDynamic(doc, campo.CampoClave);
                var cell = documentosSheet.Cell(docRow, colIndex + 1);
                cell.Value = value;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                
                // Zebra striping en filas pares para mejorar legibilidad
                if (docRow % 2 == 0)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");
                }
                
                // Línea divisoria sutil entre filas
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#e5e7eb");
            }
            docRow++;
        }
        documentosSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private string GetValorDynamic(DocumentoControlDto item, string campoClave)
    {
        if (item == null) return string.Empty;

        if (item.CamposPersonalizados != null && item.CamposPersonalizados.TryGetValue(campoClave, out var customVal))
        {
            return customVal ?? string.Empty;
        }

        var valFuzzy = GetValorCamposPersonalizados(item.CamposPersonalizados, campoClave);
        if (valFuzzy != null)
        {
            return valFuzzy;
        }

        var keyNorm = campoClave.Trim().ToLowerInvariant()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ü", "u")
            .Replace("ñ", "n")
            .Replace("ç", "c")
            .Replace(" ", string.Empty);

        switch (keyNorm)
        {
            case "codigo":
                return item.Codigo;
            case "nombre":
                return item.Nombre;
            case "procesoresponsable":
            case "proceso":
            case "responsable":
                return item.ProcesoResponsable;
            case "version":
                return item.Version;
            case "fechadocumento":
            case "fecha":
            case "fechadedocumento":
                return item.FechaDocumento.ToString("yyyy-MM-dd");
            case "onedriveurl":
            case "url":
                return item.OneDriveUrl;
            case "estado":
                return item.Estado;
            case "area":
            case "areaid":
            case "areanombre":
                return item.AreaNombre ?? string.Empty;
            case "uso":
                return item.Uso ?? GetValorCamposPersonalizados(item.CamposPersonalizados, "uso") ?? string.Empty;
            case "tiemporetencion":
            case "retencion":
            case "tiempoderetencion":
                return item.TiempoRetencion ?? GetValorCamposPersonalizados(item.CamposPersonalizados, "tiemporetencion", "tiempoderetencion", "retencion") ?? string.Empty;
            case "proteccion":
                return item.Proteccion ?? GetValorCamposPersonalizados(item.CamposPersonalizados, "proteccion") ?? string.Empty;
            case "recuperacion":
                return item.Recuperacion ?? GetValorCamposPersonalizados(item.CamposPersonalizados, "recuperacion") ?? string.Empty;
            case "disposicionfinal":
            case "disposicion":
                return item.DisposicionFinal ?? GetValorCamposPersonalizados(item.CamposPersonalizados, "disposicionfinal", "disposicion") ?? string.Empty;
            case "observaciones":
                return item.Observaciones ?? GetValorCamposPersonalizados(item.CamposPersonalizados, "observaciones") ?? string.Empty;
            case "comentariocambio":
            case "cambio":
                return item.ComentarioCambio ?? GetValorCamposPersonalizados(item.CamposPersonalizados, "comentariocambio", "cambio") ?? string.Empty;
            case "archivonombre":
            case "archivo":
                return item.ArchivoNombre ?? string.Empty;
            default:
                return GetValorCamposPersonalizados(item.CamposPersonalizados, campoClave) ?? string.Empty;
        }
    }

    private string NormalizarClave(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return key.Trim().ToLowerInvariant()
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("ü", "u").Replace("ñ", "n").Replace("ç", "c").Replace(" ", string.Empty);
    }

    private string? GetValorCamposPersonalizados(Dictionary<string, string?>? campos, params string[] clavesCandidatas)
    {
        if (campos == null || !campos.Any()) return null;

        var normalizedCandidates = clavesCandidatas.Select(NormalizarClave).ToHashSet();

        foreach (var kvp in campos)
        {
            var keyNorm = NormalizarClave(kvp.Key);
            if (normalizedCandidates.Contains(keyNorm))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    private string SanitizeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Sheet1";
        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var sanitized = string.Concat(name.Where(c => !invalidChars.Contains(c)));
        if (sanitized.Length > 31)
        {
            sanitized = sanitized.Substring(0, 31);
        }
        return string.IsNullOrWhiteSpace(sanitized) ? "Sheet1" : sanitized.Trim();
    }

    public string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalidChars.Contains(c))).Trim();
    }
}
