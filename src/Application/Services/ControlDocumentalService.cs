using System.Text.Json;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.ControlDocumental;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Application.Services;

public class ControlDocumentalService(
    IControlDocumentalRepository repository,
    IAuditLogRepository auditRepository,
    IColaboradorRepository colaboradorRepository) : IControlDocumentalService
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

    public async Task<List<DocumentoControlDto>> GetDocumentosAsync(
        int listadoId, int? areaId, string? busqueda, string? codigo,
        string? proceso, string? estado)
    {
        var docs = await repository.GetDocumentosAsync(listadoId, areaId, busqueda, codigo, proceso, estado);
        return docs.Select(MapToDto).ToList();
    }

    public async Task<DocumentoControlDetalleDto?> GetDocumentoAsync(int id)
    {
        var doc = await repository.GetDocumentoByIdAsync(id);
        return doc is null ? null : MapToDetalleDto(doc);
    }

    public async Task<ListadoMaestroDto> CreateListadoAsync(CreateListadoMaestroDto dto)
    {
        var listado = new ListadoMaestro
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim()
        };

        var created = await repository.CreateListadoAsync(listado);
        return new ListadoMaestroDto
        {
            Id = created.Id,
            Nombre = created.Nombre,
            Descripcion = created.Descripcion
        };
    }

    public async Task<DocumentoControlDto> CreateDocumentoAsync(
        CreateDocumentoControlDto dto, string usuarioEmail)
    {
        var listado = await repository.GetListadoByIdAsync(dto.ListadoMaestroId)
            ?? throw new KeyNotFoundException("Listado maestro no encontrado");

        if (string.IsNullOrWhiteSpace(dto.OneDriveUrl))
            throw new ArgumentException("La ruta del documento en OneDrive es obligatoria.");

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
            AreaId = dto.AreaId
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

        var updated = await repository.UpdateDocumentoAsync(existing);
        var cambios = BuildDiff(original, updated);
        var log = await BuildAuditLogAsync(updated.Id, updated.Nombre, "Actualizado", dto.ComentarioCambio, usuarioEmail, updated, cambios);
        await auditRepository.CreateAsync(log);

        return MapToDto(updated);
    }

    public async Task<List<AuditLogDto>> GetHistorialAsync(int documentoId)
    {
        var logs = await auditRepository.GetByEntidadAsync(EntidadTipo, documentoId);
        return logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            EntidadTipo = l.EntidadTipo,
            EntidadId = l.EntidadId,
            EntidadNombre = l.EntidadNombre,
            Accion = l.Accion,
            ColaboradorNombre = l.ColaboradorNombre,
            FechaHora = l.FechaHora,
            Observaciones = l.Observaciones,
            CamposModificados = l.CamposModificados
        }).ToList();
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
        AreaNombre = d.Area?.Nombre
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
        ListadoMaestroDescripcion = d.ListadoMaestro?.Descripcion
    };
}
