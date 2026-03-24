using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Application.Services;

public class DocumentoService(
    IDocumentoRepository repository,
    IColaboradorRepository colaboradorRepository,
    IAreaRepository areaRepository,
    ISharePointService sharePoint)
{
    // ── Resolución de identidad ──────────────────────────────────────────────

    public async Task<Colaborador> ResolverColaboradorAsync(string email)
    {
        var todos = await colaboradorRepository.GetAllAsync();
        var colaborador = todos.FirstOrDefault(c =>
            c.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return colaborador ?? throw new UnauthorizedAccessException(
            "Usuario no registrado como colaborador");
    }

    public async Task<(bool esJefe, int areaId)> EsJefeDeAreaAsync(int colaboradorId)
    {
        var areas = await areaRepository.GetAllAsync();
        var area = areas.FirstOrDefault(a => a.JefeId == colaboradorId);
        return area is not null ? (true, area.Id) : (false, 0);
    }

    // ── Listado ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<DocumentoDto>> GetAllAsync(
        string? tipo, string? estado, int? areaId, string? busqueda, bool esAdmin)
    {
        var docs = esAdmin
            ? await repository.GetAllAsync()
            : await repository.GetPublicadosAsync();

        if (!string.IsNullOrWhiteSpace(tipo) &&
            Enum.TryParse<TipoDocumento>(tipo, out var tipoEnum))
            docs = docs.Where(d => d.TipoDocumento == tipoEnum);

        if (esAdmin && !string.IsNullOrWhiteSpace(estado) &&
            Enum.TryParse<EstadoDocumento>(estado, out var estadoEnum))
            docs = docs.Where(d => d.Estado == estadoEnum);

        if (areaId.HasValue)
            docs = docs.Where(d => d.AreaId == areaId);

        if (!string.IsNullOrWhiteSpace(busqueda))
            docs = docs.Where(d => d.Titulo.Contains(busqueda, StringComparison.OrdinalIgnoreCase));

        return docs.Select(MapToDto);
    }

    public async Task<DocumentoDetalleDto?> GetByIdAsync(int id, bool esAdmin)
    {
        var doc = await repository.GetByIdConDetallesAsync(id);
        if (doc is null) return null;
        if (!esAdmin && doc.Estado != EstadoDocumento.Publicado) return null;
        return MapToDetalleDto(doc, esAdmin);
    }

    // ── CRUD Admin ───────────────────────────────────────────────────────────

    public async Task<DocumentoDto> CreateAsync(
        CreateDocumentoDto dto, Stream? archivo, string? nombreArchivo)
    {
        if (!Enum.TryParse<TipoDocumento>(dto.TipoDocumento, out var tipo))
            throw new ArgumentException("Tipo de documento inválido");

        string itemId, url;

        if (!string.IsNullOrWhiteSpace(dto.UrlExterna))
        {
            // Documento vinculado desde OneDrive/SharePoint — no se sube archivo
            itemId = dto.UrlExterna;
            url = dto.UrlExterna;
        }
        else if (archivo is not null && !string.IsNullOrWhiteSpace(nombreArchivo))
        {
            (itemId, url) = await sharePoint.SubirArchivoOficialAsync(archivo, nombreArchivo, tipo);
        }
        else
        {
            throw new ArgumentException("Debes proporcionar un archivo o una URL externa");
        }

        var doc = new Documento
        {
            Titulo = dto.Titulo,
            TipoDocumento = tipo,
            Version = "1.0",
            Estado = EstadoDocumento.Borrador,
            SharePointItemId = itemId,
            SharePointUrl = url,
            AreaId = dto.AreaId
        };

        var created = await repository.CreateAsync(doc);
        return MapToDto(created);
    }

    public async Task<DocumentoDto?> UpdateMetadatosAsync(int id, UpdateDocumentoDto dto)
    {
        var doc = await repository.GetByIdAsync(id);
        if (doc is null) return null;
        if (doc.Estado == EstadoDocumento.Publicado)
            throw new InvalidOperationException(
                "No se pueden modificar metadatos de un documento publicado");

        if (!Enum.TryParse<TipoDocumento>(dto.TipoDocumento, out var tipo))
            throw new ArgumentException("Tipo de documento inválido");

        doc.Titulo = dto.Titulo;
        doc.TipoDocumento = tipo;
        doc.AreaId = dto.AreaId;

        var updated = await repository.UpdateAsync(doc);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var doc = await repository.GetByIdAsync(id);
        if (doc is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<DocumentoDto?> SubirNuevaVersionAsync(
        int id, Stream archivo, string nombreArchivo, bool incrementoMayor)
    {
        var doc = await repository.GetByIdAsync(id);
        if (doc is null) return null;

        // Guardar versión anterior
        var versionAnterior = new VersionDocumento
        {
            DocumentoId = doc.Id,
            NumeroVersion = doc.Version,
            SharePointItemId = doc.SharePointItemId,
            FechaCreacion = DateTime.UtcNow
        };
        await repository.CreateVersionAsync(versionAnterior);

        // Subir nuevo archivo
        var (itemId, url) = await sharePoint.SubirArchivoOficialAsync(
            archivo, nombreArchivo, doc.TipoDocumento);

        // Incrementar versión
        doc.Version = IncrementarVersion(doc.Version, incrementoMayor);
        doc.SharePointItemId = itemId;
        doc.SharePointUrl = url;

        // Si estaba publicado, vuelve a borrador
        if (doc.Estado == EstadoDocumento.Publicado)
            doc.Estado = EstadoDocumento.Borrador;

        var updated = await repository.UpdateAsync(doc);
        return MapToDto(updated);
    }

    // ── Flujo de aprobación Admin ────────────────────────────────────────────

    public async Task<DocumentoDto?> AvanzarEstadoAsync(int id, int colaboradorId)
    {
        var doc = await repository.GetByIdAsync(id);
        if (doc is null) return null;

        var estadoNuevo = doc.Estado switch
        {
            EstadoDocumento.Borrador  => EstadoDocumento.Revision,
            EstadoDocumento.Revision  => EstadoDocumento.Aprobado,
            EstadoDocumento.Aprobado  => EstadoDocumento.Publicado,
            _ => throw new InvalidOperationException(
                $"No hay transición válida desde el estado '{doc.Estado}'")
        };

        var flujo = new FlujoAprobacionDoc
        {
            DocumentoId = doc.Id,
            EstadoAnterior = doc.Estado,
            EstadoNuevo = estadoNuevo,
            ColaboradorId = colaboradorId,
            FechaTransicion = DateTime.UtcNow
        };

        doc.Estado = estadoNuevo;
        await repository.UpdateAsync(doc);
        await repository.CreateFlujoAsync(flujo);

        return MapToDto(doc);
    }

    // ── Propuestas Colaborador ───────────────────────────────────────────────

    public async Task<PropuestaModificacionDto> CrearPropuestaAsync(
        int documentoId, CreatePropuestaDto dto,
        Stream? archivo, string? nombreArchivo, string email)
    {
        var doc = await repository.GetByIdAsync(documentoId)
            ?? throw new KeyNotFoundException("Documento no encontrado");

        if (doc.Estado != EstadoDocumento.Publicado)
            throw new InvalidOperationException(
                "Solo se pueden proponer cambios sobre documentos publicados");

        var colaborador = await ResolverColaboradorAsync(email);

        if (colaborador.AreaId == 0)
            throw new InvalidOperationException(
                "Debes estar asignado a un área para proponer modificaciones");

        string? itemIdPropuesta = null;
        if (archivo is not null && !string.IsNullOrWhiteSpace(nombreArchivo))
        {
            var (itemId, _) = await sharePoint.SubirArchivoPropuestaAsync(
                archivo, nombreArchivo, documentoId);
            itemIdPropuesta = itemId;
        }

        var propuesta = new PropuestaModificacion
        {
            DocumentoId = documentoId,
            ColaboradorId = colaborador.Id,
            AreaId = colaborador.AreaId,
            Descripcion = dto.Descripcion,
            SharePointItemIdPropuesta = itemIdPropuesta,
            EstadoPropuesta = EstadoPropuesta.PendienteRevision,
            FechaCreacion = DateTime.UtcNow
        };

        var created = await repository.CreatePropuestaAsync(propuesta);
        // Recargar con includes
        var full = await repository.GetPropuestaByIdAsync(created.Id);
        return MapToPropuestaDto(full!);
    }

    // ── Propuestas JefeArea ──────────────────────────────────────────────────

    public async Task<IEnumerable<PropuestaModificacionDto>> GetPropuestasPendientesAsync(
        string email)
    {
        var colaborador = await ResolverColaboradorAsync(email);
        var (esJefe, areaId) = await EsJefeDeAreaAsync(colaborador.Id);
        if (!esJefe) return [];

        var propuestas = await repository.GetPropuestasPendientesPorAreaAsync(areaId);
        return propuestas.Select(MapToPropuestaDto);
    }

    public async Task<int> CountPropuestasPendientesAsync(string email)
    {
        try
        {
            var colaborador = await ResolverColaboradorAsync(email);
            var (esJefe, areaId) = await EsJefeDeAreaAsync(colaborador.Id);
            if (!esJefe) return 0;
            return await repository.CountPropuestasPendientesPorAreaAsync(areaId);
        }
        catch
        {
            return 0;
        }
    }

    public async Task AprobarPropuestaAsync(int propuestaId, string email)
    {
        var colaborador = await ResolverColaboradorAsync(email);
        var (esJefe, areaId) = await EsJefeDeAreaAsync(colaborador.Id);

        var propuesta = await repository.GetPropuestaByIdAsync(propuestaId)
            ?? throw new KeyNotFoundException("Propuesta no encontrada");

        if (!esJefe || propuesta.AreaId != areaId)
            throw new UnauthorizedAccessException(
                "No tienes permisos para aprobar esta propuesta");

        if (propuesta.EstadoPropuesta != EstadoPropuesta.PendienteRevision)
            throw new InvalidOperationException("La propuesta ya fue resuelta");

        var doc = await repository.GetByIdAsync(propuesta.DocumentoId)
            ?? throw new KeyNotFoundException("Documento no encontrado");

        // Si tiene archivo, moverlo a carpeta oficial y crear versión
        if (!string.IsNullOrWhiteSpace(propuesta.SharePointItemIdPropuesta))
        {
            var nombreArchivo = $"{doc.Titulo}_v{IncrementarVersion(doc.Version, false)}";
            await sharePoint.MoverArchivoPropuestaAOficialAsync(
                propuesta.SharePointItemIdPropuesta, nombreArchivo, doc.TipoDocumento);

            var versionAnterior = new VersionDocumento
            {
                DocumentoId = doc.Id,
                NumeroVersion = doc.Version,
                SharePointItemId = doc.SharePointItemId,
                FechaCreacion = DateTime.UtcNow
            };
            await repository.CreateVersionAsync(versionAnterior);

            doc.Version = IncrementarVersion(doc.Version, false);
            doc.SharePointItemId = propuesta.SharePointItemIdPropuesta;
            await repository.UpdateAsync(doc);
        }

        // Registrar en flujo
        var flujo = new FlujoAprobacionDoc
        {
            DocumentoId = doc.Id,
            EstadoAnterior = doc.Estado,
            EstadoNuevo = doc.Estado, // se mantiene Publicado
            ColaboradorId = colaborador.Id,
            FechaTransicion = DateTime.UtcNow
        };
        await repository.CreateFlujoAsync(flujo);

        propuesta.EstadoPropuesta = EstadoPropuesta.Aprobada;
        propuesta.AprobadorId = colaborador.Id;
        propuesta.FechaResolucion = DateTime.UtcNow;
        await repository.UpdatePropuestaAsync(propuesta);
    }

    public async Task RechazarPropuestaAsync(int propuestaId, string motivo, string email)
    {
        var colaborador = await ResolverColaboradorAsync(email);
        var (esJefe, areaId) = await EsJefeDeAreaAsync(colaborador.Id);

        var propuesta = await repository.GetPropuestaByIdAsync(propuestaId)
            ?? throw new KeyNotFoundException("Propuesta no encontrada");

        if (!esJefe || propuesta.AreaId != areaId)
            throw new UnauthorizedAccessException(
                "No tienes permisos para rechazar esta propuesta");

        if (propuesta.EstadoPropuesta != EstadoPropuesta.PendienteRevision)
            throw new InvalidOperationException("La propuesta ya fue resuelta");

        // Limpiar archivo temporal si existe
        if (!string.IsNullOrWhiteSpace(propuesta.SharePointItemIdPropuesta))
            await sharePoint.EliminarArchivoAsync(propuesta.SharePointItemIdPropuesta);

        propuesta.EstadoPropuesta = EstadoPropuesta.Rechazada;
        propuesta.AprobadorId = colaborador.Id;
        propuesta.MotivoRechazo = motivo;
        propuesta.FechaResolucion = DateTime.UtcNow;
        await repository.UpdatePropuestaAsync(propuesta);
    }

    // ── Edición Online ───────────────────────────────────────────────────────

    public async Task<string?> ObtenerUrlEdicionAsync(int id)
    {
        var doc = await repository.GetByIdAsync(id);
        if (doc is null) return null;
        return await sharePoint.ObtenerUrlEdicionAsync(
            doc.SharePointItemId, doc.SharePointUrl);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string IncrementarVersion(string version, bool mayor)
    {
        var partes = version.Split('.');
        if (partes.Length != 2 ||
            !int.TryParse(partes[0], out var maj) ||
            !int.TryParse(partes[1], out var min))
            return "1.0";

        return mayor ? $"{maj + 1}.0" : $"{maj}.{min + 1}";
    }

    private static DocumentoDto MapToDto(Documento d) => new()
    {
        Id = d.Id,
        Titulo = d.Titulo,
        TipoDocumento = d.TipoDocumento.ToString(),
        Version = d.Version,
        Estado = d.Estado.ToString(),
        AreaId = d.AreaId,
        AreaNombre = d.Area?.Nombre
    };

    private static DocumentoDetalleDto MapToDetalleDto(Documento d, bool esAdmin) => new()
    {
        Id = d.Id,
        Titulo = d.Titulo,
        TipoDocumento = d.TipoDocumento.ToString(),
        Version = d.Version,
        Estado = d.Estado.ToString(),
        AreaId = d.AreaId,
        AreaNombre = d.Area?.Nombre,
        SharePointUrl = d.SharePointUrl,
        Versiones = d.Versiones
            .OrderByDescending(v => v.FechaCreacion)
            .Select(v => new VersionDocumentoDto
            {
                Id = v.Id,
                NumeroVersion = v.NumeroVersion,
                FechaCreacion = v.FechaCreacion
            }).ToList(),
        Flujo = d.FlujoAprobacion
            .OrderByDescending(f => f.FechaTransicion)
            .Select(f => new FlujoAprobacionDocDto
            {
                Id = f.Id,
                EstadoAnterior = f.EstadoAnterior.ToString(),
                EstadoNuevo = f.EstadoNuevo.ToString(),
                ColaboradorNombre = f.Colaborador is null ? "—"
                    : $"{f.Colaborador.Nombre} {f.Colaborador.Apellido}",
                FechaTransicion = f.FechaTransicion
            }).ToList(),
        Propuestas = esAdmin
            ? d.Propuestas.OrderByDescending(p => p.FechaCreacion)
                .Select(MapToPropuestaDto).ToList()
            : []
    };

    private static PropuestaModificacionDto MapToPropuestaDto(PropuestaModificacion p) => new()
    {
        Id = p.Id,
        DocumentoId = p.DocumentoId,
        DocumentoTitulo = p.Documento?.Titulo ?? string.Empty,
        ColaboradorId = p.ColaboradorId,
        ColaboradorNombre = p.Colaborador is null ? "—"
            : $"{p.Colaborador.Nombre} {p.Colaborador.Apellido}",
        AreaId = p.AreaId,
        AreaNombre = p.Area?.Nombre ?? string.Empty,
        Descripcion = p.Descripcion,
        TieneArchivo = !string.IsNullOrWhiteSpace(p.SharePointItemIdPropuesta),
        EstadoPropuesta = p.EstadoPropuesta.ToString(),
        MotivoRechazo = p.MotivoRechazo,
        FechaCreacion = p.FechaCreacion,
        FechaResolucion = p.FechaResolucion
    };
}
