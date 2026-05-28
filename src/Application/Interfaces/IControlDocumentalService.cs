using TalentManagement.Shared.DTOs.ControlDocumental;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Application.Interfaces;

public interface IControlDocumentalService
{
    Task<List<ListadoMaestroDto>> GetListadosAsync();
    Task<List<ListadoMaestroDto>> GetListadosParaUsuarioAsync(string usuarioEmail);
    Task<List<DocumentoControlDto>> GetDocumentosAsync(
        int listadoId,
        int? areaId,
        string? busqueda,
        string? codigo,
        string? proceso,
        string? estado);
    Task<DocumentoControlDetalleDto?> GetDocumentoAsync(int id);
    Task<DocumentoControlDto> CreateDocumentoAsync(CreateDocumentoControlDto dto, string usuarioEmail);
    Task<DocumentoControlDto?> UpdateDocumentoAsync(int id, UpdateDocumentoControlDto dto, string usuarioEmail);
    Task<SolicitudCambioDocumentoControlDto> CreateSolicitudCambioAsync(int documentoId, UpdateDocumentoControlDto propuesta, string usuarioEmail);
    Task<List<SolicitudCambioDocumentoControlDto>> GetSolicitudesCambioPendientesAsync(string usuarioEmail);
    Task<List<SolicitudCambioDocumentoControlDto>> GetSolicitudesCambioPorDocumentoAsync(int documentoId, string usuarioEmail);
    Task<int> CountSolicitudesCambioPendientesAsync(string usuarioEmail);
    Task AprobarSolicitudCambioAsync(int solicitudId, string usuarioEmail);
    Task RechazarSolicitudCambioAsync(int solicitudId, string motivo, string usuarioEmail);
    Task<List<ListadoMaestroPermisoDto>> GetListadoPermisosAsync(int listadoId);
    Task<ListadoMaestroPermisoDto?> GetListadoPermisosActualUsuarioAsync(int listadoId, string usuarioEmail);
    Task UpdateListadoPermisosAsync(int listadoId, IEnumerable<ListadoMaestroPermisoUpdateDto> permisos, string usuarioEmail);
    Task<ListadoMaestroDto> CreateListadoAsync(CreateListadoMaestroDto dto, string usuarioEmail);
    Task<ListadoMaestroDto?> UpdateListadoAsync(int id, CreateListadoMaestroDto dto, string usuarioEmail);
    Task<bool> DeleteListadoAsync(int id, string usuarioEmail);
    Task<ListadoMaestroDto?> GetListadoAsync(int id);
    Task<ListadoMaestroDto> ImportListadoAsync(CreateListadoMaestroDto dto, string usuarioEmail);
    Task<List<AuditLogDto>> GetHistorialAsync(int documentoId);
}

