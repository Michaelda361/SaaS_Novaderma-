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
        string? estado,
        string usuarioEmail);
    Task<DocumentoControlDetalleDto?> GetDocumentoAsync(int id, string usuarioEmail);
    Task<DocumentoControlDto> CreateDocumentoAsync(CreateDocumentoControlDto dto, string usuarioEmail);
    Task<UpdateDocumentoResult> UpdateDocumentoAsync(int id, UpdateDocumentoControlDto dto, string usuarioEmail);
    Task<SolicitudCambioDocumentoControlDto> CreateSolicitudCambioAsync(int documentoId, UpdateDocumentoControlDto propuesta, string usuarioEmail);
    Task<List<SolicitudCambioDocumentoControlDto>> GetSolicitudesCambioPendientesAsync(string usuarioEmail);
    Task<List<SolicitudCambioDocumentoControlDto>> GetSolicitudesCambioPorDocumentoAsync(int documentoId, string usuarioEmail);
    Task<int> CountSolicitudesCambioPendientesAsync(string usuarioEmail);
    Task IniciarRevisionSolicitudAsync(int solicitudId, string usuarioEmail);
    Task UpdateBorradorDocumentoAsync(int solicitudId, UpdateDocumentoControlDto borrador, string usuarioEmail);
    Task EnviarAAprobacionAsync(int solicitudId, string usuarioEmail);
    Task AprobarSolicitudCambioAsync(int solicitudId, string? comentarios, string usuarioEmail);
    Task RechazarSolicitudCambioAsync(int solicitudId, string motivo, string usuarioEmail);
    Task<List<ListadoMaestroPermisoDto>> GetListadoPermisosAsync(int listadoId, string usuarioEmail);
    Task<ListadoMaestroPermisoDto?> GetListadoPermisosActualUsuarioAsync(int listadoId, string usuarioEmail);
    Task UpdateListadoPermisosAsync(int listadoId, IEnumerable<ListadoMaestroPermisoUpdateDto> permisos, string usuarioEmail);
    Task<ListadoMaestroDto> CreateListadoAsync(CreateListadoMaestroDto dto, string usuarioEmail);
    Task<ListadoMaestroDto?> UpdateListadoAsync(int id, CreateListadoMaestroDto dto, string usuarioEmail);
    Task<bool> DeleteListadoAsync(int id, string usuarioEmail);
    Task<ListadoMaestroDto?> GetListadoAsync(int id, string usuarioEmail);
    Task<ListadoMaestroDto> ImportListadoAsync(CreateListadoMaestroDto dto, string usuarioEmail);
    Task<List<DocumentoControlDto>> GetHistorialAsync(int documentoId, string usuarioEmail);
}

