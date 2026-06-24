using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using TalentManagement.Application.Interfaces;
using TalentManagement.Shared.DTOs.ControlDocumental;

namespace TalentManagement.Infrastructure.Services;

public class ExcelImportService : IExcelImportService
{
    public CreateListadoMaestroDto ParseListadoMaestro(Stream stream, string fileName)
    {
        using var workbook = new XLWorkbook(stream);
        return ParseListadoMaestroWorkbook(workbook, fileName);
    }

    private CreateListadoMaestroDto ParseListadoMaestroWorkbook(XLWorkbook workbook, string fileName)
    {
        var dto = new CreateListadoMaestroDto
        {
            Nombre = Path.GetFileNameWithoutExtension(fileName) ?? "Listado importado"
        };

        // 1. Leer metadatos del listado
        ParseMetadataSheet(workbook, dto);

        // 2. Intentar leer la hoja "Campos" si existe
        var (camposSheet, camposHeaderRow) = FindWorksheetWithHeaders(workbook, "CampoClave", "Nombre");
        if (camposSheet is not null && camposHeaderRow is not null)
        {
            ParseCamposSheet(camposSheet, camposHeaderRow, dto);
        }

        // 3. Buscar la hoja de "Documentos"
        var (documentosSheet, documentosHeaderRow) = FindWorksheetWithDocumentHeaders(workbook);
        if (documentosSheet is null || documentosHeaderRow is null)
        {
            throw new ArgumentException("No se encontró una hoja de documentos válida que contenga columnas identificables como 'Codigo' y 'Nombre' en la primera fila.");
        }

        var headerDefinitions = BuildHeaderDefinitions(documentosHeaderRow);

        var customHeaderMap = new Dictionary<int, string>();
        var usedClave = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Registrar primero los campos ya definidos por la hoja 'Campos'
        foreach (var campo in dto.Campos)
        {
            usedClave.Add(campo.CampoClave);
        }

        var orden = dto.Campos.Any() ? dto.Campos.Max(c => c.Orden) + 1 : 1;

        foreach (var header in headerDefinitions)
        {
            string campoClave;
            var campoExistente = dto.Campos.FirstOrDefault(c => 
                string.Equals(c.CampoClave, header.Key, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(NormalizeHeaderKey(c.Nombre), header.Key, StringComparison.OrdinalIgnoreCase));

            if (campoExistente is not null)
            {
                campoClave = campoExistente.CampoClave;
            }
            else
            {
                campoClave = GetUniqueCampoClave(header.Key, usedClave);
                var tipo = InferFieldType(documentosSheet, header.Index, documentosHeaderRow.RowNumber() + 1);
                dto.Campos.Add(new DocumentoControlCampoDto
                {
                    CampoClave = campoClave,
                    Nombre = string.IsNullOrWhiteSpace(header.OriginalName) ? header.Key : header.OriginalName,
                    Tipo = tipo,
                    Requerido = false,
                    EsPredeterminado = false,
                    Orden = orden++
                });
            }

            customHeaderMap[header.Index] = campoClave;
        }

        var headerMap = BuildHeaderMap(documentosHeaderRow);
        ParseDocumentoRows(documentosSheet, documentosHeaderRow, headerMap, headerDefinitions, customHeaderMap, dto);

        return dto;
    }

    private void ParseMetadataSheet(XLWorkbook workbook, CreateListadoMaestroDto dto)
    {
        var metadataSheet = FindWorksheetWithMetadata(workbook);
        if (metadataSheet is null)
        {
            return;
        }

        var values = metadataSheet.RowsUsed()
            .Where(r => !r.Cell(1).IsEmpty())
            .Select(r => new
            {
                Key = NormalizeHeaderKey(r.Cell(1).GetString()),
                Value = r.Cell(2).GetString().Trim()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        if (values.TryGetValue(NormalizeHeaderKey("Nombre"), out var nombre) && !string.IsNullOrWhiteSpace(nombre))
        {
            dto.Nombre = nombre;
        }

        if (values.TryGetValue(NormalizeHeaderKey("Descripción"), out var descripcion) ||
            values.TryGetValue(NormalizeHeaderKey("Descripcion"), out descripcion))
        {
            dto.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;
        }
    }

    private IXLWorksheet? FindWorksheetWithMetadata(XLWorkbook workbook)
    {
        var preferredNames = new[] { "Listado", "Listado Maestro", "ListadoMaestro", "Meta", "Metadata" };
        foreach (var worksheet in workbook.Worksheets.OrderBy(w => preferredNames.Any(name => w.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) ? 0 : 1))
        {
            var rows = worksheet.RowsUsed().Take(20).ToList();
            if (!rows.Any())
            {
                continue;
            }

            if (rows.Any(r => NormalizeHeaderKey(r.Cell(1).GetString()) == NormalizeHeaderKey("Nombre")))
            {
                if (rows.Any(r => NormalizeHeaderKey(r.Cell(1).GetString()) == NormalizeHeaderKey("Descripción")) ||
                    rows.Any(r => NormalizeHeaderKey(r.Cell(1).GetString()) == NormalizeHeaderKey("Descripcion")))
                {
                    return worksheet;
                }

                if (rows.All(r => r.CellsUsed().Count() <= 2))
                {
                    return worksheet;
                }
            }
        }

        return null;
    }

    private void ParseCamposSheet(IXLWorksheet sheet, IXLRow headerRow, CreateListadoMaestroDto dto)
    {
        var headerMap = BuildHeaderMap(headerRow);
        if (!headerMap.ContainsKey(NormalizeHeaderKey("CampoClave")) || !headerMap.ContainsKey(NormalizeHeaderKey("Nombre")))
        {
            throw new ArgumentException("La hoja de campos debe contener las columnas 'CampoClave' y 'Nombre'.");
        }

        foreach (var row in sheet.RowsUsed().Where(r => r.RowNumber() > headerRow.RowNumber()))
        {
            var clave = row.Cell(headerMap[NormalizeHeaderKey("CampoClave")]).GetString().Trim();
            if (string.IsNullOrWhiteSpace(clave))
            {
                continue;
            }

            var nombreCampo = row.Cell(headerMap[NormalizeHeaderKey("Nombre")]).GetString().Trim();
            if (string.IsNullOrWhiteSpace(nombreCampo))
            {
                continue;
            }

            var tipo = headerMap.TryGetValue(NormalizeHeaderKey("Tipo"), out var tipoIndex)
                ? row.Cell(tipoIndex).GetString().Trim()
                : "Texto";
            var requeridoTexto = headerMap.TryGetValue(NormalizeHeaderKey("Requerido"), out var requeridoIndex)
                ? row.Cell(requeridoIndex).GetString().Trim()
                : string.Empty;
            var opciones = headerMap.TryGetValue(NormalizeHeaderKey("Opciones"), out var opcionesIndex)
                ? row.Cell(opcionesIndex).GetString().Trim()
                : null;
            var orden = headerMap.TryGetValue(NormalizeHeaderKey("Orden"), out var ordenIndex) && int.TryParse(row.Cell(ordenIndex).GetString().Trim(), out var ordenValue)
                ? ordenValue
                : 0;

            var requerido = !string.IsNullOrWhiteSpace(requeridoTexto)
                && (requeridoTexto.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                    || requeridoTexto.Equals("SI", StringComparison.OrdinalIgnoreCase)
                    || requeridoTexto.Equals("SÍ", StringComparison.OrdinalIgnoreCase)
                    || requeridoTexto.Equals("1", StringComparison.OrdinalIgnoreCase));

            var isFixed = IsFixedColumn(NormalizeHeaderKey(clave));

            dto.Campos.Add(new DocumentoControlCampoDto
            {
                CampoClave = clave,
                Nombre = nombreCampo,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "Texto" : tipo,
                Requerido = requerido,
                EsPredeterminado = isFixed,
                Opciones = string.IsNullOrWhiteSpace(opciones) ? null : opciones,
                Orden = orden
            });
        }
    }

    private void ParseDocumentoRows(
        IXLWorksheet sheet,
        IXLRow headerRow,
        Dictionary<string, int> headerMap,
        IReadOnlyList<(string Key, string OriginalName, int Index)> headerDefinitions,
        Dictionary<int, string> customHeaderMap,
        CreateListadoMaestroDto dto)
    {
        var headerRowNumber = headerRow.RowNumber();

        foreach (var row in sheet.RowsUsed().Where(r => r.RowNumber() > headerRowNumber))
        {
            var customValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headerDefinitions)
            {
                if (customHeaderMap.TryGetValue(header.Index, out var campoClave))
                {
                    var cell = row.Cell(header.Index);
                    if (cell.IsMerged() && cell.MergedRange().FirstCell().Address.RowNumber <= headerRowNumber)
                    {
                        continue;
                    }
                    var value = FormatCellValue(cell);
                    customValues[campoClave] = value;
                }
            }

            static bool IsEffectivelyEmpty(string? v)
                => string.IsNullOrWhiteSpace(v) ||
                   string.IsNullOrEmpty(v?.Trim().Trim('\u00A0', '\u200B', '\uFEFF', '\t'));

            if (customValues.Count == 0 || customValues.Values.All(IsEffectivelyEmpty))
            {
                continue;
            }

            string GetValBySynonym(string mainKey, string fallbackValue)
            {
                foreach (var header in headerDefinitions)
                {
                    if (string.Equals(header.Key, mainKey, StringComparison.OrdinalIgnoreCase) || 
                        GetHeaderSynonyms(mainKey).Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        if (customHeaderMap.TryGetValue(header.Index, out var campoClave) &&
                            customValues.TryGetValue(campoClave, out var cellVal) &&
                            !string.IsNullOrWhiteSpace(cellVal))
                        {
                            return cellVal;
                        }
                    }
                }
                return fallbackValue;
            }

            var codigo = GetValBySynonym("codigo", "ROW-" + row.RowNumber() + "-" + Guid.NewGuid().ToString("N")[..6]);
            var nombreDocumento = GetValBySynonym("nombre", "Fila " + row.RowNumber());
            var proceso = GetValBySynonym("procesoresponsable", "No asignado");
            var version = GetValBySynonym("version", "1.0");
            var estado = GetValBySynonym("estado", "Borrador");
            var oneDriveUrl = GetValBySynonym("onedriveurl", "https://onedrive.live.com/placeholder");
            var oneDriveItemId = GetValBySynonym("onedriveitemid", null!);
            var archivoNombre = GetValBySynonym("archivonombre", null!);
            var uso = GetValBySynonym("uso", null!);
            var tiempoRetencion = GetValBySynonym("tiemporetencion", null!);
            var proteccion = GetValBySynonym("proteccion", null!);
            var recuperacion = GetValBySynonym("recuperacion", null!);
            var disposicionFinal = GetValBySynonym("disposicionfinal", null!);
            var observaciones = GetValBySynonym("observaciones", null!);
            var comentarioCambio = GetValBySynonym("comentariocambio", null!);
            var area = GetValBySynonym("area", null!);

            var fechaDocumento = GetCellDateTime(row, headerMap, "FechaDocumento", headerRowNumber)
                ?? GetCellDateTime(row, headerMap, "Fecha", headerRowNumber)
                ?? DateTime.UtcNow;

            dto.Documentos.Add(new TemplateDocumentoDto
            {
                Codigo = codigo,
                Nombre = nombreDocumento,
                ProcesoResponsable = proceso,
                Version = version,
                Estado = estado,
                FechaDocumento = fechaDocumento,
                OneDriveUrl = oneDriveUrl,
                OneDriveItemId = oneDriveItemId,
                ArchivoNombre = archivoNombre,
                Uso = uso,
                TiempoRetencion = tiempoRetencion,
                Proteccion = proteccion,
                Recuperacion = recuperacion,
                DisposicionFinal = disposicionFinal,
                Observaciones = observaciones,
                ComentarioCambio = comentarioCambio,
                Area = area,
                CamposPersonalizados = customValues
            });
        }
    }

    private bool IsFixedColumn(string normalizedHeader)
    {
        var fixedKeys = new[] { "codigo", "nombre", "procesoresponsable", "version", "estado", "fechadocumento", "onedriveurl", "onedriveitemid", "archivonombre", "comentariocambio", "area", "observaciones" };
        foreach (var key in fixedKeys)
        {
            if (string.Equals(key, normalizedHeader, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (GetHeaderSynonyms(key).Contains(normalizedHeader, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private string InferFieldType(IXLWorksheet documentSheet, int column, int startRow)
    {
        foreach (var row in documentSheet.RowsUsed().Where(r => r.RowNumber() >= startRow).Take(20))
        {
            var cell = GetEffectiveCell(row.Cell(column));
            if (cell.IsEmpty())
            {
                continue;
            }

            if (cell.DataType == XLDataType.DateTime)
            {
                if (cell.TryGetValue<DateTime>(out _))
                {
                    return "Fecha";
                }
            }

            if (cell.DataType == XLDataType.Number)
            {
                return "Numero";
            }

            var text = cell.GetString().Trim();
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return "Fecha";
            }
        }

        return "Texto";
    }

    private string GetUniqueCampoClave(string campoClave, HashSet<string> existing)
    {
        var uniqueClave = campoClave;
        var suffix = 1;

        while (existing.Contains(uniqueClave))
        {
            suffix++;
            uniqueClave = $"{campoClave}_{suffix}";
        }

        existing.Add(uniqueClave);
        return uniqueClave;
    }

    private string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string headerName, int headerRowNumber = 0)
    {
        if (!TryGetCellIndex(headerMap, headerName, out var index))
            return string.Empty;

        var cell = row.Cell(index);
        if (headerRowNumber > 0 && cell.IsMerged() && cell.MergedRange().FirstCell().Address.RowNumber <= headerRowNumber)
        {
            return string.Empty;
        }

        return GetEffectiveCell(cell).GetString().Trim();
    }

    private DateTime? GetCellDateTime(IXLRow row, Dictionary<string, int> headerMap, string headerName, int headerRowNumber = 0)
    {
        if (!TryGetCellIndex(headerMap, headerName, out var index))
        {
            return null;
        }

        var cell = row.Cell(index);
        if (headerRowNumber > 0 && cell.IsMerged() && cell.MergedRange().FirstCell().Address.RowNumber <= headerRowNumber)
        {
            return null;
        }

        var effCell = GetEffectiveCell(cell);
        if (effCell.IsEmpty())
        {
            return null;
        }

        if (effCell.DataType == XLDataType.DateTime)
        {
            if (effCell.TryGetValue<DateTime>(out var dateValue))
            {
                return dateValue;
            }
        }

        if (DateTime.TryParse(effCell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private bool TryGetCellIndex(Dictionary<string, int> headerMap, string headerName, out int index)
    {
        var normalized = NormalizeHeaderKey(headerName);
        if (headerMap.TryGetValue(normalized, out index))
        {
            return true;
        }

        foreach (var synonym in GetHeaderSynonyms(normalized))
        {
            if (headerMap.TryGetValue(synonym, out index))
            {
                return true;
            }
        }

        var fuzzyKey = headerMap.Keys.FirstOrDefault(k => k.Contains(normalized) || normalized.Contains(k));
        if (fuzzyKey is not null)
        {
            index = headerMap[fuzzyKey];
            return true;
        }

        index = -1;
        return false;
    }

    private IEnumerable<string> GetHeaderSynonyms(string normalizedHeader)
    {
        return normalizedHeader switch
        {
            var n when n == "codigo" =>
                new[] { "codigo", "codigodedocumento", "cod", "reference", "ref" },
            var n when n == "nombre" =>
                new[] { "nombre", "nombrededocumento", "nombredearchivo", "titulo", "title", "name", "nombredeldocumento" },
            var n when n == "procesoresponsable" =>
                new[] { "procesoresponsable", "proceso", "responsable" },
            var n when n == "version" =>
                new[] { "version", "revision" },
            var n when n == "estado" =>
                new[] { "estado", "status" },
            var n when n == "fechadocumento" =>
                new[] { "fechadocumento", "fecha", "fechadedocumento" },
            var n when n == "onedriveurl" =>
                new[] { "onedriveurl", "archivoonedrive", "url", "rutaonedrive", "ruta" },
            var n when n == "onedriveitemid" =>
                new[] { "onedriveitemid", "itemid" },
            var n when n == "archivonombre" =>
                new[] { "archivonombre", "archivo", "nombrearchivo", "nombredearchivo" },
            var n when n == "comentariocambio" =>
                new[] { "comentariocambio", "comentariodecambio", "cambio", "comentario" },
            var n when n == "area" =>
                new[] { "area", "areaid", "areanombre", "departamento" },
            var n when n == "observaciones" =>
                new[] { "observaciones", "observacion", "notas", "comentarios", "nota" },
            var n when n == "tiemporetencion" =>
                new[] { "tiemporetencion", "tiempoderetencion", "retencion" },
            var n when n == "disposicionfinal" =>
                new[] { "disposicionfinal", "disposicion" },
            _ => Array.Empty<string>()
        };
    }

    private string FormatCellValue(IXLCell cell)
    {
        cell = GetEffectiveCell(cell);
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            if (cell.TryGetValue<DateTime>(out var dateValue))
            {
                return dateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        if (cell.DataType == XLDataType.Number)
        {
            return cell.GetDouble().ToString(CultureInfo.InvariantCulture);
        }

        return cell.GetString();
    }

    private IXLCell GetEffectiveCell(IXLCell cell)
    {
        return cell.IsMerged() ? cell.MergedRange().FirstCell() : cell;
    }

    private IReadOnlyList<(string Key, string OriginalName, int Index)> BuildHeaderDefinitions(IXLRow headerRow)
    {
        var usedCells = headerRow.CellsUsed().ToList();
        if (!usedCells.Any())
        {
            return new List<(string Key, string OriginalName, int Index)>();
        }

        var startColumn = usedCells.Min(c => c.Address.ColumnNumber);
        var endColumn = usedCells.Max(c => c.Address.ColumnNumber);
        var headers = new List<(string Key, string OriginalName, int Index)>();

        for (var column = startColumn; column <= endColumn; column++)
        {
            var cell = headerRow.Cell(column);
            var originalName = GetHeaderText(cell);
            if (string.IsNullOrWhiteSpace(originalName))
            {
                continue;
            }

            headers.Add((
                Key: NormalizeHeaderKey(originalName),
                OriginalName: originalName.Trim(),
                Index: column));
        }

        return EnsureUniqueHeaderNames(headers);
    }

    private (IXLWorksheet? Worksheet, IXLRow? HeaderRow) FindWorksheetWithHeaders(XLWorkbook workbook, params string[] requiredHeaders)
    {
        foreach (var worksheet in workbook.Worksheets)
        {
            foreach (var row in worksheet.RowsUsed().Take(15))
            {
                var headerMap = BuildHeaderMap(row);
                if (requiredHeaders.All(header => headerMap.ContainsKey(NormalizeHeaderKey(header))))
                {
                    return (worksheet, row);
                }
            }
        }

        return (null, null);
    }

    private (IXLWorksheet? Worksheet, IXLRow? HeaderRow) FindWorksheetWithDocumentHeaders(XLWorkbook workbook)
    {
        var documentosSheet = workbook.Worksheets
            .FirstOrDefault(w => w.Name.Equals("Documentos", StringComparison.OrdinalIgnoreCase));
        if (documentosSheet is not null)
        {
            var docRow = documentosSheet.Row(1);
            return (documentosSheet, docRow);
        }

        IXLWorksheet? bestSheet = null;
        IXLRow? bestHeader = null;
        var bestScore = -1;

        var codigoKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "codigo", "codigodedocumento", "cod", "reference", "ref" };
        var nombreKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "nombre", "nombrededocumento", "nombredearchivo", "titulo", "title", "name", "nombredeldocumento" };

        foreach (var worksheet in workbook.Worksheets)
        {
            if (worksheet.Name.Equals("Listado", StringComparison.OrdinalIgnoreCase) ||
                worksheet.Name.Equals("Campos", StringComparison.OrdinalIgnoreCase) ||
                worksheet.Name.Equals("Meta", StringComparison.OrdinalIgnoreCase) ||
                worksheet.Name.Equals("Metadata", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            for (int r = 1; r <= 20; r++)
            {
                var row = worksheet.Row(r);
                if (row is null || row.IsEmpty())
                {
                    continue;
                }

                var headerMap = BuildHeaderMap(row);
                if (!headerMap.Any()) continue;

                bool hasCodigo = headerMap.Keys.Any(k => codigoKeys.Contains(k));
                bool hasNombre = headerMap.Keys.Any(k => nombreKeys.Contains(k));

                if (!hasCodigo || !hasNombre)
                {
                    continue;
                }

                var score = headerMap.Count;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSheet = worksheet;
                    bestHeader = row;
                }
            }
        }

        return (bestSheet, bestHeader);
    }

    private Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
    {
        var usedCells = headerRow.CellsUsed().ToList();
        if (!usedCells.Any())
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var startColumn = usedCells.Min(c => c.Address.ColumnNumber);
        var endColumn = usedCells.Max(c => c.Address.ColumnNumber);

        return Enumerable.Range(startColumn, endColumn - startColumn + 1)
            .Select(column => new
            {
                Key = NormalizeHeaderKey(GetHeaderText(headerRow.Cell(column))),
                Index = column
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(x => x.Key, x => x.Index, StringComparer.OrdinalIgnoreCase);
    }

    private string GetHeaderText(IXLCell cell)
    {
        cell = GetEffectiveCell(cell);
        return cell?.GetString().Trim() ?? string.Empty;
    }

    private IReadOnlyList<(string Key, string OriginalName, int Index)> EnsureUniqueHeaderNames(
        IEnumerable<(string Key, string OriginalName, int Index)> headers)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var uniqueHeaders = new List<(string Key, string OriginalName, int Index)>();

        foreach (var header in headers)
        {
            var originalName = header.OriginalName;
            if (counts.TryGetValue(originalName, out var count))
            {
                count++;
                counts[originalName] = count;
                originalName = $"{header.OriginalName} ({count})";
            }
            else
            {
                counts[originalName] = 1;
            }

            uniqueHeaders.Add((header.Key, originalName, header.Index));
        }

        return uniqueHeaders;
    }

    private string NormalizeHeaderKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var normalized = key.Trim().ToLowerInvariant();
        normalized = normalized
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ü", "u")
            .Replace("ñ", "n")
            .Replace("ç", "c")
            .Replace(" ", string.Empty)
            .Replace("'", string.Empty)
            .Replace("\"", string.Empty);

        return normalized;
    }
}
