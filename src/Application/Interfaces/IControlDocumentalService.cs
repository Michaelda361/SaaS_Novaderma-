using TalentManagement.Shared.DTOs.ControlDocumental;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Application.Interfaces;

public interface IControlDocumentalService
{
    Task<List<ListadoMaestroDto>> GetListadosAsync();
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
    Task<ListadoMaestroDto> CreateListadoAsync(CreateListadoMaestroDto dto, string usuarioEmail);
    Task<ListadoMaestroDto?> UpdateListadoAsync(int id, CreateListadoMaestroDto dto, string usuarioEmail);
    Task<ListadoMaestroDto?> GetListadoAsync(int id);
    Task<List<AuditLogDto>> GetHistorialAsync(int documentoId);
}

