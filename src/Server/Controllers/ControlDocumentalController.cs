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

        Console.WriteLine($"[EXPORT DIAGNOSTIC] Encabezados recuperados para exportación: {listado.Campos.Count} columnas registradas en la base de datos.");

        using var workbook = new XLWorkbook();
        var sheetName = SanitizeSheetName(listado.Nombre);
        var documentosSheet = workbook.AddWorksheet(sheetName);
        
        var camposOrdenados = listado.Campos.OrderBy(c => c.Orden).ToList();
        for (int colIndex = 0; colIndex < camposOrdenados.Count; colIndex++)
        {
            documentosSheet.Cell(1, colIndex + 1).Value = camposOrdenados[colIndex].Nombre;
        }

        var documentos = await service.GetDocumentosAsync(id, null, null, null, null, null);
        Console.WriteLine($"[EXPORT DIAGNOSTIC] Registros recuperados para exportación: {documentos.Count} documentos.");

        var docRow = 2;
        foreach (var doc in documentos)
        {
            for (int colIndex = 0; colIndex < camposOrdenados.Count; colIndex++)
            {
                var campo = camposOrdenados[colIndex];
                var value = GetValorDynamic(doc, campo.CampoClave);
                documentosSheet.Cell(docRow, colIndex + 1).Value = value;
            }
            docRow++;
        }
        documentosSheet.Columns().AdjustToContents();

        Console.WriteLine($"[EXPORT DIAGNOSTIC] Exportación completada. Hoja '{sheetName}' escrita con {docRow - 2} documentos. Cantidad de columnas exportadas: {camposOrdenados.Count}.");

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var fileName = SanitizeFileName(listado.Nombre) + ".xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static string GetValorDynamic(DocumentoControlDto item, string campoClave)
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

    private static string? GetValorCamposPersonalizados(Dictionary<string, string?>? campos, params string[] clavesCandidatas)
    {
        if (campos == null || !campos.Any()) return null;
        foreach (var clave in clavesCandidatas)
        {
            if (campos.TryGetValue(clave, out var val))
            {
                return val;
            }
            var keyMatch = campos.Keys.FirstOrDefault(k => string.Equals(k, clave, StringComparison.OrdinalIgnoreCase));
            if (keyMatch != null)
            {
                return campos[keyMatch];
            }
        }
        foreach (var clave in clavesCandidatas)
        {
            var keyNorm = clave.ToLowerInvariant();
            var fuzzyMatch = campos.Keys.FirstOrDefault(k => k.ToLowerInvariant().Contains(keyNorm));
            if (fuzzyMatch != null)
            {
                return campos[fuzzyMatch];
            }
        }
        return null;
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

        Console.WriteLine($"[IMPORT DIAGNOSTIC] Archivo recibido: '{file.FileName}', Tamaño: {file.Length} bytes.");

        try
        {
            using var workbook = new XLWorkbook(file.OpenReadStream());
            var dto = ParseListadoMaestroWorkbook(workbook, file.FileName);
            
            Console.WriteLine($"[IMPORT DIAGNOSTIC] Registros listos para persistir. Listado: '{dto.Nombre}', Campos/Columnas: {dto.Campos.Count}, Documentos/Filas: {dto.Documentos.Count}.");

            var email = currentUser.GetEmail();
            var created = await service.ImportListadoAsync(dto, email);
            
            Console.WriteLine($"[IMPORT DIAGNOSTIC] Registros guardados exitosamente. Listado ID: {created.Id}, Nombre: '{created.Nombre}'.");

            return CreatedAtAction(nameof(GetListadoMaestro), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"[IMPORT DIAGNOSTIC] Error de validación al importar listado: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IMPORT DIAGNOSTIC] Error inesperado al importar listado: {ex.Message}");
            return BadRequest($"No se pudo leer el archivo XLSX: {ex.Message}");
        }
    }

    private static CreateListadoMaestroDto ParseListadoMaestroWorkbook(XLWorkbook workbook, string fileName)
    {
        Console.WriteLine($"[IMPORT DIAGNOSTIC] Iniciando parsing del libro de Excel. Nombre de archivo: '{fileName}'. Hojas disponibles: {string.Join(", ", workbook.Worksheets.Select(w => w.Name))}");

        var dto = new CreateListadoMaestroDto
        {
            Nombre = Path.GetFileNameWithoutExtension(fileName) ?? "Listado importado"
        };

        // 1. Leer metadatos del listado
        ParseMetadataSheet(workbook, dto);
        Console.WriteLine($"[IMPORT DIAGNOSTIC] Metadatos leídos. Nombre de listado a crear: '{dto.Nombre}', Descripción: '{dto.Descripcion ?? "ninguna"}'.");

        // 2. Intentar leer la hoja "Campos" si existe
        var (camposSheet, camposHeaderRow) = FindWorksheetWithHeaders(workbook, "CampoClave", "Nombre");
        if (camposSheet is not null && camposHeaderRow is not null)
        {
            Console.WriteLine($"[IMPORT DIAGNOSTIC] Hoja 'Campos' detectada en '{camposSheet.Name}'. Leyendo configuraciones de columnas.");
            ParseCamposSheet(camposSheet, camposHeaderRow, dto);
        }

        // 3. Buscar la hoja de "Documentos"
        var (documentosSheet, documentosHeaderRow) = FindWorksheetWithDocumentHeaders(workbook);
        if (documentosSheet is null || documentosHeaderRow is null)
        {
            throw new ArgumentException("No se encontró una hoja de documentos válida que contenga columnas identificables como 'Codigo' y 'Nombre' en la primera fila.");
        }

        Console.WriteLine($"[IMPORT DIAGNOSTIC] Hoja seleccionada para datos: '{documentosSheet.Name}'. Leyendo encabezados.");

        var headerDefinitions = BuildHeaderDefinitions(documentosHeaderRow);
        Console.WriteLine($"[IMPORT DIAGNOSTIC] Encabezados detectados ({headerDefinitions.Count} columnas): {string.Join(", ", headerDefinitions.Select(h => $"{h.OriginalName} ({h.Key} -> index {h.Index})"))}");

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

        Console.WriteLine($"[IMPORT DIAGNOSTIC] Fin de parsing. Total de columnas dinámicas generadas en DTO: {dto.Campos.Count}. Total de registros parsed en DTO: {dto.Documentos.Count}.");

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



    private static void ParseDocumentoRows(
        IXLWorksheet sheet,
        IXLRow headerRow,
        Dictionary<string, int> headerMap,
        IReadOnlyList<(string Key, string OriginalName, int Index)> headerDefinitions,
        Dictionary<int, string> customHeaderMap,
        CreateListadoMaestroDto dto)
    {
        var totalRowsUsed = sheet.RowsUsed().Count();
        Console.WriteLine($"[IMPORT DIAGNOSTIC] Iniciando parsing de filas en '{sheet.Name}'. Filas usadas totales: {totalRowsUsed}. Fila de cabecera: {headerRow.RowNumber()}.");
        Console.WriteLine($"[IMPORT DIAGNOSTIC] Total de filas detectadas con datos en la hoja: {totalRowsUsed - headerRow.RowNumber()}.");

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
                    // Preserve empty values as empty strings instead of discarding them
                    customValues[campoClave] = value;
                }
            }

            // Only skip row if it is completely empty of data in all mapped cells.
            // Also strip non-breaking spaces (\u00A0) and other invisible Unicode characters
            // that Excel may store in visually-empty cells.
            static bool IsEffectivelyEmpty(string? v)
                => string.IsNullOrWhiteSpace(v) ||
                   string.IsNullOrEmpty(v?.Trim().Trim('\u00A0', '\u200B', '\uFEFF', '\t'));

            if (customValues.Count == 0 || customValues.Values.All(IsEffectivelyEmpty))
            {
                Console.WriteLine($"[IMPORT DIAGNOSTIC] Fila {row.RowNumber()} omitida por estar vacía.");
                continue;
            }

            // Helper to get value by synonym search in customValues
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
        if (normalizedHeader == "codigo" || GetHeaderSynonyms("codigo").Contains(normalizedHeader))
            return "Codigo";
        if (normalizedHeader == "nombre" || GetHeaderSynonyms("nombre").Contains(normalizedHeader))
            return "Nombre";
        if (normalizedHeader == "procesoresponsable" || GetHeaderSynonyms("procesoresponsable").Contains(normalizedHeader))
            return "ProcesoResponsable";
        if (normalizedHeader == "version" || GetHeaderSynonyms("version").Contains(normalizedHeader))
            return "Version";
        if (normalizedHeader == "estado" || GetHeaderSynonyms("estado").Contains(normalizedHeader))
            return "Estado";
        if (normalizedHeader == "fechadocumento" || GetHeaderSynonyms("fechadocumento").Contains(normalizedHeader))
            return "FechaDocumento";
        if (normalizedHeader == "onedriveurl" || GetHeaderSynonyms("onedriveurl").Contains(normalizedHeader))
            return "OneDriveUrl";
        if (normalizedHeader == "onedriveitemid" || GetHeaderSynonyms("onedriveitemid").Contains(normalizedHeader))
            return "OneDriveItemId";
        if (normalizedHeader == "archivonombre" || GetHeaderSynonyms("archivonombre").Contains(normalizedHeader))
            return "ArchivoNombre";
        if (normalizedHeader == "comentariocambio" || GetHeaderSynonyms("comentariocambio").Contains(normalizedHeader))
            return "ComentarioCambio";
        if (normalizedHeader == "area" || GetHeaderSynonyms("area").Contains(normalizedHeader))
            return "Area";
        if (normalizedHeader == "observaciones" || GetHeaderSynonyms("observaciones").Contains(normalizedHeader))
            return "Observaciones";

        return normalizedHeader switch
        {
            var key when key == "uso" => "Uso",
            var key when key == "tiemporetencion" || key == "tiempoderetencion" || key == "retencion" => "TiempoRetencion",
            var key when key == "proteccion" => "Proteccion",
            var key when key == "recuperacion" => "Recuperacion",
            var key when key == "disposicionfinal" || key == "disposicion" => "DisposicionFinal",
            _ => null
        };
    }

    private static string MapHeaderToTipo(string normalizedKey)
    {
        if (normalizedKey == "fechadocumento" || GetHeaderSynonyms("fechadocumento").Contains(normalizedKey))
            return "Fecha";
        return "Texto";
    }

    private static string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string headerName, int headerRowNumber = 0)
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

    private static DateTime? GetCellDateTime(IXLRow row, Dictionary<string, int> headerMap, string headerName, int headerRowNumber = 0)
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

        // Fuzzy match: check if any key in headerMap contains the normalized name, or vice versa
        var fuzzyKey = headerMap.Keys.FirstOrDefault(k => k.Contains(normalized) || normalized.Contains(k));
        if (fuzzyKey is not null)
        {
            index = headerMap[fuzzyKey];
            return true;
        }

        index = -1;
        return false;
    }

    private static IEnumerable<string> GetHeaderSynonyms(string normalizedHeader)
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

    private static string FormatCellValue(IXLCell cell)
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
                continue;
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
        // Priority 1: a sheet explicitly named 'Documentos' (our export format)
        var documentosSheet = workbook.Worksheets
            .FirstOrDefault(w => w.Name.Equals("Documentos", StringComparison.OrdinalIgnoreCase));
        if (documentosSheet is not null)
        {
            // Its header is always row 1
            var docRow = documentosSheet.Row(1);
            Console.WriteLine($"[IMPORT DIAGNOSTIC] Found dedicated 'Documentos' sheet. Using row 1 as header.");
            return (documentosSheet, docRow);
        }

        // Priority 2: any sheet whose first 20 rows contain BOTH 'Codigo'/'Código' AND 'Nombre'
        // using EXACT and SYNONYM matching only — NO fuzzy substring matching to avoid false positives.
        IXLWorksheet? bestSheet = null;
        IXLRow? bestHeader = null;
        var bestScore = -1;

        // Accepted exact normalized keys for Codigo
        var codigoKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "codigo", "codigodedocumento", "cod", "reference", "ref" };
        // Accepted exact normalized keys for Nombre
        var nombreKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "nombre", "nombrededocumento", "nombredearchivo", "titulo", "title", "name", "nombredeldocumento" };

        foreach (var worksheet in workbook.Worksheets)
        {
            // Skip sheets that look like metadata / field definition sheets
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

                // Use exact + synonym matching only — no fuzzy substring
                bool hasCodigo = headerMap.Keys.Any(k => codigoKeys.Contains(k));
                bool hasNombre = headerMap.Keys.Any(k => nombreKeys.Contains(k));

                if (!hasCodigo || !hasNombre)
                {
                    continue;
                }

                // Score: prefer sheets with more headers (more columns = more complete header row)
                var score = headerMap.Count;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSheet = worksheet;
                    bestHeader = row;
                }
            }
        }

        if (bestSheet is not null)
        {
            Console.WriteLine($"[IMPORT DIAGNOSTIC] Found document header in sheet '{bestSheet.Name}' at row {bestHeader!.RowNumber()} with {bestScore} columns.");
        }
        else
        {
            Console.WriteLine($"[IMPORT DIAGNOSTIC] WARNING: No sheet with 'Codigo'+'Nombre' header columns found. Import will fail validation.");
        }

        // Do NOT fall back silently — return null so the caller throws a clear error.
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

    private static string SanitizeSheetName(string name)
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
    public async Task<IActionResult> AprobarSolicitudCambio(int solicitudId, [FromBody] AprobarSolicitudCambioDto? dto)
    {
        try
        {
            var email = currentUser.GetEmail();
            await service.AprobarSolicitudCambioAsync(solicitudId, dto?.Comentarios, email);
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

    [HttpPost("solicitudes/{solicitudId:int}/iniciar-revision")]
    public async Task<IActionResult> IniciarRevisionSolicitud(int solicitudId)
    {
        try
        {
            var email = currentUser.GetEmail();
            await service.IniciarRevisionSolicitudAsync(solicitudId, email);
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

    [HttpPut("solicitudes/{solicitudId:int}/borrador")]
    public async Task<IActionResult> UpdateBorradorDocumento(int solicitudId, [FromBody] UpdateDocumentoControlDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = currentUser.GetEmail();
            await service.UpdateBorradorDocumentoAsync(solicitudId, dto, email);
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

    [HttpPost("solicitudes/{solicitudId:int}/enviar-aprobacion")]
    public async Task<IActionResult> EnviarAAprobacion(int solicitudId)
    {
        try
        {
            var email = currentUser.GetEmail();
            await service.EnviarAAprobacionAsync(solicitudId, email);
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
