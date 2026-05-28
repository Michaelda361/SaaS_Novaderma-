using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TalentManagement.Application.Interfaces;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.ControlDocumental;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/control-documental")]
public class ControlDocumentalController(
    IControlDocumentalService service,
    IHubContext<TalentManagement.Server.Hubs.NotificacionesHub> hub,
    CurrentUserService currentUser) : ControllerBase
{
    [HttpGet("listados-maestros")]
    public async Task<IActionResult> GetListadosMaestros()
    {
        var email = currentUser.GetEmail();
        var listados = await service.GetListadosParaUsuarioAsync(email);
        return Ok(listados);
    }

    [HttpGet("listados-maestros/{id:int}")]
    public async Task<IActionResult> GetListadoMaestro(int id)
    {
        var listado = await service.GetListadoAsync(id);
        return listado is null ? NotFound() : Ok(listado);
    }

    [HttpGet("listados-maestros/{id:int}/export")]
    public async Task<IActionResult> ExportListadoMaestro(int id)
    {
        var listado = await service.GetListadoAsync(id);
        if (listado is null)
        {
            return NotFound();
        }

        using var workbook = new XLWorkbook();
        var listadoSheet = workbook.AddWorksheet("Listado");
        listadoSheet.Cell(1, 1).Value = "Nombre";
        listadoSheet.Cell(1, 2).Value = listado.Nombre;
        listadoSheet.Cell(2, 1).Value = "Descripción";
        listadoSheet.Cell(2, 2).Value = listado.Descripcion ?? string.Empty;
        listadoSheet.Columns().AdjustToContents();

        var camposSheet = workbook.AddWorksheet("Campos");
        camposSheet.Cell(1, 1).Value = "CampoClave";
        camposSheet.Cell(1, 2).Value = "Nombre";
        camposSheet.Cell(1, 3).Value = "Tipo";
        camposSheet.Cell(1, 4).Value = "Requerido";
        camposSheet.Cell(1, 5).Value = "Opciones";
        camposSheet.Cell(1, 6).Value = "Orden";

        var row = 2;
        foreach (var campo in listado.Campos.OrderBy(c => c.Orden))
        {
            camposSheet.Cell(row, 1).Value = campo.CampoClave;
            camposSheet.Cell(row, 2).Value = campo.Nombre;
            camposSheet.Cell(row, 3).Value = campo.Tipo;
            camposSheet.Cell(row, 4).Value = campo.Requerido ? "TRUE" : "FALSE";
            camposSheet.Cell(row, 5).Value = campo.Opciones ?? string.Empty;
            camposSheet.Cell(row, 6).Value = campo.Orden;
            row++;
        }

        camposSheet.Columns().AdjustToContents();

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var fileName = SanitizeFileName(listado.Nombre) + ".xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("listados-maestros/import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportListadoMaestro()
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            return Forbid();
        }

        var file = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;
        if (file is null)
        {
            return BadRequest("No se encontró un archivo XLSX en la solicitud.");
        }

        if (file.Length == 0)
        {
            return BadRequest("El archivo XLSX está vacío.");
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.InvariantCultureIgnoreCase))
        {
            return BadRequest("El archivo debe tener extensión .xlsx.");
        }

        try
        {
            using var workbook = new XLWorkbook(file.OpenReadStream());
            var dto = ParseListadoMaestroWorkbook(workbook, file.FileName);
            var email = currentUser.GetEmail();
            var created = await service.ImportListadoAsync(dto, email);
            return CreatedAtAction(nameof(GetListadoMaestro), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"No se pudo leer el archivo XLSX: {ex.Message}");
        }
    }

    private static CreateListadoMaestroDto ParseListadoMaestroWorkbook(XLWorkbook workbook, string fileName)
    {
        var dto = new CreateListadoMaestroDto
        {
            Nombre = Path.GetFileNameWithoutExtension(fileName) ?? "Listado importado"
        };

        ParseMetadataSheet(workbook, dto);

        var (documentosSheet, documentosHeaderRow) = FindWorksheetWithDocumentHeaders(workbook);
        if (documentosSheet is null || documentosHeaderRow is null)
        {
            throw new ArgumentException("No se encontró una hoja de documentos que contenga las columnas requeridas 'Codigo' y 'Nombre'.");
        }

        var headerDefinitions = BuildHeaderDefinitions(documentosHeaderRow);

        var customHeaderMap = new Dictionary<int, string>();
        var usedClave = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var orden = 1;

        foreach (var header in headerDefinitions)
        {
            var isFixed = IsFixedColumn(header.Key);
            var campoClave = isFixed ? MapHeaderToFieldKey(header.Key) ?? header.Key : GetUniqueCampoClave(header.Key, usedClave);

            if (isFixed && usedClave.Contains(campoClave))
            {
                continue;
            }
            usedClave.Add(campoClave);

            if (!isFixed)
            {
                customHeaderMap[header.Index] = campoClave;
            }

            var tipo = InferFieldType(documentosSheet, header.Index, documentosHeaderRow.RowNumber() + 1);
            dto.Campos.Add(new DocumentoControlCampoDto
            {
                CampoClave = campoClave,
                Nombre = string.IsNullOrWhiteSpace(header.OriginalName) ? header.Key : header.OriginalName,
                Tipo = isFixed ? MapHeaderToTipo(header.Key) : tipo,
                Requerido = isFixed && IsFieldRequiredByDefault(campoClave),
                EsPredeterminado = isFixed,
                Orden = orden++
            });
        }

        var headerMap = BuildHeaderMap(documentosHeaderRow);
        ParseDocumentoRows(documentosSheet, documentosHeaderRow, headerMap, headerDefinitions, customHeaderMap, dto);

        return dto;
    }

    private static void ParseMetadataSheet(XLWorkbook workbook, CreateListadoMaestroDto dto)
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

    private static IXLWorksheet? FindWorksheetWithMetadata(XLWorkbook workbook)
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

    private static void ParseCamposSheet(IXLWorksheet sheet, IXLRow headerRow, CreateListadoMaestroDto dto)
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

            dto.Campos.Add(new DocumentoControlCampoDto
            {
                CampoClave = clave,
                Nombre = nombreCampo,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "Texto" : tipo,
                Requerido = requerido,
                Opciones = string.IsNullOrWhiteSpace(opciones) ? null : opciones,
                Orden = orden
            });
        }
    }

    private static void ParseDocumentoRows(
        IXLWorksheet sheet,
        IXLRow headerRow,
        Dictionary<string, int> headerMap,
        IReadOnlyList<(string Key, string OriginalName, int Index)> headerDefinitions,
        Dictionary<int, string> customHeaderMap,
        CreateListadoMaestroDto dto)
    {
        foreach (var row in sheet.RowsUsed().Where(r => r.RowNumber() > headerRow.RowNumber()))
        {
            var codigo = GetCellString(row, headerMap, "Codigo");
            if (string.IsNullOrWhiteSpace(codigo))
            {
                continue;
            }

            var nombreDocumento = GetCellString(row, headerMap, "Nombre");
            if (string.IsNullOrWhiteSpace(nombreDocumento))
            {
                continue;
            }

            var proceso = GetCellString(row, headerMap, "ProcesoResponsable");
            if (string.IsNullOrWhiteSpace(proceso))
            {
                proceso = GetCellString(row, headerMap, "Lugar de archivo");
            }

            if (string.IsNullOrWhiteSpace(proceso))
            {
                proceso = "No asignado";
            }

            var version = GetCellString(row, headerMap, "Version");
            var estado = GetCellString(row, headerMap, "Estado");
            var fechaDocumento = GetCellDateTime(row, headerMap, "FechaDocumento")
                ?? GetCellDateTime(row, headerMap, "Fecha");
            var oneDriveUrl = GetCellString(row, headerMap, "OneDriveUrl");
            var oneDriveItemId = GetCellString(row, headerMap, "OneDrive Item ID");
            var archivoNombre = GetCellString(row, headerMap, "ArchivoNombre");
            var uso = GetCellString(row, headerMap, "Uso");
            var tiempoRetencion = GetCellString(row, headerMap, "TiempoRetencion");
            var proteccion = GetCellString(row, headerMap, "Proteccion");
            var recuperacion = GetCellString(row, headerMap, "Recuperacion");
            var disposicionFinal = GetCellString(row, headerMap, "DisposicionFinal");
            var observaciones = GetCellString(row, headerMap, "Observaciones");
            var comentarioCambio = GetCellString(row, headerMap, "ComentarioCambio");
            var area = GetCellString(row, headerMap, "Area");

            var customValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headerDefinitions)
            {
                if (customHeaderMap.TryGetValue(header.Index, out var campoClave))
                {
                    var value = FormatCellValue(row.Cell(header.Index));
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        customValues[campoClave] = value;
                    }
                }
            }

            dto.Documentos.Add(new TemplateDocumentoDto
            {
                Codigo = codigo,
                Nombre = nombreDocumento,
                ProcesoResponsable = proceso,
                Version = string.IsNullOrWhiteSpace(version) ? "1.0" : version,
                Estado = string.IsNullOrWhiteSpace(estado) ? "Borrador" : estado,
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
                Area = string.IsNullOrWhiteSpace(area) ? null : area,
                CamposPersonalizados = customValues
            });
        }
    }

    private static IEnumerable<DocumentoControlCampoDto> BuildCustomCampoDefinitionsFromHeaders(
        IReadOnlyList<(string Key, string OriginalName, int Index)> headerDefinitions,
        IXLWorksheet documentSheet,
        int headerRowNumber)
    {
        var orden = 1;
        var usedClave = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headerDefinitions)
        {
            if (IsFixedColumn(header.Key))
            {
                continue;
            }

            var campoClave = GetUniqueCampoClave(header.Key, usedClave);
            var tipo = InferFieldType(documentSheet, header.Index, headerRowNumber + 1);

            yield return new DocumentoControlCampoDto
            {
                CampoClave = campoClave,
                Nombre = string.IsNullOrWhiteSpace(header.OriginalName)
                    ? header.Key
                    : header.OriginalName,
                Tipo = tipo,
                Requerido = false,
                Orden = orden++
            };
        }
    }

    private static bool IsFixedColumn(string normalizedHeader)
    {
        var fixedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "codigo",
            "nombre",
            "procesoresponsable", "lugardearchivo", "proceso", "responsable",
            "version",
            "estado",
            "fechadocumento", "fecha", "fechadedocumento",
            "onedriveurl", "archivoonedrive", "url",
            "onedriveitemid", "itemid",
            "archivonombre", "archivo", "nombrearchivo", "nombredearchivo",
            "comentariocambio", "comentariodecambio", "cambio",
            "area", "areaid"
        };

        return fixedKeys.Contains(normalizedHeader);
    }

    private static bool IsFieldRequiredByDefault(string campoClave)
    {
        return campoClave switch
        {
            "Codigo" or "Nombre" or "ProcesoResponsable" or "FechaDocumento" or "OneDriveUrl" or "Estado" => true,
            _ => false
        };
    }

    private static string InferFieldType(IXLWorksheet documentSheet, int column, int startRow)
    {
        foreach (var row in documentSheet.RowsUsed().Where(r => r.RowNumber() >= startRow).Take(20))
        {
            var cell = GetEffectiveCell(row.Cell(column));
            if (cell.IsEmpty())
            {
                continue;
            }

            if (cell.TryGetValue<DateTime>(out _))
            {
                return "Fecha";
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

    private static string GetUniqueCampoClave(string campoClave, HashSet<string> existing)
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

    private static string? MapHeaderToFieldKey(string normalizedHeader)
    {
        return normalizedHeader switch
        {
            var key when key == "codigo" => "codigo",
            var key when key == "nombre" => "nombre",
            var key when key == "procesoresponsable" || key == "lugardearchivo" || key == "proceso" || key == "responsable" => "procesoresponsable",
            var key when key == "version" => "version",
            var key when key == "estado" => "estado",
            var key when key == "fechadocumento" || key == "fecha" || key == "fechadedocumento" => "fechadocumento",
            var key when key == "uso" => "uso",
            var key when key == "tiemporetencion" || key == "tiempoderetencion" || key == "retencion" => "tiemporetencion",
            var key when key == "proteccion" => "proteccion",
            var key when key == "recuperacion" => "recuperacion",
            var key when key == "disposicionfinal" || key == "disposicion" => "disposicionfinal",
            var key when key == "observaciones" => "observaciones",
            var key when key == "onedriveurl" || key == "archivoonedrive" || key == "url" => "onedriveurl",
            var key when key == "onedriveitemid" || key == "itemid" => "onedriveitemid",
            var key when key == "archivonombre" || key == "archivo" || key == "nombrearchivo" || key == "nombredearchivo" => "archivonombre",
            var key when key == "comentariocambio" || key == "comentariodecambio" || key == "cambio" => "comentariocambio",
            var key when key == "area" || key == "areaid" => "area",
            _ => null
        };
    }

    private static string MapHeaderToTipo(string normalizedKey)
    {
        return normalizedKey switch
        {
            var k when k == "fechadocumento" || k == "fecha" || k == "fechadedocumento" => "Fecha",
            _ => "Texto"
        };
    }

    private static string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string headerName)
    {
        return TryGetCellIndex(headerMap, headerName, out var index)
            ? GetEffectiveCell(row.Cell(index)).GetString().Trim()
            : string.Empty;
    }

    private static DateTime? GetCellDateTime(IXLRow row, Dictionary<string, int> headerMap, string headerName)
    {
        if (!TryGetCellIndex(headerMap, headerName, out var index))
        {
            return null;
        }

        var cell = GetEffectiveCell(row.Cell(index));
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue<DateTime>(out var dateValue))
        {
            return dateValue;
        }

        if (DateTime.TryParse(cell.GetString().Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool TryGetCellIndex(Dictionary<string, int> headerMap, string headerName, out int index)
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

        index = -1;
        return false;
    }

    private static IEnumerable<string> GetHeaderSynonyms(string normalizedHeader)
    {
        return normalizedHeader switch
        {
            var n when n == "procesoresponsable" =>
                new[] { "procesoresponsable", "lugardearchivo", "proceso", "responsable" },
            var n when n == "fechadocumento" =>
                new[] { "fechadocumento", "fecha", "fechadedocumento" },
            var n when n == "onedriveurl" =>
                new[] { "onedriveurl", "archivoonedrive", "url" },
            var n when n == "onedriveitemid" =>
                new[] { "onedriveitemid", "itemid" },
            var n when n == "archivonombre" =>
                new[] { "archivonombre", "archivo", "nombrearchivo", "nombredearchivo" },
            var n when n == "comentariocambio" =>
                new[] { "comentariocambio", "comentariodecambio", "cambio" },
            var n when n == "area" =>
                new[] { "area", "areaid" },
            var n when n == "tiemporetencion" =>
                new[] { "tiemporetencion", "tiempoderetencion", "retencion" },
            var n when n == "disposicionfinal" =>
                new[] { "disposicionfinal", "disposicion" },
            _ => Array.Empty<string>()
        };
    }

    private static string FormatCellValue(IXLCell cell)
    {
        cell = GetEffectiveCell(cell);
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (cell.TryGetValue<DateTime>(out var dateValue))
        {
            return dateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (cell.DataType == XLDataType.Number)
        {
            return cell.GetDouble().ToString(CultureInfo.InvariantCulture);
        }

        return cell.GetString().Trim();
    }

    private static IXLCell GetEffectiveCell(IXLCell cell)
    {
        return cell.IsMerged() ? cell.MergedRange().FirstCell() : cell;
    }

    private static IReadOnlyList<(string Key, string OriginalName, int Index)> BuildHeaderDefinitions(IXLRow headerRow)
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
                originalName = $"Columna {column}";
            }

            headers.Add((
                Key: NormalizeHeaderKey(originalName),
                OriginalName: originalName.Trim(),
                Index: column));
        }

        return EnsureUniqueHeaderNames(headers);
    }

    private static (IXLWorksheet? Worksheet, IXLRow? HeaderRow) FindWorksheetWithHeaders(XLWorkbook workbook, params string[] requiredHeaders)
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

    private static (IXLWorksheet? Worksheet, IXLRow? HeaderRow) FindWorksheetWithDocumentHeaders(XLWorkbook workbook)
    {
        IXLWorksheet? bestSheet = null;
        IXLRow? bestHeader = null;
        var bestScore = -1;

        foreach (var worksheet in workbook.Worksheets)
        {
            foreach (var row in worksheet.RowsUsed().Take(20))
            {
                var headerMap = BuildHeaderMap(row);
                if (!headerMap.ContainsKey(NormalizeHeaderKey("Codigo")) || !headerMap.ContainsKey(NormalizeHeaderKey("Nombre")))
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

    private static IXLWorksheet? FindWorksheetWithRowKey(XLWorkbook workbook, string key)
    {
        foreach (var worksheet in workbook.Worksheets)
        {
            foreach (var row in worksheet.RowsUsed().Take(20))
            {
                if (NormalizeHeaderKey(row.Cell(1).GetString()) == NormalizeHeaderKey(key))
                {
                    return worksheet;
                }
            }
        }

        return null;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
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

    private static string GetHeaderText(IXLCell cell)
    {
        cell = GetEffectiveCell(cell);
        return cell?.GetString().Trim() ?? string.Empty;
    }

    private static IReadOnlyList<(string Key, string OriginalName, int Index)> EnsureUniqueHeaderNames(
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

    private static string NormalizeHeaderKey(string? key)
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

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalidChars.Contains(c))).Trim();
    }

    [HttpPost("listados-maestros")]
    public async Task<IActionResult> CreateListadoMaestro([FromBody] CreateListadoMaestroDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = currentUser.GetEmail();
        var created = await service.CreateListadoAsync(dto, email);
        return CreatedAtAction(nameof(GetListadosMaestros), new { id = created.Id }, created);
    }

    [HttpPut("listados-maestros/{id:int}")]
    public async Task<IActionResult> UpdateListadoMaestro(int id, [FromBody] CreateListadoMaestroDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = currentUser.GetEmail();
        var updated = await service.UpdateListadoAsync(id, dto, email);
        return updated is null ? NotFound() : NoContent();
    }

    [HttpDelete("listados-maestros/{id:int}")]
    public async Task<IActionResult> DeleteListadoMaestro(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            return Forbid();
        }

        try
        {
            var deleted = await service.DeleteListadoAsync(id, currentUser.GetEmail());
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("documentos")]
    public async Task<IActionResult> GetDocumentos(
        [FromQuery] int listadoMaestroId,
        [FromQuery] int? areaId,
        [FromQuery] string? busqueda,
        [FromQuery] string? codigo,
        [FromQuery] string? proceso,
        [FromQuery] string? estado)
    {
        var documentos = await service.GetDocumentosAsync(listadoMaestroId, areaId, busqueda, codigo, proceso, estado);
        return Ok(documentos);
    }

    [HttpGet("documentos/{id:int}")]
    public async Task<IActionResult> GetDocumento(int id)
    {
        var documento = await service.GetDocumentoAsync(id);
        return documento is null ? NotFound() : Ok(documento);
    }

    [HttpPost("documentos")]
    public async Task<IActionResult> CreateDocumento([FromBody] CreateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            var document = await service.CreateDocumentoAsync(dto, email);
            return CreatedAtAction(nameof(GetDocumento), new { id = document.Id }, document);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("documentos/{id:int}")]
    public async Task<IActionResult> UpdateDocumento(int id, [FromBody] UpdateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            var updated = await service.UpdateDocumentoAsync(id, dto, email);
            return updated is null ? NotFound() : NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            try
            {
                var email = currentUser.GetEmail();
                var created = await service.CreateSolicitudCambioAsync(id, dto, email);
                return CreatedAtAction(nameof(GetSolicitudesPorDocumento), new { documentoId = id }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }

    [HttpPost("documentos/{documentoId:int}/solicitudes")]
    public async Task<IActionResult> CreateSolicitudCambio(int documentoId, [FromBody] UpdateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            var created = await service.CreateSolicitudCambioAsync(documentoId, dto, email);

            // Notify admins in real-time about the new control-documental solicitud
            try
            {
                await hub.Clients.Group("admins").SendAsync("NuevaSolicitudCambio", created);
            }
            catch
            {
                // Swallow errors - notification is best-effort
            }

            return CreatedAtAction(nameof(GetSolicitudesPorDocumento), new { documentoId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("solicitudes/pendientes")]
    public async Task<IActionResult> GetSolicitudesPendientes()
    {
        var email = currentUser.GetEmail();
        var solicitudes = await service.GetSolicitudesCambioPendientesAsync(email);
        return Ok(solicitudes);
    }

    [HttpGet("solicitudes/pendientes/count")]
    public async Task<IActionResult> CountSolicitudesPendientes()
    {
        try
        {
            var email = currentUser.GetEmail();
            var count = await service.CountSolicitudesCambioPendientesAsync(email);
            return Ok(count);
        }
        catch
        {
            return Ok(0);
        }
    }

    [HttpPost("solicitudes/{solicitudId:int}/aprobar")]
    public async Task<IActionResult> AprobarSolicitudCambio(int solicitudId)
    {
        try
        {
            var email = currentUser.GetEmail();
            await service.AprobarSolicitudCambioAsync(solicitudId, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("solicitudes/{solicitudId:int}/rechazar")]
    public async Task<IActionResult> RechazarSolicitudCambio(int solicitudId, [FromBody] RechazarSolicitudCambioDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            await service.RechazarSolicitudCambioAsync(solicitudId, dto.MotivoRechazo, email);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("documentos/{id:int}/auditoria")]
    public async Task<IActionResult> GetHistorial(int id)
    {
        var historial = await service.GetHistorialAsync(id);
        return Ok(historial);
    }

    [HttpGet("documentos/{documentoId:int}/solicitudes")]
    public async Task<IActionResult> GetSolicitudesPorDocumento(int documentoId)
    {
        var email = currentUser.GetEmail();
        var solicitudes = await service.GetSolicitudesCambioPorDocumentoAsync(documentoId, email);
        return Ok(solicitudes);
    }

    // ────── Endpoints de Permisos ──────

    [HttpGet("listados-maestros/{listadoId:int}/permisos")]
    public async Task<IActionResult> GetListadoPermisos(int listadoId)
    {
        try
        {
            var permisos = await service.GetListadoPermisosAsync(listadoId);
            return Ok(permisos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("listados-maestros/{listadoId:int}/permisos/actual")]
    public async Task<IActionResult> GetListadoPermisosActual(int listadoId)
    {
        try
        {
            var email = currentUser.GetEmail();
            var permiso = await service.GetListadoPermisosActualUsuarioAsync(listadoId, email);
            return permiso is null ? NotFound() : Ok(permiso);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("listados-maestros/{listadoId:int}/permisos")]
    public async Task<IActionResult> UpdateListadoPermisos(int listadoId, [FromBody] List<ListadoMaestroPermisoUpdateDto>? permisos)
    {
        try
        {
            var email = currentUser.GetEmail();
            permisos = permisos ?? new List<ListadoMaestroPermisoUpdateDto>();
            await service.UpdateListadoPermisosAsync(listadoId, permisos, email);
            return Ok(new { mensaje = "Permisos actualizados correctamente." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
