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

    public async Task<ListadoMaestroDto?> GetListadoAsync(int id)
    {
        var listado = await repository.GetListadoByIdAsync(id);
        if (listado is null) return null;
        var dto = MapToListadoDto(listado);
        var campos = (await repository.GetCamposPorListadoAsync(id)).ToList();
        dto.Campos = campos.Select(MapToCampoDto).ToList();
        return dto;
    }

    public async Task<List<DocumentoControlDto>> GetDocumentosAsync(
        int listadoId, int? areaId, string? busqueda, string? codigo,
        string? proceso, string? estado)
    {
        var docs = await repository.GetDocumentosAsync(listadoId, areaId, busqueda, codigo, proceso, estado);
        var docList = docs.ToList();
        Console.WriteLine($"[RETRIEVAL DIAGNOSTIC] Registros recuperados para visualización: {docList.Count} documentos obtenidos para el listado ID {listadoId}.");
        return docList.Select(MapToDto).ToList();
    }

    public async Task<DocumentoControlDetalleDto?> GetDocumentoAsync(int id)
    {
        var doc = await repository.GetDocumentoByIdAsync(id);
        return doc is null ? null : MapToDetalleDto(doc);
    }

    public async Task<ListadoMaestroDto> CreateListadoAsync(CreateListadoMaestroDto dto, string usuarioEmail)
    {
        // Verificar permisos: solo Jefe o Admin pueden crear listados maestros
        var creador = await TryResolverColaboradorAsync(usuarioEmail);
        if (creador is null || (creador.Rol != Domain.Enums.RolUsuario.Admin && creador.Rol != Domain.Enums.RolUsuario.Jefe))
            throw new UnauthorizedAccessException("No tiene permisos para crear listados maestros.");

        Console.WriteLine($"[PERSISTENCE DIAGNOSTIC] Registros enviados a persistencia para creación: {dto.Documentos?.Count ?? 0} documentos and {dto.Campos?.Count ?? 0} columnas.");

        var listado = new ListadoMaestro
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim()
        };

        var created = await repository.CreateListadoAsync(listado);
        Console.WriteLine($"[PERSISTENCE DIAGNOSTIC] Listado Maestro guardado en base de datos. ID: {created.Id}, Nombre: '{created.Nombre}'");

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
                Console.WriteLine($"[PERSISTENCE DIAGNOSTIC] Registro/Documento guardado en base de datos: Código='{createdDoc.Codigo}', Nombre='{createdDoc.Nombre}', ID={createdDoc.Id}");

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

        Console.WriteLine($"[PERSISTENCE DIAGNOSTIC] Registros enviados a persistencia para actualización del listado ID {id}: {dto.Documentos?.Count ?? 0} documentos y {dto.Campos?.Count ?? 0} columnas.");

        var existing = await repository.GetListadoByIdAsync(id);
        if (existing is null) return null;

        existing.Nombre = dto.Nombre.Trim();
        existing.Descripcion = dto.Descripcion?.Trim();

        var updated = await repository.UpdateListadoAsync(existing);
        Console.WriteLine($"[PERSISTENCE DIAGNOSTIC] Listado Maestro actualizado en base de datos. ID: {updated.Id}, Nombre: '{updated.Nombre}'");

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
        var existing = await repository.GetListadoByNombreAsync(dto.Nombre.Trim());
        if (existing is null)
        {
            return await CreateListadoAsync(dto, usuarioEmail);
        }

        var updated = await UpdateListadoAsync(existing.Id, dto, usuarioEmail);
        return updated ?? await CreateListadoAsync(dto, usuarioEmail);
    }

    private static bool IsPlaceholderCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return true;
        var norm = code.Trim().ToUpperInvariant();
        return norm == "N.A" || norm == "N/A" || norm == "NA" || norm == "NO APLICA" || norm == "N/D" || norm == "ND";
    }    private async Task SyncImportDocumentsAsync(int listadoId, IEnumerable<TemplateDocumentoDto> documentos, string usuarioEmail)
    {
        Console.WriteLine($"[PERSISTENCE DIAGNOSTIC] Iniciando sincronización limpia para listado ID {listadoId}. Importando {documentos.Count()} registros.");

        // 1. Desactivar todos los documentos actuales para evitar mezclas con importaciones anteriores
        var activeDbDocs = (await repository.GetDocumentosAsync(listadoId, null, null, null, null, null))
            .Where(d => d.Activo)
            .ToList();

        foreach (var dbDoc in activeDbDocs)
        {
            var docEntity = await repository.GetDocumentoByIdAsync(dbDoc.Id);
            if (docEntity is not null)
            {
                docEntity.Activo = false;
                await repository.UpdateDocumentoAsync(docEntity);
                
                var log = await BuildAuditLogAsync(docEntity.Id, docEntity.Nombre, "Eliminado", "Eliminado por nueva importación (sobrescritura limpia)", usuarioEmail, docEntity, null);
                await auditRepository.CreateAsync(log);
            }
        }

        // 2. Crear todos los nuevos registros
        int createdCount = 0;
        var areas = await areaRepository.GetAllAsync();
        var areasByName = areas.ToDictionary(a => a.Nombre.Trim(), a => a.Id, StringComparer.OrdinalIgnoreCase);
        int? GetAreaId(string? areaName)
        {
            if (string.IsNullOrWhiteSpace(areaName)) return null;
            return areasByName.TryGetValue(areaName.Trim(), out var id) ? id : null;
        }

        foreach (var docDto in documentos)
        {
            var newDoc = new DocumentoControl
            {
                ListadoMaestroId = listadoId,
                Activo = true
            };
            MapTemplateToDocumento(docDto, newDoc, GetAreaId(docDto.Area));

            // EF navigation properties null to avoid conflicts
            newDoc.ListadoMaestro = null!;
            newDoc.Area = null!;

            var createdDoc = await repository.CreateDocumentoAsync(newDoc);
            createdCount++;
            Console.WriteLine($"[PERSISTENCE DIAGNOSTIC] Documento creado: Código='{createdDoc.Codigo}', Nombre='{createdDoc.Nombre}', ID={createdDoc.Id}");

            var log = await BuildAuditLogAsync(createdDoc.Id, createdDoc.Nombre, "Creado", docDto.ComentarioCambio ?? "Importado mediante archivo Excel", usuarioEmail, createdDoc, null);
            await auditRepository.CreateAsync(log);
        }

        Console.WriteLine($"[PERSISTENCE DIAGNOSTIC] Sincronización completada. Creados: {createdCount}.");
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

    private static string? GetValoresPersonalizados(Dictionary<string, string?>? campos, params string[] clavesCandidatas)
    {
        if (campos is null || !campos.Any()) return null;
        foreach (var clave in clavesCandidatas)
        {
            if (campos.TryGetValue(clave, out var val))
            {
                return val;
            }
        }
        foreach (var candidate in clavesCandidatas)
        {
            var match = campos.Keys.FirstOrDefault(k => k.Contains(candidate, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return campos[match];
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

        var created = await repository.CreateDocumentoAsync(doc);
        var log = await BuildAuditLogAsync(created.Id, created.Nombre, "Creado", dto.ComentarioCambio, usuarioEmail, created, null);
        await auditRepository.CreateAsync(log);

        return MapToDto(created);
    }

    public async Task<DocumentoControlDto?> UpdateDocumentoAsync(
        int id, UpdateDocumentoControlDto dto, string usuarioEmail)
    {
        var existing = await repository.GetDocumentoByIdAsync(id);
        if (existing is null) return null;

        var editor = await TryResolverColaboradorAsync(usuarioEmail);
        if (editor is null)
            throw new UnauthorizedAccessException("No tiene permisos para actualizar este documento.");

        // Verificar permisos basados en ListadoMaestroPermiso
        var tienePermisoEditar = editor.Rol == RolUsuario.Admin || 
                                editor.Rol == RolUsuario.Jefe ||
                                await ValidarPermisoAsync(existing.ListadoMaestroId, editor.Id, "editar");

        if (!tienePermisoEditar)
            throw new UnauthorizedAccessException("No tienes permisos para editar documentos de este listado maestro.");

        if (dto.ListadoMaestroId != existing.ListadoMaestroId &&
            editor.Rol != RolUsuario.Admin && editor.Rol != RolUsuario.Jefe)
        {
            throw new UnauthorizedAccessException("No tiene permisos para reasignar el listado maestro del documento.");
        }

        await ValidateCamposPersonalizados(dto.ListadoMaestroId, dto.CamposPersonalizados);

        var original = CloneForDiff(existing);

        existing.ListadoMaestroId = dto.ListadoMaestroId;
        existing.Codigo = dto.Codigo.Trim();
        existing.Nombre = dto.Nombre.Trim();
        existing.ProcesoResponsable = dto.ProcesoResponsable.Trim();
        existing.Version = string.IsNullOrWhiteSpace(dto.Version) ? existing.Version : dto.Version.Trim();
        existing.FechaDocumento = dto.FechaDocumento;
        existing.OneDriveUrl = dto.OneDriveUrl.Trim();
        existing.OneDriveItemId = dto.OneDriveItemId?.Trim();
        existing.ArchivoNombre = dto.ArchivoNombre?.Trim();
        existing.Uso = dto.Uso?.Trim();
        existing.TiempoRetencion = dto.TiempoRetencion?.Trim();
        existing.Proteccion = dto.Proteccion?.Trim();
        existing.Recuperacion = dto.Recuperacion?.Trim();
        existing.DisposicionFinal = dto.DisposicionFinal?.Trim();
        existing.Estado = dto.Estado.Trim();
        existing.Observaciones = dto.Observaciones?.Trim();
        existing.ComentarioCambio = dto.ComentarioCambio?.Trim();
        existing.AreaId = dto.AreaId;
        existing.DatosPersonalizados = dto.CamposPersonalizados is null
            ? existing.DatosPersonalizados
            : JsonSerializer.Serialize(dto.CamposPersonalizados);

        var updated = await repository.UpdateDocumentoAsync(existing);
        var cambios = BuildDiff(original, updated);
        var log = await BuildAuditLogAsync(updated.Id, updated.Nombre, "Actualizado", dto.ComentarioCambio, usuarioEmail, updated, cambios);
        await auditRepository.CreateAsync(log);

        return MapToDto(updated);
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
            EstadoPropuesta = EstadoPropuesta.PendienteRevision,
            FechaCreacion = DateTime.UtcNow
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
            return (await repository.GetSolicitudesCambioPendientesAsync()).Count();

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
            Estado = "Borrador",
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

        await repository.UpdateDocumentoAsync(borrador);

        if (solicitud.EditorId != editor.Id)
        {
            solicitud.EditorId = editor.Id;
            await repository.UpdateSolicitudCambioAsync(solicitud);
        }
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

    public async Task AprobarSolicitudCambioAsync(int solicitudId, string usuarioEmail)
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

        UpdateDocumentoControlDto? propuesta = null;
        DocumentoControl? borradorToDelete = null;

        if (solicitud.BorradorDocumentoId.HasValue)
        {
            var borrador = await repository.GetDocumentoByIdAsync(solicitud.BorradorDocumentoId.Value);
            if (borrador != null)
            {
                borradorToDelete = borrador;
                propuesta = new UpdateDocumentoControlDto
                {
                    ListadoMaestroId = borrador.ListadoMaestroId,
                    Codigo = borrador.Codigo,
                    Nombre = borrador.Nombre,
                    ProcesoResponsable = borrador.ProcesoResponsable,
                    Version = borrador.Version,
                    FechaDocumento = borrador.FechaDocumento,
                    OneDriveUrl = borrador.OneDriveUrl,
                    OneDriveItemId = borrador.OneDriveItemId,
                    ArchivoNombre = borrador.ArchivoNombre,
                    Uso = borrador.Uso,
                    TiempoRetencion = borrador.TiempoRetencion,
                    Proteccion = borrador.Proteccion,
                    Recuperacion = borrador.Recuperacion,
                    DisposicionFinal = borrador.DisposicionFinal,
                    Estado = "Vigente",
                    Observaciones = borrador.Observaciones,
                    ComentarioCambio = borrador.ComentarioCambio,
                    AreaId = borrador.AreaId,
                    CamposPersonalizados = string.IsNullOrWhiteSpace(borrador.DatosPersonalizados)
                        ? new Dictionary<string, string?>()
                        : JsonSerializer.Deserialize<Dictionary<string, string?>>(borrador.DatosPersonalizados)
                };
            }
        }

        if (propuesta == null)
        {
            propuesta = JsonSerializer.Deserialize<UpdateDocumentoControlDto>(solicitud.DatosPropuestos)
                ?? throw new InvalidOperationException("Los datos de la solicitud son inválidos.");
        }

        await ValidateCamposPersonalizados(propuesta.ListadoMaestroId, propuesta.CamposPersonalizados);

        // 1. Clone current Vigente document as Historical (Activo = false)
        var historicalClone = new DocumentoControl
        {
            ListadoMaestroId = documento.ListadoMaestroId,
            Codigo = documento.Codigo,
            Nombre = documento.Nombre,
            ProcesoResponsable = documento.ProcesoResponsable,
            Version = documento.Version,
            FechaDocumento = documento.FechaDocumento,
            OneDriveUrl = documento.OneDriveUrl,
            OneDriveItemId = documento.OneDriveItemId,
            ArchivoNombre = documento.ArchivoNombre,
            Uso = documento.Uso,
            TiempoRetencion = documento.TiempoRetencion,
            Proteccion = documento.Proteccion,
            Recuperacion = documento.Recuperacion,
            DisposicionFinal = documento.DisposicionFinal,
            Estado = "Histórica",
            Observaciones = documento.Observaciones,
            ComentarioCambio = documento.ComentarioCambio,
            DatosPersonalizados = documento.DatosPersonalizados,
            AreaId = documento.AreaId,
            DocumentoOriginalId = documento.Id,
            Activo = false,
            
            SolicitanteId = documento.SolicitanteId,
            EditorId = documento.EditorId,
            AprobadorId = documento.AprobadorId,
            FechaPublicacion = documento.FechaPublicacion,
            MotivoCambio = documento.MotivoCambio,
            DescripcionDetallada = documento.DescripcionDetallada
        };

        await repository.CreateDocumentoAsync(historicalClone);

        // 2. Update Vigente document in place
        var original = CloneForDiff(documento);

        documento.ListadoMaestroId = propuesta.ListadoMaestroId;
        documento.Codigo = propuesta.Codigo.Trim();
        documento.Nombre = propuesta.Nombre.Trim();
        documento.ProcesoResponsable = propuesta.ProcesoResponsable.Trim();
        documento.Version = string.IsNullOrWhiteSpace(propuesta.Version) ? documento.Version : propuesta.Version.Trim();
        documento.FechaDocumento = propuesta.FechaDocumento;
        documento.OneDriveUrl = propuesta.OneDriveUrl.Trim();
        documento.OneDriveItemId = propuesta.OneDriveItemId?.Trim();
        documento.ArchivoNombre = propuesta.ArchivoNombre?.Trim();
        documento.Uso = propuesta.Uso?.Trim();
        documento.TiempoRetencion = propuesta.TiempoRetencion?.Trim();
        documento.Proteccion = propuesta.Proteccion?.Trim();
        documento.Recuperacion = propuesta.Recuperacion?.Trim();
        documento.DisposicionFinal = propuesta.DisposicionFinal?.Trim();
        documento.Estado = "Vigente";
        documento.Observaciones = propuesta.Observaciones?.Trim();
        documento.ComentarioCambio = solicitud.ComentarioSolicitud;
        documento.AreaId = propuesta.AreaId;
        documento.DatosPersonalizados = propuesta.CamposPersonalizados is null
            ? documento.DatosPersonalizados
            : JsonSerializer.Serialize(propuesta.CamposPersonalizados);

        documento.SolicitanteId = solicitud.SolicitanteId;
        documento.EditorId = solicitud.EditorId ?? solicitud.SolicitanteId;
        documento.AprobadorId = aprobador.Id;
        documento.FechaPublicacion = DateTime.UtcNow;
        documento.MotivoCambio = solicitud.MotivoCambio ?? solicitud.ComentarioSolicitud;
        documento.DescripcionDetallada = solicitud.DescripcionDetallada;

        var updated = await repository.UpdateDocumentoAsync(documento);
        var cambios = BuildDiff(original, updated);

        // 3. Update request status and clean up borrador
        solicitud.EstadoPropuesta = EstadoPropuesta.Aprobada;
        solicitud.AprobadorId = aprobador.Id;
        solicitud.FechaResolucion = DateTime.UtcNow;

        if (borradorToDelete != null)
        {
            solicitud.BorradorDocumentoId = null;
            await repository.UpdateSolicitudCambioAsync(solicitud);
            await repository.DeleteDocumentoAsync(borradorToDelete);
        }
        else
        {
            await repository.UpdateSolicitudCambioAsync(solicitud);
        }

        var log = await BuildAuditLogAsync(updated.Id, updated.Nombre, "SolicitudCambioAprobada", solicitud.ComentarioSolicitud, usuarioEmail, updated, cambios);
        await auditRepository.CreateAsync(log);
    }

    public async Task RechazarSolicitudCambioAsync(int solicitudId, string motivo, string usuarioEmail)
    {
        var aprobador = await TryResolverColaboradorAsync(usuarioEmail);
        if (aprobador is null)
            throw new UnauthorizedAccessException("No tiene permisos para rechazar solicitudes.");

        var solicitud = await repository.GetSolicitudCambioByIdAsync(solicitudId)
            ?? throw new KeyNotFoundException("Solicitud de cambio no encontrada.");

        if (solicitud.EstadoPropuesta == EstadoPropuesta.Aprobada || solicitud.EstadoPropuesta == EstadoPropuesta.Rechazada)
            throw new InvalidOperationException("La solicitud ya fue resuelta.");

        var (esJefe, areaId) = await EsJefeDeAreaAsync(aprobador.Id);
        if (aprobador.Rol != RolUsuario.Admin && !esJefe)
            throw new UnauthorizedAccessException("No tienes permisos para rechazar esta solicitud.");

        if (aprobador.Rol == RolUsuario.Jefe && solicitud.DocumentoControl.AreaId != areaId)
            throw new UnauthorizedAccessException("No tienes permisos para rechazar esta solicitud.");

        solicitud.EstadoPropuesta = EstadoPropuesta.Rechazada;
        solicitud.AprobadorId = aprobador.Id;
        solicitud.ComentarioResolucion = motivo?.Trim();
        solicitud.FechaResolucion = DateTime.UtcNow;

        DocumentoControl? borradorToDelete = null;
        if (solicitud.BorradorDocumentoId.HasValue)
        {
            var borrador = await repository.GetDocumentoByIdAsync(solicitud.BorradorDocumentoId.Value);
            if (borrador != null)
            {
                borradorToDelete = borrador;
                solicitud.BorradorDocumentoId = null;
            }
        }

        await repository.UpdateSolicitudCambioAsync(solicitud);

        if (borradorToDelete != null)
        {
            await repository.DeleteDocumentoAsync(borradorToDelete);
        }

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
        SolicitanteNombre = solicitud.Solicitante?.Nombre + " " + solicitud.Solicitante?.Apellido,
        EstadoPropuesta = solicitud.EstadoPropuesta.ToString(),
        ComentarioSolicitud = solicitud.ComentarioSolicitud,
        ComentarioResolucion = solicitud.ComentarioResolucion,
        FechaCreacion = solicitud.FechaCreacion,
        FechaEdicion = solicitud.FechaEdicion,
        FechaResolucion = solicitud.FechaResolucion,
        DatosPropuestos = solicitud.DatosPropuestos,
        AprobadorId = solicitud.AprobadorId,
        AprobadorNombre = solicitud.Aprobador?.Nombre + " " + solicitud.Aprobador?.Apellido,
        EditorNombre = solicitud.Editor?.Nombre + " " + solicitud.Editor?.Apellido,
        
        RevisorId = solicitud.RevisorId,
        RevisorNombre = solicitud.Revisor != null ? $"{solicitud.Revisor.Nombre} {solicitud.Revisor.Apellido}" : null,
        FechaRevision = solicitud.FechaRevision,
        ObservacionesRevision = solicitud.ObservacionesRevision,
        MotivoCambio = solicitud.MotivoCambio,
        DescripcionDetallada = solicitud.DescripcionDetallada,
        BorradorDocumentoId = solicitud.BorradorDocumentoId
    };

    public async Task<List<DocumentoControlDto>> GetHistorialAsync(int documentoId)
    {
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

    public async Task<List<ListadoMaestroPermisoDto>> GetListadoPermisosAsync(int listadoId)
    {
        var permisos = await repository.GetPermisosPorListadoAsync(listadoId);
        return permisos.Select(MapToPermisoDto).ToList();
    }

    public async Task<ListadoMaestroPermisoDto?> GetListadoPermisosActualUsuarioAsync(int listadoId, string usuarioEmail)
    {
        var colaborador = await TryResolverColaboradorAsync(usuarioEmail);
        if (colaborador is null)
            return null;

        var permisos = await repository.GetPermisosPorListadoAsync(listadoId);
        var permiso = permisos.FirstOrDefault(p => p.ColaboradorId == colaborador.Id);
        
        return permiso is null ? null : MapToPermisoDto(permiso);
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
            var colaborador = await colaboradorRepository.GetByIdAsync(permisoDto.ColaboradorId);
            if (colaborador is null)
                continue;

            nuevosPermisos.Add(new ListadoMaestroPermiso
            {
                ListadoMaestroId = listadoId,
                ColaboradorId = permisoDto.ColaboradorId,
                PuedeVer = permisoDto.PuedeVer,
                PuedeEditar = permisoDto.PuedeEditar,
                PuedeAprobar = permisoDto.PuedeAprobar
            });
        }

        if (nuevosPermisos.Any())
        {
            await repository.CreatePermisosAsync(nuevosPermisos);
        }

        var log = await BuildAuditLogAsync(
            listadoId,
            listado.Nombre,
            "PermisosActualizados",
            $"Se actualizaron permisos para {nuevosPermisos.Count} colaborador(es)",
            usuarioEmail,
            new DocumentoControl { Id = 0, Nombre = listado.Nombre },
            null);
        await auditRepository.CreateAsync(log);
    }

    private async Task<bool> ValidarPermisoAsync(int listadoId, int colaboradorId, string tipoPermiso)
    {
        var permisos = await repository.GetPermisosPorListadoAsync(listadoId);
        var permiso = permisos.FirstOrDefault(p => p.ColaboradorId == colaboradorId);

        if (permiso is null)
            return false;

        return tipoPermiso switch
        {
            "ver" => permiso.PuedeVer,
            "editar" => permiso.PuedeEditar || permiso.PuedeAprobar,
            "aprobar" => permiso.PuedeAprobar,
            _ => false
        };
    }

    private static ListadoMaestroPermisoDto MapToPermisoDto(ListadoMaestroPermiso permiso) => new()
    {
        ColaboradorId = permiso.ColaboradorId,
        ColaboradorNombre = permiso.Colaborador?.Nombre + " " + permiso.Colaborador?.Apellido,
        ColaboradorEmail = permiso.Colaborador?.Email ?? string.Empty,
        PuedeVer = permiso.PuedeVer,
        PuedeEditar = permiso.PuedeEditar,
        PuedeAprobar = permiso.PuedeAprobar
    };
}
