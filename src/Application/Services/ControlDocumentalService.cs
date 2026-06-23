using System.Text.Json;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Shared.DTOs.ControlDocumental;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Application.Services;

public class ControlDocumentalService(
    IControlDocumentalRepository repository,
    IAuditLogRepository auditRepository,
    IColaboradorRepository colaboradorRepository,
    IAreaRepository areaRepository) : IControlDocumentalService
{
    private const string EntidadTipo = "ControlDocumental";

    public async Task<List<ListadoMaestroDto>> GetListadosAsync()
    {
        var listados = await repository.GetListadosAsync();
        return listados.Select(l => new ListadoMaestroDto
        {
            Id = l.Id,
            Nombre = l.Nombre,
            Descripcion = l.Descripcion
        }).ToList();
    }

    public async Task<List<ListadoMaestroDto>> GetListadosParaUsuarioAsync(string usuarioEmail)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);
        if (colaborador is null)
            return new List<ListadoMaestroDto>();

        if (colaborador.Rol == Domain.Enums.RolUsuario.Admin || colaborador.Rol == Domain.Enums.RolUsuario.Jefe)
            return await GetListadosAsync();

        var permisos = await repository.GetPermisosPorColaboradorAsync(colaborador.Id);
        var listadoIds = permisos.Where(p => p.PuedeVer).Select(p => p.ListadoMaestroId).ToHashSet();

        var listados = await repository.GetListadosAsync();
        return listados
            .Where(l => listadoIds.Contains(l.Id))
            .Select(MapToListadoDto)
            .ToList();
    }

    private async Task<bool> TienePermisoAccesoAsync(int listadoId, string usuarioEmail, string tipoPermiso)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);
        if (colaborador is null)
            return false;

        if (colaborador.Rol == RolUsuario.Admin || colaborador.Rol == RolUsuario.Jefe)
            return true;

        return await ValidarPermisoAsync(listadoId, colaborador.Id, tipoPermiso);
    }

    public async Task<ListadoMaestroDto?> GetListadoAsync(int id, string usuarioEmail)
    {
        if (!await TienePermisoAccesoAsync(id, usuarioEmail, "ver"))
            throw new UnauthorizedAccessException("No tiene permisos para acceder a este listado maestro.");

        var listado = await repository.GetListadoByIdAsync(id);
        if (listado is null) return null;
        var dto = MapToListadoDto(listado);
        var campos = (await repository.GetCamposPorListadoAsync(id)).ToList();
        dto.Campos = campos.Select(MapToCampoDto).ToList();
        return dto;
    }

    public async Task<List<DocumentoControlDto>> GetDocumentosAsync(
        int listadoId, int? areaId, string? busqueda, string? codigo,
        string? proceso, string? estado, string usuarioEmail)
    {
        if (!await TienePermisoAccesoAsync(listadoId, usuarioEmail, "ver"))
            throw new UnauthorizedAccessException("No tiene permisos para ver documentos de este listado maestro.");

        var docs = await repository.GetDocumentosAsync(listadoId, areaId, busqueda, codigo, proceso, estado);
        var docList = docs.ToList();
        return docList.Select(MapToDto).ToList();
    }

    public async Task<DocumentoControlDetalleDto?> GetDocumentoAsync(int id, string usuarioEmail)
    {
        var doc = await repository.GetDocumentoByIdAsync(id);
        if (doc is null) return null;

        if (!await TienePermisoAccesoAsync(doc.ListadoMaestroId, usuarioEmail, "ver"))
            throw new UnauthorizedAccessException("No tiene permisos para ver este documento.");

        return MapToDetalleDto(doc);
    }

    public async Task<ListadoMaestroDto> CreateListadoAsync(CreateListadoMaestroDto dto, string usuarioEmail)
    {
        // Verificar permisos: solo Jefe o Admin pueden crear listados maestros
        var creador = await TryResolverColaboradorAsync(usuarioEmail);
        if (creador is null || (creador.Rol != Domain.Enums.RolUsuario.Admin && creador.Rol != Domain.Enums.RolUsuario.Jefe))
            throw new UnauthorizedAccessException("No tiene permisos para crear listados maestros.");


        var listado = new ListadoMaestro
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim()
        };

        var created = await repository.CreateListadoAsync(listado);

        if (dto.Campos is not null && dto.Campos.Any())
        {
            foreach (var campoDto in dto.Campos)
            {
                var campo = MapToCampoDefinicion(campoDto, created.Id);
                await repository.CreateCampoAsync(campo);
            }
        }

        if (dto.Documentos != null && dto.Documentos.Any())
        {
            var areas = await areaRepository.GetAllAsync();
            var areasByName = areas.ToDictionary(a => a.Nombre.Trim(), a => a.Id, StringComparer.OrdinalIgnoreCase);
            int? GetAreaId(string? areaName)
            {
                if (string.IsNullOrWhiteSpace(areaName)) return null;
                return areasByName.TryGetValue(areaName.Trim(), out var id) ? id : null;
            }

            foreach (var docDto in dto.Documentos)
            {
                var doc = new DocumentoControl
                {
                    ListadoMaestroId = created.Id,
                    Activo = true
                };
                MapTemplateToDocumento(docDto, doc, GetAreaId(docDto.Area));

                var createdDoc = await repository.CreateDocumentoAsync(doc);

                var log = await BuildAuditLogAsync(
                    createdDoc.Id,
                    createdDoc.Nombre,
                    "Creado",
                    "Documento inicializado con plantilla base",
                    usuarioEmail,
                    createdDoc,
                    null);
                await auditRepository.CreateAsync(log);
            }
        }

        return new ListadoMaestroDto
        {
            Id = created.Id,
            Nombre = created.Nombre,
            Descripcion = created.Descripcion
        };
    }

    public async Task<ListadoMaestroDto?> UpdateListadoAsync(int id, CreateListadoMaestroDto dto, string usuarioEmail)
    {
        // Verificar permisos: solo Jefe o Admin pueden actualizar listados maestros
        var editor = await TryResolverColaboradorAsync(usuarioEmail);
        if (editor is null || (editor.Rol != Domain.Enums.RolUsuario.Admin && editor.Rol != Domain.Enums.RolUsuario.Jefe))
            throw new UnauthorizedAccessException("No tiene permisos para actualizar listados maestros.");


        var existing = await repository.GetListadoByIdAsync(id);
        if (existing is null) return null;

        existing.Nombre = dto.Nombre.Trim();
        existing.Descripcion = dto.Descripcion?.Trim();

        var updated = await repository.UpdateListadoAsync(existing);

        if (dto.Documentos is not null && dto.Documentos.Any())
        {
            await SyncImportDocumentsAsync(updated.Id, dto.Documentos, usuarioEmail);
        }

        if (dto.Campos is not null && dto.Campos.Any())
        {
            var existentes = existing.Campos?.ToDictionary(c => c.CampoClave, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, DocumentoControlCampoDefinicion>(StringComparer.OrdinalIgnoreCase);
            var solicitados = dto.Campos.ToDictionary(c => c.CampoClave, StringComparer.OrdinalIgnoreCase);

            foreach (var campoDto in dto.Campos)
            {
                if (existentes.TryGetValue(campoDto.CampoClave, out var existente))
                {
                    existente.Nombre = campoDto.Nombre.Trim();
                    existente.Tipo = string.IsNullOrWhiteSpace(campoDto.Tipo) ? "Texto" : campoDto.Tipo.Trim();
                    existente.Requerido = campoDto.Requerido;
                    existente.OpcionesJson = string.IsNullOrWhiteSpace(campoDto.Opciones)
                        ? null
                        : JsonSerializer.Serialize(campoDto.Opciones.Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).ToList());
                    existente.Orden = campoDto.Orden;
                    existente.EsPredeterminado = campoDto.EsPredeterminado;
                    await repository.UpdateCampoAsync(existente);
                }
                else
                {
                    var nuevo = MapToCampoDefinicion(campoDto, updated.Id);
                    await repository.CreateCampoAsync(nuevo);
                }
            }

            foreach (var existente in existentes.Values)
            {
                if (!solicitados.ContainsKey(existente.CampoClave))
                {
                    await repository.DeleteCampoAsync(existente);
                }
            }
        }

        return new ListadoMaestroDto
        {
            Id = updated.Id,
            Nombre = updated.Nombre,
            Descripcion = updated.Descripcion
        };
    }

    public async Task<ListadoMaestroDto> ImportListadoAsync(CreateListadoMaestroDto dto, string usuarioEmail)
    {
        ListadoMaestroDto? result = null;

        await repository.ExecuteInTransactionAsync(async () =>
        {
            var existing = await repository.GetListadoByNombreAsync(dto.Nombre.Trim());
            if (existing is null)
            {
                result = await CreateListadoAsync(dto, usuarioEmail);
                return;
            }

            var updated = await UpdateListadoAsync(existing.Id, dto, usuarioEmail);
            result = updated ?? await CreateListadoAsync(dto, usuarioEmail);
        });

        return result!;
    }

    private static bool IsPlaceholderCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return true;
        var norm = code.Trim().ToUpperInvariant();
        return norm == "N.A" || norm == "N/A" || norm == "NA" || norm == "NO APLICA" || norm == "N/D" || norm == "ND";
    }

    private async Task SyncImportDocumentsAsync(int listadoId, IEnumerable<TemplateDocumentoDto> documentos, string usuarioEmail)
    {

        // 1. Obtener todos los documentos activos actuales en base de datos
        var activeDbDocs = (await repository.GetDocumentosAsync(listadoId, null, null, null, null, null))
            .Where(d => d.Activo)
            .ToList();

        var dbDocsByCodigo = new Dictionary<string, DocumentoControl>(StringComparer.OrdinalIgnoreCase);
        var dbDocsPlaceholderByNombre = new Dictionary<string, DocumentoControl>(StringComparer.OrdinalIgnoreCase);

        foreach (var doc in activeDbDocs)
        {
            var fullDoc = await repository.GetDocumentoByIdAsync(doc.Id);
            if (fullDoc != null)
            {
                if (IsPlaceholderCode(fullDoc.Codigo))
                {
                    dbDocsPlaceholderByNombre[fullDoc.Nombre.Trim()] = fullDoc;
                }
                else
                {
                    dbDocsByCodigo[fullDoc.Codigo.Trim()] = fullDoc;
                }
            }
        }

        // 2. Resolver áreas
        var areas = await areaRepository.GetAllAsync();
        var areasByName = areas.ToDictionary(a => a.Nombre.Trim(), a => a.Id, StringComparer.OrdinalIgnoreCase);
        int? GetAreaId(string? areaName)
        {
            if (string.IsNullOrWhiteSpace(areaName)) return null;
            return areasByName.TryGetValue(areaName.Trim(), out var id) ? id : null;
        }

        int createdCount = 0;
        int updatedCount = 0;

        foreach (var docDto in documentos)
        {
            var codigoTrimmed = docDto.Codigo.Trim();
            var nombreTrimmed = docDto.Nombre.Trim();

            int? targetAreaId = GetAreaId(docDto.Area);

            DocumentoControl? existingDoc = null;
            bool matched = false;

            if (IsPlaceholderCode(codigoTrimmed))
            {
                if (dbDocsPlaceholderByNombre.TryGetValue(nombreTrimmed, out existingDoc))
                {
                    matched = true;
                    dbDocsPlaceholderByNombre.Remove(nombreTrimmed);
                }
            }
            else
            {
                if (dbDocsByCodigo.TryGetValue(codigoTrimmed, out existingDoc))
                {
                    matched = true;
                    dbDocsByCodigo.Remove(codigoTrimmed);
                }
                else if (dbDocsPlaceholderByNombre.TryGetValue(nombreTrimmed, out existingDoc))
                {
                    matched = true;
                    dbDocsPlaceholderByNombre.Remove(nombreTrimmed);
                }
            }

            if (matched && existingDoc != null)
            {
                // Comparar si existen cambios reales
                if (TieneCambios(existingDoc, docDto, targetAreaId))
                {
                    // Clone vigente to historical
                    var historicalClone = new DocumentoControl
                    {
                        ListadoMaestroId = existingDoc.ListadoMaestroId,
                        Codigo = existingDoc.Codigo,
                        Nombre = existingDoc.Nombre,
                        ProcesoResponsable = existingDoc.ProcesoResponsable,
                        Version = existingDoc.Version,
                        FechaDocumento = existingDoc.FechaDocumento,
                        OneDriveUrl = existingDoc.OneDriveUrl,
                        OneDriveItemId = existingDoc.OneDriveItemId,
                        ArchivoNombre = existingDoc.ArchivoNombre,
                        Uso = existingDoc.Uso,
                        TiempoRetencion = existingDoc.TiempoRetencion,
                        Proteccion = existingDoc.Proteccion,
                        Recuperacion = existingDoc.Recuperacion,
                        DisposicionFinal = existingDoc.DisposicionFinal,
                        Estado = "Histórica",
                        Observaciones = existingDoc.Observaciones,
                        ComentarioCambio = existingDoc.ComentarioCambio,
                        DatosPersonalizados = existingDoc.DatosPersonalizados,
                        AreaId = existingDoc.AreaId,
                        DocumentoOriginalId = existingDoc.Id,
                        Activo = false,
                        SolicitanteId = existingDoc.SolicitanteId,
                        EditorId = existingDoc.EditorId,
                        AprobadorId = existingDoc.AprobadorId,
                        FechaPublicacion = existingDoc.FechaPublicacion,
                        MotivoCambio = existingDoc.MotivoCambio,
                        DescripcionDetallada = existingDoc.DescripcionDetallada
                    };

                    await repository.CreateDocumentoAsync(historicalClone);

                    // Update vigente in-place
                    var original = CloneForDiff(existingDoc);
                    MapTemplateToDocumento(docDto, existingDoc, targetAreaId);
                    
                    existingDoc.FechaPublicacion = DateTime.UtcNow;
                    existingDoc.MotivoCambio = docDto.ComentarioCambio ?? "Importado mediante archivo Excel (Actualización)";

                    var updated = await repository.UpdateDocumentoAsync(existingDoc);
                    updatedCount++;

                    var cambios = BuildDiff(original, updated);
                    var log = await BuildAuditLogAsync(updated.Id, updated.Nombre, "Actualizado", docDto.ComentarioCambio ?? "Actualizado mediante importación Excel", usuarioEmail, updated, cambios);
                    await auditRepository.CreateAsync(log);
                }
            }
            else
            {
                // Create new document control record
                var newDoc = new DocumentoControl
                {
                    ListadoMaestroId = listadoId,
                    Activo = true
                };
                MapTemplateToDocumento(docDto, newDoc, targetAreaId);

                newDoc.FechaPublicacion = DateTime.UtcNow;
                newDoc.MotivoCambio = "Creación inicial por importación Excel";

                var createdDoc = await repository.CreateDocumentoAsync(newDoc);
                createdCount++;

                var log = await BuildAuditLogAsync(createdDoc.Id, createdDoc.Nombre, "Creado", docDto.ComentarioCambio ?? "Importado mediante archivo Excel", usuarioEmail, createdDoc, null);
                await auditRepository.CreateAsync(log);
            }
        }

        // 3. Desactivar documentos que estaban activos pero no vinieron en la planilla
        int deactivatedCount = 0;
        var unmatchedDocs = dbDocsByCodigo.Values.Concat(dbDocsPlaceholderByNombre.Values).ToList();
        foreach (var docToDeactivate in unmatchedDocs)
        {
            docToDeactivate.Activo = false;
            
            await repository.UpdateDocumentoAsync(docToDeactivate);
            deactivatedCount++;

            var log = await BuildAuditLogAsync(docToDeactivate.Id, docToDeactivate.Nombre, "Eliminado", "Eliminado por no estar presente en la importación Excel", usuarioEmail, docToDeactivate, null);
            await auditRepository.CreateAsync(log);
        }

    }

    private static bool TieneCambios(DocumentoControl dbDoc, TemplateDocumentoDto excelDto, int? areaId)
    {
        if (!string.Equals(dbDoc.Nombre?.Trim(), excelDto.Nombre?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.Equals(dbDoc.ProcesoResponsable?.Trim(), excelDto.ProcesoResponsable?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.Equals(dbDoc.Version?.Trim(), excelDto.Version?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        
        var excelFecha = excelDto.FechaDocumento ?? DateTime.Today;
        var dbFechaLocal = dbDoc.FechaDocumento.Kind == DateTimeKind.Utc 
            ? dbDoc.FechaDocumento.ToLocalTime().Date 
            : dbDoc.FechaDocumento.Date;
        var excelFechaLocal = excelFecha.Kind == DateTimeKind.Utc
            ? excelFecha.ToLocalTime().Date
            : excelFecha.Date;
        if (dbFechaLocal != excelFechaLocal) return true;

        if (!string.Equals(dbDoc.OneDriveUrl?.Trim(), excelDto.OneDriveUrl?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.Equals(dbDoc.OneDriveItemId?.Trim(), excelDto.OneDriveItemId?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.Equals(dbDoc.ArchivoNombre?.Trim(), excelDto.ArchivoNombre?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        
        var usoExcel = (excelDto.Uso ?? GetValoresPersonalizados(excelDto.CamposPersonalizados, "uso"))?.Trim();
        if (!string.Equals(dbDoc.Uso?.Trim(), usoExcel, StringComparison.OrdinalIgnoreCase)) return true;

        var retencionExcel = (excelDto.TiempoRetencion ?? GetValoresPersonalizados(excelDto.CamposPersonalizados, "tiemporetencion", "tiempoderetencion", "retencion"))?.Trim();
        if (!string.Equals(dbDoc.TiempoRetencion?.Trim(), retencionExcel, StringComparison.OrdinalIgnoreCase)) return true;

        var proteccionExcel = (excelDto.Proteccion ?? GetValoresPersonalizados(excelDto.CamposPersonalizados, "proteccion"))?.Trim();
        if (!string.Equals(dbDoc.Proteccion?.Trim(), proteccionExcel, StringComparison.OrdinalIgnoreCase)) return true;

        var recuperacionExcel = (excelDto.Recuperacion ?? GetValoresPersonalizados(excelDto.CamposPersonalizados, "recuperacion"))?.Trim();
        if (!string.Equals(dbDoc.Recuperacion?.Trim(), recuperacionExcel, StringComparison.OrdinalIgnoreCase)) return true;

        var dispExcel = (excelDto.DisposicionFinal ?? GetValoresPersonalizados(excelDto.CamposPersonalizados, "disposicionfinal", "disposicion"))?.Trim();
        if (!string.Equals(dbDoc.DisposicionFinal?.Trim(), dispExcel, StringComparison.OrdinalIgnoreCase)) return true;

        var obsExcel = (excelDto.Observaciones ?? GetValoresPersonalizados(excelDto.CamposPersonalizados, "observaciones"))?.Trim();
        if (!string.Equals(dbDoc.Observaciones?.Trim(), obsExcel, StringComparison.OrdinalIgnoreCase)) return true;

        if (dbDoc.AreaId != areaId) return true;

        var dbCustomJson = dbDoc.DatosPersonalizados;
        var excelCustom = excelDto.CamposPersonalizados;
        
        if (string.IsNullOrWhiteSpace(dbCustomJson))
        {
            if (excelCustom != null && excelCustom.Any()) return true;
        }
        else
        {
            if (excelCustom == null || !excelCustom.Any()) return true;
            try
            {
                var dbCustom = JsonSerializer.Deserialize<Dictionary<string, string?>>(dbCustomJson);
                if (dbCustom == null) return true;
                if (dbCustom.Count != excelCustom.Count) return true;
                foreach (var kv in excelCustom)
                {
                    if (!dbCustom.TryGetValue(kv.Key, out var dbVal) || !string.Equals(dbVal?.Trim(), kv.Value?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeFieldName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name.Trim().ToLowerInvariant()
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
    }

    private static string NormalizarClave(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return key.Trim().ToLowerInvariant()
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("ü", "u").Replace("ñ", "n").Replace("ç", "c").Replace(" ", string.Empty);
    }

    private static string? GetValoresPersonalizados(Dictionary<string, string?>? campos, params string[] clavesCandidatas)
    {
        if (campos is null || !campos.Any()) return null;

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

    private static void MapTemplateToDocumento(TemplateDocumentoDto src, DocumentoControl dest, int? areaId)
    {
        dest.Codigo = src.Codigo.Trim();
        dest.Nombre = src.Nombre.Trim();
        dest.ProcesoResponsable = src.ProcesoResponsable.Trim();
        dest.Version = string.IsNullOrWhiteSpace(src.Version) ? "1.0" : src.Version.Trim();
        dest.FechaDocumento = src.FechaDocumento ?? DateTime.Today;
        dest.OneDriveUrl = string.IsNullOrWhiteSpace(src.OneDriveUrl)
            ? "https://onedrive.live.com/placeholder"
            : src.OneDriveUrl.Trim();
        dest.OneDriveItemId = src.OneDriveItemId?.Trim();
        dest.ArchivoNombre = src.ArchivoNombre?.Trim();
        dest.Uso = (src.Uso ?? GetValoresPersonalizados(src.CamposPersonalizados, "uso"))?.Trim();
        dest.TiempoRetencion = (src.TiempoRetencion ?? GetValoresPersonalizados(src.CamposPersonalizados, "tiemporetencion", "tiempoderetencion", "retencion"))?.Trim();
        dest.Proteccion = (src.Proteccion ?? GetValoresPersonalizados(src.CamposPersonalizados, "proteccion"))?.Trim();
        dest.Recuperacion = (src.Recuperacion ?? GetValoresPersonalizados(src.CamposPersonalizados, "recuperacion"))?.Trim();
        dest.DisposicionFinal = (src.DisposicionFinal ?? GetValoresPersonalizados(src.CamposPersonalizados, "disposicionfinal", "disposicion"))?.Trim();
        dest.Estado = "Vigente";
        dest.Observaciones = (src.Observaciones ?? GetValoresPersonalizados(src.CamposPersonalizados, "observaciones"))?.Trim();
        dest.ComentarioCambio = src.ComentarioCambio?.Trim();
        dest.AreaId = areaId;
        dest.DatosPersonalizados = src.CamposPersonalizados is not null && src.CamposPersonalizados.Any()
            ? JsonSerializer.Serialize(src.CamposPersonalizados)
            : null;
    }

    public async Task<bool> DeleteListadoAsync(int id, string usuarioEmail)
    {
        var editor = await TryResolverColaboradorAsync(usuarioEmail);
        if (editor is null || (editor.Rol != Domain.Enums.RolUsuario.Admin && editor.Rol != Domain.Enums.RolUsuario.Jefe))
            throw new UnauthorizedAccessException("No tiene permisos para eliminar listados maestros.");

        return await repository.DeleteListadoAsync(id);
    }

    public async Task<DocumentoControlDto> CreateDocumentoAsync(
        CreateDocumentoControlDto dto, string usuarioEmail)
    {
        var creador = await TryResolverColaboradorAsync(usuarioEmail);
        if (creador is null || (creador.Rol != RolUsuario.Admin && creador.Rol != RolUsuario.Jefe))
            throw new UnauthorizedAccessException("No tiene permisos para crear documentos.");

        var listado = await repository.GetListadoByIdAsync(dto.ListadoMaestroId)
            ?? throw new KeyNotFoundException("Listado maestro no encontrado");

        if (string.IsNullOrWhiteSpace(dto.OneDriveUrl))
            throw new ArgumentException("La ruta del documento en OneDrive es obligatoria.");

        await ValidateCamposPersonalizados(dto.ListadoMaestroId, dto.CamposPersonalizados);

        var doc = new DocumentoControl
        {
            ListadoMaestroId = dto.ListadoMaestroId,
            Codigo = dto.Codigo.Trim(),
            Nombre = dto.Nombre.Trim(),
            ProcesoResponsable = dto.ProcesoResponsable.Trim(),
            Version = string.IsNullOrWhiteSpace(dto.Version) ? "1.0" : dto.Version.Trim(),
            FechaDocumento = dto.FechaDocumento,
            OneDriveUrl = dto.OneDriveUrl.Trim(),
            OneDriveItemId = dto.OneDriveItemId?.Trim(),
            ArchivoNombre = dto.ArchivoNombre?.Trim(),
            Uso = dto.Uso?.Trim(),
            TiempoRetencion = dto.TiempoRetencion?.Trim(),
            Proteccion = dto.Proteccion?.Trim(),
            Recuperacion = dto.Recuperacion?.Trim(),
            DisposicionFinal = dto.DisposicionFinal?.Trim(),
            Estado = dto.Estado.Trim(),
            Observaciones = dto.Observaciones?.Trim(),
            ComentarioCambio = dto.ComentarioCambio?.Trim(),
            AreaId = dto.AreaId,
            DatosPersonalizados = dto.CamposPersonalizados is not null && dto.CamposPersonalizados.Any()
                ? JsonSerializer.Serialize(dto.CamposPersonalizados)
                : null
        };

        SincronizarCamposPersonalizados(doc, dto.CamposPersonalizados);

        var created = await repository.CreateDocumentoAsync(doc);
        var log = await BuildAuditLogAsync(created.Id, created.Nombre, "Creado", dto.ComentarioCambio, usuarioEmail, created, null);
        await auditRepository.CreateAsync(log);

        return MapToDto(created);
    }

    public async Task<UpdateDocumentoResult> UpdateDocumentoAsync(
        int id, UpdateDocumentoControlDto dto, string usuarioEmail)
    {
        var existing = await repository.GetDocumentoByIdAsync(id);
        if (existing is null)
        {
            return new UpdateDocumentoResult { Exito = false, MensajeError = "Documento no encontrado." };
        }

        var editor = await TryResolverColaboradorAsync(usuarioEmail);
        if (editor is null)
        {
            return new UpdateDocumentoResult { Exito = false, MensajeError = "No tiene permisos para actualizar este documento." };
        }

        // Verificar permisos basados en ListadoMaestroPermiso
        var tienePermisoEditar = editor.Rol == RolUsuario.Admin || 
                                editor.Rol == RolUsuario.Jefe ||
                                await ValidarPermisoAsync(existing.ListadoMaestroId, editor.Id, "editar");

        if (!tienePermisoEditar)
        {
            return new UpdateDocumentoResult
            {
                Exito = false,
                RequiereSolicitud = true,
                MensajeError = "No tienes permisos para editar directamente, se requiere una solicitud de cambio."
            };
        }

        if (dto.ListadoMaestroId != existing.ListadoMaestroId &&
            editor.Rol != RolUsuario.Admin && editor.Rol != RolUsuario.Jefe)
        {
            throw new UnauthorizedAccessException("No tiene permisos para reasignar el listado maestro del documento.");
        }

        await ValidateCamposPersonalizados(dto.ListadoMaestroId, dto.CamposPersonalizados);

        // Check if editor has approval permission to auto-approve
        var tienePermisoAprobar = editor.Rol == RolUsuario.Admin || 
                                 await ValidarPermisoAsync(dto.ListadoMaestroId, editor.Id, "aprobar");

        if (!tienePermisoAprobar)
        {
            var (esJefe, areaId) = await EsJefeDeAreaAsync(editor.Id);
            if (esJefe && editor.Rol == RolUsuario.Jefe && existing.AreaId == areaId)
            {
                tienePermisoAprobar = true;
            }
        }

        DocumentoControl createdDoc;
        SolicitudCambioDocumentoControl solicitud;
        AuditLog log;

        if (dto.AutoApprove && tienePermisoAprobar)
        {
            // 1. Deactivate old vigente document
            existing.Estado = "Histórica";
            existing.Activo = false;
            await repository.UpdateDocumentoAsync(existing);

            // 2. Create new document direct as Vigente
            var newDoc = new DocumentoControl
            {
                ListadoMaestroId = dto.ListadoMaestroId,
                Codigo = dto.Codigo.Trim(),
                Nombre = dto.Nombre.Trim(),
                ProcesoResponsable = dto.ProcesoResponsable.Trim(),
                Version = string.IsNullOrWhiteSpace(dto.Version) ? existing.Version : dto.Version.Trim(),
                FechaDocumento = dto.FechaDocumento,
                OneDriveUrl = dto.OneDriveUrl.Trim(),
                OneDriveItemId = dto.OneDriveItemId?.Trim(),
                ArchivoNombre = dto.ArchivoNombre?.Trim(),
                Uso = dto.Uso?.Trim(),
                TiempoRetencion = dto.TiempoRetencion?.Trim(),
                Proteccion = dto.Proteccion?.Trim(),
                Recuperacion = dto.Recuperacion?.Trim(),
                DisposicionFinal = dto.DisposicionFinal?.Trim(),
                Estado = "Vigente",
                Observaciones = dto.Observaciones?.Trim(),
                ComentarioCambio = dto.ComentarioCambio?.Trim() ?? "Edición directa de metadatos",
                AreaId = dto.AreaId,
                DatosPersonalizados = dto.CamposPersonalizados is null
                    ? existing.DatosPersonalizados
                    : JsonSerializer.Serialize(dto.CamposPersonalizados),
                DocumentoOriginalId = existing.Id,
                Activo = true,
                SolicitanteId = editor.Id,
                EditorId = editor.Id,
                AprobadorId = editor.Id,
                FechaPublicacion = DateTime.UtcNow,
                MotivoCambio = dto.ComentarioCambio?.Trim() ?? "Edición directa por administrador/jefe",
                DescripcionDetallada = "Modificación directa de versión vigente."
            };

            SincronizarCamposPersonalizados(newDoc, dto.CamposPersonalizados);

            createdDoc = await repository.CreateDocumentoAsync(newDoc);

            // 3. Create solicitud as auto-approved and published
            solicitud = new SolicitudCambioDocumentoControl
            {
                DocumentoControlId = existing.Id,
                SolicitanteId = editor.Id,
                EditorId = editor.Id,
                AprobadorId = editor.Id,
                ComentarioSolicitud = dto.ComentarioCambio?.Trim() ?? "Edición directa de metadatos",
                DatosPropuestos = JsonSerializer.Serialize(dto),
                DatosOriginales = JsonSerializer.Serialize(MapToDto(existing)),
                EstadoPropuesta = EstadoPropuesta.Publicada,
                FechaCreacion = DateTime.UtcNow,
                FechaEdicion = DateTime.UtcNow,
                FechaResolucion = DateTime.UtcNow,
                ComentarioResolucion = "Auto-aprobado por edición directa de administrador/jefe",
                MotivoCambio = dto.ComentarioCambio?.Trim() ?? "Edición directa por administrador/jefe",
                DescripcionDetallada = "Modificación directa y auto-aprobada de versión vigente.",
                BorradorDocumentoId = createdDoc.Id
            };

            await repository.CreateSolicitudCambioAsync(solicitud);

            log = await BuildAuditLogAsync(
                createdDoc.Id,
                createdDoc.Nombre,
                "SolicitudCambioAprobada",
                $"Edición directa y auto-aprobación por {editor.Nombre} {editor.Apellido}. Nueva versión vigente ID {createdDoc.Id}.",
                usuarioEmail,
                createdDoc,
                null);
            await auditRepository.CreateAsync(log);
        }
        else
        {
            // Flow for users without approval permission (create draft & request approval)
            var borrador = new DocumentoControl
            {
                ListadoMaestroId = dto.ListadoMaestroId,
                Codigo = dto.Codigo.Trim(),
                Nombre = dto.Nombre.Trim(),
                ProcesoResponsable = dto.ProcesoResponsable.Trim(),
                Version = string.IsNullOrWhiteSpace(dto.Version) ? existing.Version : dto.Version.Trim(),
                FechaDocumento = dto.FechaDocumento,
                OneDriveUrl = dto.OneDriveUrl.Trim(),
                OneDriveItemId = dto.OneDriveItemId?.Trim(),
                ArchivoNombre = dto.ArchivoNombre?.Trim(),
                Uso = dto.Uso?.Trim(),
                TiempoRetencion = dto.TiempoRetencion?.Trim(),
                Proteccion = dto.Proteccion?.Trim(),
                Recuperacion = dto.Recuperacion?.Trim(),
                DisposicionFinal = dto.DisposicionFinal?.Trim(),
                Estado = "En Revisión",
                Observaciones = dto.Observaciones?.Trim(),
                ComentarioCambio = dto.ComentarioCambio?.Trim(),
                AreaId = dto.AreaId,
                DatosPersonalizados = dto.CamposPersonalizados is null
                    ? existing.DatosPersonalizados
                    : JsonSerializer.Serialize(dto.CamposPersonalizados),
                DocumentoOriginalId = existing.Id,
                Activo = true,
                SolicitanteId = editor.Id,
                EditorId = editor.Id
            };

            SincronizarCamposPersonalizados(borrador, dto.CamposPersonalizados);

            createdDoc = await repository.CreateDocumentoAsync(borrador);

            solicitud = new SolicitudCambioDocumentoControl
            {
                DocumentoControlId = existing.Id,
                SolicitanteId = editor.Id,
                EditorId = editor.Id,
                ComentarioSolicitud = dto.ComentarioCambio?.Trim() ?? "Edición directa de metadatos",
                DatosPropuestos = JsonSerializer.Serialize(dto),
                DatosOriginales = JsonSerializer.Serialize(MapToDto(existing)),
                EstadoPropuesta = EstadoPropuesta.PendienteAprobacion,
                FechaCreacion = DateTime.UtcNow,
                FechaEdicion = DateTime.UtcNow,
                MotivoCambio = dto.ComentarioCambio?.Trim() ?? "Edición directa por administrador/jefe",
                DescripcionDetallada = "Modificación directa de versión vigente.",
                BorradorDocumentoId = createdDoc.Id
            };

            await repository.CreateSolicitudCambioAsync(solicitud);

            log = await BuildAuditLogAsync(
                existing.Id,
                existing.Nombre,
                "SolicitudCambioCreada",
                $"Edición directa por {editor.Nombre} {editor.Apellido}. Se creó borrador en revisión ID {createdDoc.Id}.",
                usuarioEmail,
                existing,
                null);
            await auditRepository.CreateAsync(log);
        }

        return new UpdateDocumentoResult
        {
            Exito = true,
            Documento = MapToDto(createdDoc)
        };
    }

    public async Task<SolicitudCambioDocumentoControlDto> CreateSolicitudCambioAsync(int documentoId, UpdateDocumentoControlDto propuesta, string usuarioEmail)
    {
        var solicitante = await TryResolverColaboradorAsync(usuarioEmail)
            ?? throw new UnauthorizedAccessException("No tiene permisos para enviar solicitudes de cambio.");

        var documento = await repository.GetDocumentoByIdAsync(documentoId)
            ?? throw new KeyNotFoundException("Documento no encontrado.");

        if (solicitante.Rol == RolUsuario.Admin || solicitante.Rol == RolUsuario.Jefe)
            throw new InvalidOperationException("Los usuarios con permisos de administración deben actualizar el documento directamente.");

        if (await repository.ExisteSolicitudCambioPendienteAsync(documentoId, solicitante.Id))
            throw new InvalidOperationException("Ya existe una solicitud de cambio pendiente para este documento.");

        await ValidateCamposPersonalizados(propuesta.ListadoMaestroId, propuesta.CamposPersonalizados);

        var solicitud = new SolicitudCambioDocumentoControl
        {
            DocumentoControlId = documentoId,
            SolicitanteId = solicitante.Id,
            ComentarioSolicitud = propuesta.ComentarioCambio?.Trim() ?? string.Empty,
            DatosPropuestos = JsonSerializer.Serialize(propuesta),
            DatosOriginales = JsonSerializer.Serialize(MapToDto(documento)),
            EstadoPropuesta = EstadoPropuesta.PendienteRevision,
            FechaCreacion = DateTime.UtcNow,
            MotivoCambio = propuesta.ComentarioCambio?.Trim() ?? string.Empty,
            DescripcionDetallada = propuesta.Observaciones?.Trim() ?? string.Empty
        };

        var created = await repository.CreateSolicitudCambioAsync(solicitud);

        var log = await BuildAuditLogAsync(documento.Id, documento.Nombre, "SolicitudCambioCreada", created.ComentarioSolicitud, usuarioEmail, documento, null);
        await auditRepository.CreateAsync(log);

        return MapToSolicitudCambioDto(created);
    }

    public async Task<List<SolicitudCambioDocumentoControlDto>> GetSolicitudesCambioPendientesAsync(string usuarioEmail)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);
        if (colaborador is null)
            return new List<SolicitudCambioDocumentoControlDto>();

        IEnumerable<SolicitudCambioDocumentoControl> solicitudes;
        if (colaborador.Rol == RolUsuario.Admin)
        {
            solicitudes = await repository.GetSolicitudesCambioPendientesAsync();
        }
        else
        {
            var (esJefe, areaId) = await EsJefeDeAreaAsync(colaborador.Id);
            if (!esJefe)
                return new List<SolicitudCambioDocumentoControlDto>();

            solicitudes = await repository.GetSolicitudesCambioPendientesPorAreaAsync(areaId);
        }

        return solicitudes.Select(MapToSolicitudCambioDto).ToList();
    }

    public async Task<List<SolicitudCambioDocumentoControlDto>> GetSolicitudesCambioPorDocumentoAsync(int documentoId, string usuarioEmail)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);
        if (colaborador is null)
            return new List<SolicitudCambioDocumentoControlDto>();

        var solicitudes = await repository.GetSolicitudesPorDocumentoAsync(documentoId);
        if (colaborador.Rol == RolUsuario.Admin || colaborador.Rol == RolUsuario.Jefe)
        {
            return solicitudes.Select(MapToSolicitudCambioDto).ToList();
        }

        // Permitir ver toda la trazabilidad si el colaborador tiene permisos explícitos sobre el listado
        var doc = await repository.GetDocumentoByIdAsync(documentoId);
        if (doc != null)
        {
            var tienePermiso = await ValidarPermisoAsync(doc.ListadoMaestroId, colaborador.Id, "ver")
                               || await ValidarPermisoAsync(doc.ListadoMaestroId, colaborador.Id, "editar");
            if (tienePermiso)
            {
                return solicitudes.Select(MapToSolicitudCambioDto).ToList();
            }
        }

        return solicitudes
            .Where(s => s.SolicitanteId == colaborador.Id || s.EditorId == colaborador.Id || s.AprobadorId == colaborador.Id)
            .Select(MapToSolicitudCambioDto)
            .ToList();
    }

    public async Task<int> CountSolicitudesCambioPendientesAsync(string usuarioEmail)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);
        if (colaborador is null)
            return 0;

        if (colaborador.Rol == RolUsuario.Admin)
        {
            var todas = await repository.GetSolicitudesCambioPendientesAsync();
            return todas.Count(s => s.EstadoPropuesta == EstadoPropuesta.PendienteRevision
                || s.EstadoPropuesta == EstadoPropuesta.EnEdicion
                || s.EstadoPropuesta == EstadoPropuesta.PendienteAprobacion);
        }

        var (esJefe, areaId) = await EsJefeDeAreaAsync(colaborador.Id);
        if (!esJefe)
            return 0;

        return await repository.CountSolicitudesCambioPendientesPorAreaAsync(areaId);
    }

    public async Task IniciarRevisionSolicitudAsync(int solicitudId, string usuarioEmail)
    {
        var revisor = await TryResolverColaboradorAsync(usuarioEmail)
            ?? throw new UnauthorizedAccessException("No tiene permisos para revisar solicitudes.");

        var solicitud = await repository.GetSolicitudCambioByIdAsync(solicitudId)
            ?? throw new KeyNotFoundException("Solicitud de cambio no encontrada.");

        if (solicitud.EstadoPropuesta != EstadoPropuesta.PendienteRevision)
            throw new InvalidOperationException("La solicitud no está en estado pendiente de revisión.");

        var tienePermiso = revisor.Rol == RolUsuario.Admin || 
                           await ValidarPermisoAsync(solicitud.DocumentoControl.ListadoMaestroId, revisor.Id, "editar");

        if (!tienePermiso)
        {
            var (esJefe, areaId) = await EsJefeDeAreaAsync(revisor.Id);
            if (!esJefe || (revisor.Rol == RolUsuario.Jefe && solicitud.DocumentoControl.AreaId != areaId))
                throw new UnauthorizedAccessException("No tienes permisos para revisar esta solicitud.");
        }

        var originalDoc = await repository.GetDocumentoByIdAsync(solicitud.DocumentoControlId)
            ?? throw new KeyNotFoundException("Documento original no encontrado.");

        var borrador = new DocumentoControl
        {
            ListadoMaestroId = originalDoc.ListadoMaestroId,
            Codigo = originalDoc.Codigo,
            Nombre = originalDoc.Nombre,
            ProcesoResponsable = originalDoc.ProcesoResponsable,
            Version = originalDoc.Version,
            FechaDocumento = originalDoc.FechaDocumento,
            OneDriveUrl = originalDoc.OneDriveUrl,
            OneDriveItemId = originalDoc.OneDriveItemId,
            ArchivoNombre = originalDoc.ArchivoNombre,
            Uso = originalDoc.Uso,
            TiempoRetencion = originalDoc.TiempoRetencion,
            Proteccion = originalDoc.Proteccion,
            Recuperacion = originalDoc.Recuperacion,
            DisposicionFinal = originalDoc.DisposicionFinal,
            Estado = "En Revisión",
            Observaciones = originalDoc.Observaciones,
            ComentarioCambio = originalDoc.ComentarioCambio,
            DatosPersonalizados = originalDoc.DatosPersonalizados,
            AreaId = originalDoc.AreaId,
            DocumentoOriginalId = originalDoc.Id,
            Activo = true
        };

        var createdBorrador = await repository.CreateDocumentoAsync(borrador);

        solicitud.EstadoPropuesta = EstadoPropuesta.EnEdicion;
        solicitud.RevisorId = revisor.Id;
        solicitud.FechaRevision = DateTime.UtcNow;
        solicitud.BorradorDocumentoId = createdBorrador.Id;

        await repository.UpdateSolicitudCambioAsync(solicitud);

        var log = await BuildAuditLogAsync(originalDoc.Id, originalDoc.Nombre, "SolicitudRevisionIniciada", $"Revisión iniciada por {revisor.Nombre} {revisor.Apellido}. Se creó el borrador ID {createdBorrador.Id}.", usuarioEmail, originalDoc, null);
        await auditRepository.CreateAsync(log);
    }

    public async Task UpdateBorradorDocumentoAsync(int solicitudId, UpdateDocumentoControlDto borradorDto, string usuarioEmail)
    {
        var editor = await TryResolverColaboradorAsync(usuarioEmail)
            ?? throw new UnauthorizedAccessException("No tiene permisos para editar.");

        var solicitud = await repository.GetSolicitudCambioByIdAsync(solicitudId)
            ?? throw new KeyNotFoundException("Solicitud de cambio no encontrada.");

        if (solicitud.EstadoPropuesta != EstadoPropuesta.EnEdicion)
            throw new InvalidOperationException("La solicitud no está en fase de edición.");

        if (!solicitud.BorradorDocumentoId.HasValue)
            throw new InvalidOperationException("No hay un borrador asociado a esta solicitud.");

        var tienePermiso = editor.Rol == RolUsuario.Admin || 
                           solicitud.EditorId == editor.Id || 
                           solicitud.SolicitanteId == editor.Id ||
                           await ValidarPermisoAsync(solicitud.DocumentoControl.ListadoMaestroId, editor.Id, "editar");

        if (!tienePermiso)
        {
            var (esJefe, areaId) = await EsJefeDeAreaAsync(editor.Id);
            if (!esJefe || (editor.Rol == RolUsuario.Jefe && solicitud.DocumentoControl.AreaId != areaId))
                throw new UnauthorizedAccessException("No tienes permisos para editar este borrador.");
        }

        var borrador = await repository.GetDocumentoByIdAsync(solicitud.BorradorDocumentoId.Value)
            ?? throw new KeyNotFoundException("Borrador no encontrado.");

        await ValidateCamposPersonalizados(borradorDto.ListadoMaestroId, borradorDto.CamposPersonalizados);

        borrador.ListadoMaestroId = borradorDto.ListadoMaestroId;
        borrador.Codigo = borradorDto.Codigo.Trim();
        borrador.Nombre = borradorDto.Nombre.Trim();
        borrador.ProcesoResponsable = borradorDto.ProcesoResponsable.Trim();
        borrador.Version = string.IsNullOrWhiteSpace(borradorDto.Version) ? borrador.Version : borradorDto.Version.Trim();
        borrador.FechaDocumento = borradorDto.FechaDocumento;
        borrador.OneDriveUrl = borradorDto.OneDriveUrl.Trim();
        borrador.OneDriveItemId = borradorDto.OneDriveItemId?.Trim();
        borrador.ArchivoNombre = borradorDto.ArchivoNombre?.Trim();
        borrador.Uso = borradorDto.Uso?.Trim();
        borrador.TiempoRetencion = borradorDto.TiempoRetencion?.Trim();
        borrador.Proteccion = borradorDto.Proteccion?.Trim();
        borrador.Recuperacion = borradorDto.Recuperacion?.Trim();
        borrador.DisposicionFinal = borradorDto.DisposicionFinal?.Trim();
        borrador.Observaciones = borradorDto.Observaciones?.Trim();
        borrador.ComentarioCambio = borradorDto.ComentarioCambio?.Trim();
        borrador.AreaId = borradorDto.AreaId;
        borrador.DatosPersonalizados = borradorDto.CamposPersonalizados is null
            ? null
            : JsonSerializer.Serialize(borradorDto.CamposPersonalizados);

        SincronizarCamposPersonalizados(borrador, borradorDto.CamposPersonalizados);

        await repository.UpdateDocumentoAsync(borrador);

        solicitud.DatosPropuestos = JsonSerializer.Serialize(borradorDto);

        if (solicitud.EditorId != editor.Id)
        {
            solicitud.EditorId = editor.Id;
        }
        await repository.UpdateSolicitudCambioAsync(solicitud);
    }

    public async Task EnviarAAprobacionAsync(int solicitudId, string usuarioEmail)
    {
        var editor = await TryResolverColaboradorAsync(usuarioEmail)
            ?? throw new UnauthorizedAccessException("No tiene permisos para realizar esta acción.");

        var solicitud = await repository.GetSolicitudCambioByIdAsync(solicitudId)
            ?? throw new KeyNotFoundException("Solicitud de cambio no encontrada.");

        if (solicitud.EstadoPropuesta != EstadoPropuesta.EnEdicion)
            throw new InvalidOperationException("La solicitud no está en fase de edición.");

        solicitud.EstadoPropuesta = EstadoPropuesta.PendienteAprobacion;
        solicitud.FechaEdicion = DateTime.UtcNow;

        await repository.UpdateSolicitudCambioAsync(solicitud);

        var log = await BuildAuditLogAsync(solicitud.DocumentoControlId, solicitud.DocumentoControl?.Nombre ?? "—", "SolicitudEnviadaAAprobacion", "Edición del borrador finalizada. Enviado a aprobación.", usuarioEmail, solicitud.DocumentoControl ?? new DocumentoControl { Id = solicitud.DocumentoControlId, Nombre = "—" }, null);
        await auditRepository.CreateAsync(log);
    }

    public async Task AprobarSolicitudCambioAsync(int solicitudId, string? comentarios, string usuarioEmail)
    {
        var aprobador = await TryResolverColaboradorAsync(usuarioEmail);
        if (aprobador is null)
            throw new UnauthorizedAccessException("No tiene permisos para aprobar solicitudes.");

        var solicitud = await repository.GetSolicitudCambioByIdAsync(solicitudId)
            ?? throw new KeyNotFoundException("Solicitud de cambio no encontrada.");

        if (solicitud.EstadoPropuesta != EstadoPropuesta.PendienteAprobacion && solicitud.EstadoPropuesta != EstadoPropuesta.PendienteRevision)
            throw new InvalidOperationException("La solicitud no está lista para aprobación.");

        var tienePermisoAprobar = aprobador.Rol == RolUsuario.Admin || 
                                 await ValidarPermisoAsync(solicitud.DocumentoControl.ListadoMaestroId, aprobador.Id, "aprobar");

        if (!tienePermisoAprobar)
        {
            var (esJefe, areaId) = await EsJefeDeAreaAsync(aprobador.Id);
            if (!esJefe || (aprobador.Rol == RolUsuario.Jefe && solicitud.DocumentoControl.AreaId != areaId))
                throw new UnauthorizedAccessException("No tienes permisos para aprobar esta solicitud.");
        }

        var documento = await repository.GetDocumentoByIdAsync(solicitud.DocumentoControlId)
            ?? throw new KeyNotFoundException("Documento no encontrado.");

        var originalDocClone = CloneForDiff(documento);

        // 1. Update previous vigente to Historical
        documento.Estado = "Histórica";
        documento.Activo = false;
        await repository.UpdateDocumentoAsync(documento);

        DocumentoControl? finalVigente = null;

        if (solicitud.BorradorDocumentoId.HasValue)
        {
            var borrador = await repository.GetDocumentoByIdAsync(solicitud.BorradorDocumentoId.Value);
            if (borrador != null)
            {
                // Promote the borrador (which was in state "En Revisión") to Vigente
                borrador.Estado = "Vigente";
                borrador.Activo = true;
                borrador.SolicitanteId = solicitud.SolicitanteId;
                borrador.EditorId = solicitud.EditorId ?? solicitud.SolicitanteId;
                borrador.AprobadorId = aprobador.Id;
                borrador.FechaPublicacion = DateTime.UtcNow;
                borrador.MotivoCambio = solicitud.MotivoCambio ?? solicitud.ComentarioSolicitud;
                borrador.DescripcionDetallada = solicitud.DescripcionDetallada;

                finalVigente = await repository.UpdateDocumentoAsync(borrador);
            }
        }

        if (finalVigente == null)
        {
            // If no borrador existed (approved directly), create a new Vigente version record using proposal data
            var propuestaDto = JsonSerializer.Deserialize<UpdateDocumentoControlDto>(solicitud.DatosPropuestos)
                ?? throw new InvalidOperationException("Los datos de la solicitud son inválidos.");

            await ValidateCamposPersonalizados(propuestaDto.ListadoMaestroId, propuestaDto.CamposPersonalizados);

            var newDoc = new DocumentoControl
            {
                ListadoMaestroId = propuestaDto.ListadoMaestroId,
                Codigo = propuestaDto.Codigo.Trim(),
                Nombre = propuestaDto.Nombre.Trim(),
                ProcesoResponsable = propuestaDto.ProcesoResponsable.Trim(),
                Version = string.IsNullOrWhiteSpace(propuestaDto.Version) ? documento.Version : propuestaDto.Version.Trim(),
                FechaDocumento = propuestaDto.FechaDocumento,
                OneDriveUrl = propuestaDto.OneDriveUrl.Trim(),
                OneDriveItemId = propuestaDto.OneDriveItemId?.Trim(),
                ArchivoNombre = propuestaDto.ArchivoNombre?.Trim(),
                Uso = propuestaDto.Uso?.Trim(),
                TiempoRetencion = propuestaDto.TiempoRetencion?.Trim(),
                Proteccion = propuestaDto.Proteccion?.Trim(),
                Recuperacion = propuestaDto.Recuperacion?.Trim(),
                DisposicionFinal = propuestaDto.DisposicionFinal?.Trim(),
                Estado = "Vigente",
                Observaciones = propuestaDto.Observaciones?.Trim(),
                ComentarioCambio = solicitud.ComentarioSolicitud,
                AreaId = propuestaDto.AreaId,
                DatosPersonalizados = propuestaDto.CamposPersonalizados is null
                    ? documento.DatosPersonalizados
                    : JsonSerializer.Serialize(propuestaDto.CamposPersonalizados),
                DocumentoOriginalId = documento.Id,
                Activo = true,
                SolicitanteId = solicitud.SolicitanteId,
                EditorId = solicitud.EditorId ?? solicitud.SolicitanteId,
                AprobadorId = aprobador.Id,
                FechaPublicacion = DateTime.UtcNow,
                MotivoCambio = solicitud.MotivoCambio ?? solicitud.ComentarioSolicitud,
                DescripcionDetallada = solicitud.DescripcionDetallada
            };

            SincronizarCamposPersonalizados(newDoc, propuestaDto.CamposPersonalizados);

            finalVigente = await repository.CreateDocumentoAsync(newDoc);
            solicitud.BorradorDocumentoId = finalVigente.Id;
        }

        // 2. Update request status
        solicitud.EstadoPropuesta = EstadoPropuesta.Publicada;
        solicitud.AprobadorId = aprobador.Id;
        solicitud.FechaResolucion = DateTime.UtcNow;
        solicitud.ComentarioResolucion = comentarios;

        await repository.UpdateSolicitudCambioAsync(solicitud);

        var cambios = BuildDiff(originalDocClone, finalVigente);
        var log = await BuildAuditLogAsync(finalVigente.Id, finalVigente.Nombre, "SolicitudCambioAprobada", solicitud.ComentarioSolicitud, usuarioEmail, finalVigente, cambios);
        await auditRepository.CreateAsync(log);
    }

    public async Task RechazarSolicitudCambioAsync(int solicitudId, string motivo, string usuarioEmail)
    {
        var aprobador = await TryResolverColaboradorAsync(usuarioEmail);
        if (aprobador is null)
            throw new UnauthorizedAccessException("No tiene permisos para rechazar solicitudes.");

        var solicitud = await repository.GetSolicitudCambioByIdAsync(solicitudId)
            ?? throw new KeyNotFoundException("Solicitud de cambio no encontrada.");

        if (solicitud.EstadoPropuesta == EstadoPropuesta.Aprobada || solicitud.EstadoPropuesta == EstadoPropuesta.Publicada || solicitud.EstadoPropuesta == EstadoPropuesta.Rechazada)
            throw new InvalidOperationException("La solicitud ya fue resuelta.");

        var (esJefe, areaId) = await EsJefeDeAreaAsync(aprobador.Id);
        if (aprobador.Rol != RolUsuario.Admin && !esJefe)
            throw new UnauthorizedAccessException("No tienes permisos para rechazar esta solicitud.");

        if (aprobador.Rol == RolUsuario.Jefe && solicitud.DocumentoControl.AreaId != areaId)
            throw new UnauthorizedAccessException("No tienes permisos para rechazar esta solicitud.");

        // Update the borrador (if any) to state "Rechazada" and set it to Activo = false
        if (solicitud.BorradorDocumentoId.HasValue)
        {
            var borrador = await repository.GetDocumentoByIdAsync(solicitud.BorradorDocumentoId.Value);
            if (borrador != null)
            {
                borrador.Estado = "Rechazada";
                borrador.Activo = false;
                await repository.UpdateDocumentoAsync(borrador);
            }
        }

        solicitud.EstadoPropuesta = EstadoPropuesta.Rechazada;
        solicitud.AprobadorId = aprobador.Id;
        solicitud.ComentarioResolucion = motivo?.Trim();
        solicitud.FechaResolucion = DateTime.UtcNow;

        await repository.UpdateSolicitudCambioAsync(solicitud);

        var documento = await repository.GetDocumentoByIdAsync(solicitud.DocumentoControlId);
        var log = await BuildAuditLogAsync(solicitud.DocumentoControlId, documento?.Nombre ?? "—", "SolicitudCambioRechazada", motivo, usuarioEmail, documento ?? new DocumentoControl { Id = solicitud.DocumentoControlId, Nombre = "—" }, null);
        await auditRepository.CreateAsync(log);
    }

    private async Task<(bool EsJefe, int AreaId)> EsJefeDeAreaAsync(int colaboradorId)
    {
        var areas = await areaRepository.GetAllAsync();
        var area = areas.FirstOrDefault(a => a.JefeId == colaboradorId);
        return area is not null ? (true, area.Id) : (false, 0);
    }

    private static SolicitudCambioDocumentoControlDto MapToSolicitudCambioDto(SolicitudCambioDocumentoControl solicitud) => new()
    {
        Id = solicitud.Id,
        DocumentoControlId = solicitud.DocumentoControlId,
        DocumentoControlNombre = solicitud.DocumentoControl?.Nombre ?? string.Empty,
        DocumentoControlCodigo = solicitud.DocumentoControl?.Codigo ?? string.Empty,
        SolicitanteId = solicitud.SolicitanteId,
        SolicitanteNombre = string.IsNullOrWhiteSpace(solicitud.Solicitante?.Nombre) && string.IsNullOrWhiteSpace(solicitud.Solicitante?.Apellido)
            ? string.Empty
            : $"{solicitud.Solicitante?.Nombre} {solicitud.Solicitante?.Apellido}".Trim(),
        EstadoPropuesta = solicitud.EstadoPropuesta.ToString(),
        ComentarioSolicitud = solicitud.ComentarioSolicitud,
        ComentarioResolucion = solicitud.ComentarioResolucion,
        FechaCreacion = solicitud.FechaCreacion,
        FechaEdicion = solicitud.FechaEdicion,
        FechaResolucion = solicitud.FechaResolucion,
        DatosPropuestos = solicitud.DatosPropuestos,
        DatosOriginales = solicitud.DatosOriginales,
        AprobadorId = solicitud.AprobadorId,
        AprobadorNombre = !string.IsNullOrWhiteSpace(solicitud.Aprobador?.Nombre) || !string.IsNullOrWhiteSpace(solicitud.Aprobador?.Apellido)
            ? $"{solicitud.Aprobador?.Nombre} {solicitud.Aprobador?.Apellido}".Trim()
            : (solicitud.AprobadorId.HasValue && solicitud.AprobadorId == solicitud.EditorId && solicitud.Editor != null
                ? $"{solicitud.Editor.Nombre} {solicitud.Editor.Apellido}".Trim()
                : (solicitud.AprobadorId.HasValue && solicitud.AprobadorId == solicitud.SolicitanteId && solicitud.Solicitante != null
                    ? $"{solicitud.Solicitante.Nombre} {solicitud.Solicitante.Apellido}".Trim()
                    : null)),
        EditorNombre = string.IsNullOrWhiteSpace(solicitud.Editor?.Nombre) && string.IsNullOrWhiteSpace(solicitud.Editor?.Apellido)
            ? null
            : $"{solicitud.Editor?.Nombre} {solicitud.Editor?.Apellido}".Trim(),
        
        RevisorId = solicitud.RevisorId,
        RevisorNombre = solicitud.Revisor != null ? $"{solicitud.Revisor.Nombre} {solicitud.Revisor.Apellido}" : null,
        FechaRevision = solicitud.FechaRevision,
        ObservacionesRevision = solicitud.ObservacionesRevision,
        MotivoCambio = solicitud.MotivoCambio,
        DescripcionDetallada = solicitud.DescripcionDetallada,
        BorradorDocumentoId = solicitud.BorradorDocumentoId
    };

    public async Task<List<DocumentoControlDto>> GetHistorialAsync(int documentoId, string usuarioEmail)
    {
        var mainDoc = await repository.GetDocumentoByIdAsync(documentoId);
        if (mainDoc is null) return new List<DocumentoControlDto>();

        if (!await TienePermisoAccesoAsync(mainDoc.ListadoMaestroId, usuarioEmail, "ver"))
            throw new UnauthorizedAccessException("No tiene permisos para ver el historial de este documento.");

        var docs = await repository.GetDocumentosIgnoreFiltersAsync(documentoId);
        return docs.Select(MapToDto).ToList();
    }

    private async Task<AuditLog> BuildAuditLogAsync(
        int entidadId,
        string entidadNombre,
        string accion,
        string? observaciones,
        string usuarioEmail,
        DocumentoControl documento,
        Dictionary<string, object?>? cambios)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);

        return new AuditLog
        {
            EntidadTipo = EntidadTipo,
            EntidadId = entidadId,
            EntidadNombre = entidadNombre,
            Accion = accion,
            ColaboradorId = colaborador?.Id,
            ColaboradorNombre = colaborador is null
                ? usuarioEmail
                : $"{colaborador.Nombre} {colaborador.Apellido}",
            FechaHora = DateTime.UtcNow,
            Observaciones = observaciones,
            CamposModificados = cambios is null ? null : JsonSerializer.Serialize(cambios)
        };
    }

    private static DocumentoControl CloneForDiff(DocumentoControl source) => new()
    {
        Id = source.Id,
        ListadoMaestroId = source.ListadoMaestroId,
        Codigo = source.Codigo,
        Nombre = source.Nombre,
        ProcesoResponsable = source.ProcesoResponsable,
        Version = source.Version,
        FechaDocumento = source.FechaDocumento,
        OneDriveUrl = source.OneDriveUrl,
        OneDriveItemId = source.OneDriveItemId,
        ArchivoNombre = source.ArchivoNombre,
        Uso = source.Uso,
        TiempoRetencion = source.TiempoRetencion,
        Proteccion = source.Proteccion,
        Recuperacion = source.Recuperacion,
        DisposicionFinal = source.DisposicionFinal,
        Estado = source.Estado,
        Observaciones = source.Observaciones,
        ComentarioCambio = source.ComentarioCambio,
        AreaId = source.AreaId,
        ListadoMaestro = source.ListadoMaestro,
        Area = source.Area
    };

    private static Dictionary<string, object?> BuildDiff(
        DocumentoControl original,
        DocumentoControl actualizado)
    {
        var cambios = new Dictionary<string, object?>();

        void Comparar(string campo, object? antes, object? despues)
        {
            var antesText = antes?.ToString() ?? string.Empty;
            var despuesText = despues?.ToString() ?? string.Empty;
            if (antesText != despuesText)
                cambios[campo] = new { de = antesText, a = despuesText };
        }

        Comparar("ListadoMaestroId", original.ListadoMaestroId, actualizado.ListadoMaestroId);
        Comparar("Codigo", original.Codigo, actualizado.Codigo);
        Comparar("Nombre", original.Nombre, actualizado.Nombre);
        Comparar("ProcesoResponsable", original.ProcesoResponsable, actualizado.ProcesoResponsable);
        Comparar("Version", original.Version, actualizado.Version);
        Comparar("FechaDocumento", original.FechaDocumento.ToString("o"), actualizado.FechaDocumento.ToString("o"));
        Comparar("OneDriveUrl", original.OneDriveUrl, actualizado.OneDriveUrl);
        Comparar("OneDriveItemId", original.OneDriveItemId, actualizado.OneDriveItemId);
        Comparar("ArchivoNombre", original.ArchivoNombre, actualizado.ArchivoNombre);
        Comparar("Uso", original.Uso, actualizado.Uso);
        Comparar("TiempoRetencion", original.TiempoRetencion, actualizado.TiempoRetencion);
        Comparar("Proteccion", original.Proteccion, actualizado.Proteccion);
        Comparar("Recuperacion", original.Recuperacion, actualizado.Recuperacion);
        Comparar("DisposicionFinal", original.DisposicionFinal, actualizado.DisposicionFinal);
        Comparar("Estado", original.Estado, actualizado.Estado);
        Comparar("Observaciones", original.Observaciones, actualizado.Observaciones);
        Comparar("ComentarioCambio", original.ComentarioCambio, actualizado.ComentarioCambio);
        Comparar("AreaId", original.AreaId?.ToString(), actualizado.AreaId?.ToString());

        return cambios;
    }

    private async Task<Domain.Entities.Colaborador?> TryResolverColaboradorAsync(string email)
    {
        try
        {
            return await colaboradorRepository.GetByEmailAsync(email);
        }
        catch
        {
            return null;
        }
    }

    private static DocumentoControlDto MapToDto(DocumentoControl d) => new()
    {
        Id = d.Id,
        ListadoMaestroId = d.ListadoMaestroId,
        ListadoMaestroNombre = d.ListadoMaestro?.Nombre,
        Codigo = d.Codigo,
        Nombre = d.Nombre,
        ProcesoResponsable = d.ProcesoResponsable,
        Version = d.Version,
        FechaDocumento = d.FechaDocumento,
        OneDriveUrl = d.OneDriveUrl,
        Estado = d.Estado,
        AreaId = d.AreaId,
        AreaNombre = d.Area?.Nombre,
        Uso = d.Uso,
        TiempoRetencion = d.TiempoRetencion,
        Proteccion = d.Proteccion,
        Recuperacion = d.Recuperacion,
        DisposicionFinal = d.DisposicionFinal,
        Observaciones = d.Observaciones,
        ComentarioCambio = d.ComentarioCambio,
        ArchivoNombre = d.ArchivoNombre,
        CamposPersonalizados = string.IsNullOrWhiteSpace(d.DatosPersonalizados)
            ? new Dictionary<string, string?>()
            : JsonSerializer.Deserialize<Dictionary<string, string?>>(d.DatosPersonalizados) ?? new Dictionary<string, string?>(),

        DocumentoOriginalId = d.DocumentoOriginalId,
        SolicitanteId = d.SolicitanteId,
        SolicitanteNombre = d.Solicitante != null ? $"{d.Solicitante.Nombre} {d.Solicitante.Apellido}" : null,
        EditorId = d.EditorId,
        EditorNombre = d.Editor != null ? $"{d.Editor.Nombre} {d.Editor.Apellido}" : null,
        AprobadorId = d.AprobadorId,
        AprobadorNombre = d.Aprobador != null ? $"{d.Aprobador.Nombre} {d.Aprobador.Apellido}" : null,
        FechaPublicacion = d.FechaPublicacion,
        MotivoCambio = d.MotivoCambio,
        DescripcionDetallada = d.DescripcionDetallada
    };

    private static DocumentoControlDetalleDto MapToDetalleDto(DocumentoControl d) => new()
    {
        Id = d.Id,
        ListadoMaestroId = d.ListadoMaestroId,
        ListadoMaestroNombre = d.ListadoMaestro?.Nombre,
        Codigo = d.Codigo,
        Nombre = d.Nombre,
        ProcesoResponsable = d.ProcesoResponsable,
        Version = d.Version,
        FechaDocumento = d.FechaDocumento,
        OneDriveUrl = d.OneDriveUrl,
        OneDriveItemId = d.OneDriveItemId,
        ArchivoNombre = d.ArchivoNombre,
        Uso = d.Uso,
        TiempoRetencion = d.TiempoRetencion,
        Proteccion = d.Proteccion,
        Recuperacion = d.Recuperacion,
        DisposicionFinal = d.DisposicionFinal,
        Estado = d.Estado,
        Observaciones = d.Observaciones,
        ComentarioCambio = d.ComentarioCambio,
        AreaId = d.AreaId,
        AreaNombre = d.Area?.Nombre,
        ListadoMaestroDescripcion = d.ListadoMaestro?.Descripcion,
        CamposPersonalizados = string.IsNullOrWhiteSpace(d.DatosPersonalizados)
            ? new Dictionary<string, string?>()
            : JsonSerializer.Deserialize<Dictionary<string, string?>>(d.DatosPersonalizados) ?? new Dictionary<string, string?>(),

        DocumentoOriginalId = d.DocumentoOriginalId,
        SolicitanteId = d.SolicitanteId,
        SolicitanteNombre = d.Solicitante != null ? $"{d.Solicitante.Nombre} {d.Solicitante.Apellido}" : null,
        EditorId = d.EditorId,
        EditorNombre = d.Editor != null ? $"{d.Editor.Nombre} {d.Editor.Apellido}" : null,
        AprobadorId = d.AprobadorId,
        AprobadorNombre = d.Aprobador != null ? $"{d.Aprobador.Nombre} {d.Aprobador.Apellido}" : null,
        FechaPublicacion = d.FechaPublicacion,
        MotivoCambio = d.MotivoCambio,
        DescripcionDetallada = d.DescripcionDetallada
    };

    private static ListadoMaestroDto MapToListadoDto(ListadoMaestro listado) => new()
    {
        Id = listado.Id,
        Nombre = listado.Nombre,
        Descripcion = listado.Descripcion,
        Campos = listado.Campos?.OrderBy(c => c.Orden).Select(MapToCampoDto).ToList() ?? new List<DocumentoControlCampoDto>()
    };

    private static DocumentoControlCampoDefinicion MapToCampoDefinicion(DocumentoControlCampoDto dto, int listadoId) => new()
    {
        ListadoMaestroId = listadoId,
        CampoClave = dto.CampoClave,
        Nombre = dto.Nombre.Trim(),
        Tipo = string.IsNullOrWhiteSpace(dto.Tipo) ? "Texto" : dto.Tipo.Trim(),
        Requerido = dto.Requerido,
        EsPredeterminado = dto.EsPredeterminado,
        Orden = dto.Orden,
        OpcionesJson = string.IsNullOrWhiteSpace(dto.Opciones)
            ? null
            : JsonSerializer.Serialize(dto.Opciones.Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).ToList())
    };

    private static DocumentoControlCampoDto MapToCampoDto(DocumentoControlCampoDefinicion c) => new()
    {
        CampoClave = c.CampoClave,
        Nombre = c.Nombre,
        Tipo = c.Tipo,
        Requerido = c.Requerido,
        EsPredeterminado = c.EsPredeterminado,
        Orden = c.Orden,
        Opciones = string.IsNullOrWhiteSpace(c.OpcionesJson)
            ? null
            : string.Join(", ", JsonSerializer.Deserialize<List<string>>(c.OpcionesJson) ?? new List<string>())
    };

    private static List<DocumentoControlCampoDefinicion> GenerateDefaultCampoDefiniciones(int listadoId) => new()
    {
        new DocumentoControlCampoDefinicion
        {
            ListadoMaestroId = listadoId,
            CampoClave = "Codigo",
            Nombre = "Código",
            Tipo = "Texto",
            Requerido = true,
            EsPredeterminado = true,
            Orden = 1
        },
        new DocumentoControlCampoDefinicion
        {
            ListadoMaestroId = listadoId,
            CampoClave = "Nombre",
            Nombre = "Nombre",
            Tipo = "Texto",
            Requerido = true,
            EsPredeterminado = true,
            Orden = 2
        },
        new DocumentoControlCampoDefinicion
        {
            ListadoMaestroId = listadoId,
            CampoClave = "ProcesoResponsable",
            Nombre = "Proceso responsable",
            Tipo = "Texto",
            Requerido = true,
            EsPredeterminado = true,
            Orden = 3
        },
        new DocumentoControlCampoDefinicion
        {
            ListadoMaestroId = listadoId,
            CampoClave = "Version",
            Nombre = "Versión",
            Tipo = "Texto",
            Requerido = false,
            EsPredeterminado = true,
            Orden = 4
        },
        new DocumentoControlCampoDefinicion
        {
            ListadoMaestroId = listadoId,
            CampoClave = "FechaDocumento",
            Nombre = "Fecha de documento",
            Tipo = "Fecha",
            Requerido = true,
            EsPredeterminado = true,
            Orden = 5
        },
        new DocumentoControlCampoDefinicion
        {
            ListadoMaestroId = listadoId,
            CampoClave = "OneDriveUrl",
            Nombre = "Archivo OneDrive",
            Tipo = "Texto",
            Requerido = true,
            EsPredeterminado = true,
            Orden = 6
        },
        new DocumentoControlCampoDefinicion
        {
            ListadoMaestroId = listadoId,
            CampoClave = "Estado",
            Nombre = "Estado",
            Tipo = "Texto",
            Requerido = true,
            EsPredeterminado = true,
            Orden = 7
        },
        new DocumentoControlCampoDefinicion
        {
            ListadoMaestroId = listadoId,
            CampoClave = "Area",
            Nombre = "Área",
            Tipo = "Texto",
            Requerido = false,
            EsPredeterminado = true,
            Orden = 8
        }
    };

    private async Task ValidateCamposPersonalizados(int listadoId, Dictionary<string, string?>? camposPersonalizados)
    {
        var definiciones = await repository.GetCamposPorListadoAsync(listadoId);
        if (definiciones is null || !definiciones.Any())
            return;

        foreach (var definicion in definiciones.Where(d => d.Requerido && !d.EsPredeterminado))
        {
            if (camposPersonalizados is null || !camposPersonalizados.TryGetValue(definicion.CampoClave, out var valor) || string.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException($"El campo '{definicion.Nombre}' es obligatorio.");
            }

            if (definicion.Tipo == "Lista")
            {
                var opciones = GetOpciones(definicion);
                if (opciones.Any() && !opciones.Contains(valor, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"El valor '{valor}' no es válido para el campo '{definicion.Nombre}'.");
                }
            }

            if (definicion.Tipo == "Numero" && !string.IsNullOrWhiteSpace(valor) && !decimal.TryParse(valor, out _))
            {
                throw new ArgumentException($"El campo '{definicion.Nombre}' requiere un número válido.");
            }
        }
    }

    private static List<string> GetOpciones(DocumentoControlCampoDefinicion definicion)
    {
        if (string.IsNullOrWhiteSpace(definicion.OpcionesJson))
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>(definicion.OpcionesJson) ?? new List<string>();
    }

    // ────── Métodos de Permisos ──────

    public async Task<List<ListadoMaestroPermisoDto>> GetListadoPermisosAsync(int listadoId, string usuarioEmail)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);
        if (colaborador is null || (colaborador.Rol != RolUsuario.Admin && colaborador.Rol != RolUsuario.Jefe))
            throw new UnauthorizedAccessException("No tiene permisos para consultar los accesos de este listado.");

        var permisos = await repository.GetPermisosPorListadoAsync(listadoId);
        return permisos.Select(MapToPermisoDto).ToList();
    }

    public async Task<ListadoMaestroPermisoDto?> GetListadoPermisosActualUsuarioAsync(int listadoId, string usuarioEmail)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);
        if (colaborador is null)
            return null;

        var permisos = await repository.GetPermisosPorListadoAsync(listadoId);
        var permisosUsuario = permisos.Where(p => 
            p.ColaboradorId == colaborador.Id || 
            (p.AreaId.HasValue && p.AreaId == colaborador.AreaId)
        ).ToList();

        if (!permisosUsuario.Any())
            return null;

        return new ListadoMaestroPermisoDto
        {
            ColaboradorId = colaborador.Id,
            ColaboradorNombre = $"{colaborador.Nombre} {colaborador.Apellido}".Trim(),
            ColaboradorEmail = colaborador.Email,
            PuedeVer = permisosUsuario.Any(p => p.PuedeVer),
            PuedeEditar = permisosUsuario.Any(p => p.PuedeEditar),
            PuedeAprobar = permisosUsuario.Any(p => p.PuedeAprobar)
        };
    }

    public async Task UpdateListadoPermisosAsync(int listadoId, IEnumerable<ListadoMaestroPermisoUpdateDto> permisos, string usuarioEmail)
    {
        var administrador = await TryResolverColaboradorAsync(usuarioEmail);
        if (administrador is null || administrador.Rol != RolUsuario.Admin)
            throw new UnauthorizedAccessException("Solo administradores pueden configurar permisos.");

        var listado = await repository.GetListadoByIdAsync(listadoId);
        if (listado is null)
            throw new KeyNotFoundException("Listado maestro no encontrado.");

        // Eliminar permisos antiguos
        await repository.DeletePermisosPorListadoAsync(listadoId);

        // Crear nuevos permisos
        var nuevosPermisos = new List<ListadoMaestroPermiso>();
        foreach (var permisoDto in permisos)
        {
            if (permisoDto.ColaboradorId.HasValue && permisoDto.ColaboradorId > 0)
            {
                var colaborador = await colaboradorRepository.GetByIdAsync(permisoDto.ColaboradorId.Value);
                if (colaborador is null)
                    continue;

                nuevosPermisos.Add(new ListadoMaestroPermiso
                {
                    ListadoMaestroId = listadoId,
                    ColaboradorId = permisoDto.ColaboradorId,
                    AreaId = null,
                    PuedeVer = permisoDto.PuedeVer,
                    PuedeEditar = permisoDto.PuedeEditar,
                    PuedeAprobar = permisoDto.PuedeAprobar
                });
            }
            else if (permisoDto.AreaId.HasValue && permisoDto.AreaId > 0)
            {
                var area = await areaRepository.GetByIdAsync(permisoDto.AreaId.Value);
                if (area is null)
                    continue;

                nuevosPermisos.Add(new ListadoMaestroPermiso
                {
                    ListadoMaestroId = listadoId,
                    ColaboradorId = null,
                    AreaId = permisoDto.AreaId,
                    PuedeVer = permisoDto.PuedeVer,
                    PuedeEditar = permisoDto.PuedeEditar,
                    PuedeAprobar = permisoDto.PuedeAprobar
                });
            }
        }

        if (nuevosPermisos.Any())
        {
            await repository.CreatePermisosAsync(nuevosPermisos);
        }

        var log = await BuildAuditLogAsync(
            listadoId,
            listado.Nombre,
            "PermisosActualizados",
            $"Se actualizaron permisos para {nuevosPermisos.Count} colaborador(es)/área(s)",
            usuarioEmail,
            new DocumentoControl { Id = 0, Nombre = listado.Nombre },
            null);
        await auditRepository.CreateAsync(log);
    }

    private async Task<bool> ValidarPermisoAsync(int listadoId, int colaboradorId, string tipoPermiso)
    {
        var colaborador = await colaboradorRepository.GetByIdAsync(colaboradorId);
        if (colaborador is null)
            return false;

        var permisos = await repository.GetPermisosPorListadoAsync(listadoId);
        var permisosAplicables = permisos.Where(p => 
            p.ColaboradorId == colaboradorId || 
            (p.AreaId.HasValue && p.AreaId == colaborador.AreaId)
        ).ToList();

        if (!permisosAplicables.Any())
            return false;

        return tipoPermiso switch
        {
            "ver" => permisosAplicables.Any(p => p.PuedeVer),
            "editar" => permisosAplicables.Any(p => p.PuedeEditar || p.PuedeAprobar),
            "aprobar" => permisosAplicables.Any(p => p.PuedeAprobar),
            _ => false
        };
    }

    private static void SincronizarCamposPersonalizados(DocumentoControl doc, Dictionary<string, string?>? campos)
    {
        if (campos == null) return;

        foreach (var kvp in campos)
        {
            var keyNorm = kvp.Key.Trim().ToLowerInvariant()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("ü", "u").Replace("ñ", "n").Replace("ç", "c").Replace(" ", string.Empty);

            var value = kvp.Value?.Trim();
            if (string.IsNullOrWhiteSpace(value)) continue;

            if (keyNorm == "codigo")
            {
                doc.Codigo = value;
            }
            else if (keyNorm == "nombre")
            {
                doc.Nombre = value;
            }
            else if (keyNorm == "procesoresponsable" || keyNorm == "proceso" || keyNorm == "responsable")
            {
                doc.ProcesoResponsable = value;
            }
            else if (keyNorm == "version")
            {
                doc.Version = value;
            }
            else if (keyNorm == "fechadocumento" || keyNorm == "fecha")
            {
                if (DateTime.TryParse(value, out var parsedDate))
                {
                    doc.FechaDocumento = parsedDate;
                }
            }
            else if (keyNorm == "uso")
            {
                doc.Uso = value;
            }
            else if (keyNorm == "tiempoderetenciondelregistro(anos)" || keyNorm == "tiempoderetencion" || keyNorm == "retencion")
            {
                doc.TiempoRetencion = value;
            }
            else if (keyNorm == "proteccion")
            {
                doc.Proteccion = value;
            }
            else if (keyNorm == "recuperacion")
            {
                doc.Recuperacion = value;
            }
            else if (keyNorm == "disposicionfinal" || keyNorm == "disposicion")
            {
                doc.DisposicionFinal = value;
            }
            else if (keyNorm == "estado")
            {
                doc.Estado = value;
            }
            else if (keyNorm == "observaciones")
            {
                doc.Observaciones = value;
            }
        }
    }

    private static ListadoMaestroPermisoDto MapToPermisoDto(ListadoMaestroPermiso permiso) => new()
    {
        ColaboradorId = permiso.ColaboradorId,
        ColaboradorNombre = permiso.ColaboradorId.HasValue
            ? $"{permiso.Colaborador?.Nombre} {permiso.Colaborador?.Apellido}".Trim()
            : string.Empty,
        ColaboradorEmail = permiso.Colaborador?.Email ?? string.Empty,
        AreaId = permiso.AreaId,
        AreaNombre = permiso.Area?.Nombre ?? string.Empty,
        PuedeVer = permiso.PuedeVer,
        PuedeEditar = permiso.PuedeEditar,
        PuedeAprobar = permiso.PuedeAprobar
    };
}
