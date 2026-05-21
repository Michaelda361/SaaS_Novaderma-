using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IControlDocumentalRepository
{
    Task<IEnumerable<ListadoMaestro>> GetListadosAsync();
    Task<ListadoMaestro?> GetListadoByIdAsync(int id);
    Task<IEnumerable<DocumentoControl>> GetDocumentosAsync(
        int listadoId,
        int? areaId,
        string? busqueda,
        string? codigo,
        string? proceso,
        string? estado);
    Task<DocumentoControl?> GetDocumentoByIdAsync(int id);
    Task<DocumentoControl> CreateDocumentoAsync(DocumentoControl documento);
    Task<DocumentoControl> UpdateDocumentoAsync(DocumentoControl documento);
    Task<ListadoMaestro> CreateListadoAsync(ListadoMaestro listado);
    Task<ListadoMaestro> UpdateListadoAsync(ListadoMaestro listado);
    Task<IEnumerable<DocumentoControlCampoDefinicion>> GetCamposPorListadoAsync(int listadoId);
    Task<DocumentoControlCampoDefinicion> CreateCampoAsync(DocumentoControlCampoDefinicion campo);
    Task<DocumentoControlCampoDefinicion> UpdateCampoAsync(DocumentoControlCampoDefinicion campo);
    Task DeleteCampoAsync(DocumentoControlCampoDefinicion campo);
}
