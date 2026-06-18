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
    Task<DocumentoControl?> GetDocumentoByCodigoAsync(int listadoId, string codigo);
    Task<DocumentoControl?> GetDocumentoByCodigoYNombreAsync(int listadoId, string codigo, string nombre);
    Task<DocumentoControl> CreateDocumentoAsync(DocumentoControl documento);
    Task<DocumentoControl> UpdateDocumentoAsync(DocumentoControl documento);
    Task<bool> ExisteSolicitudCambioPendienteAsync(int documentoControlId, int colaboradorId);
    Task<SolicitudCambioDocumentoControl> CreateSolicitudCambioAsync(SolicitudCambioDocumentoControl solicitud);
    Task<SolicitudCambioDocumentoControl> UpdateSolicitudCambioAsync(SolicitudCambioDocumentoControl solicitud);
    Task<SolicitudCambioDocumentoControl?> GetSolicitudCambioByIdAsync(int id);
    Task<IEnumerable<SolicitudCambioDocumentoControl>> GetSolicitudesPorDocumentoAsync(int documentoId);
    Task<IEnumerable<ListadoMaestroPermiso>> GetPermisosPorListadoAsync(int listadoId);
    Task<IEnumerable<ListadoMaestroPermiso>> GetPermisosPorColaboradorAsync(int colaboradorId);
    Task<IEnumerable<ListadoMaestroPermiso>> CreatePermisosAsync(IEnumerable<ListadoMaestroPermiso> permisos);
    Task DeletePermisosPorListadoAsync(int listadoId);
    Task<IEnumerable<SolicitudCambioDocumentoControl>> GetSolicitudesCambioPendientesAsync();
    Task<IEnumerable<SolicitudCambioDocumentoControl>> GetSolicitudesCambioPendientesPorAreaAsync(int areaId);
    Task<int> CountSolicitudesCambioPendientesPorAreaAsync(int areaId);
    Task<ListadoMaestro?> GetListadoByNombreAsync(string nombre);
    Task<ListadoMaestro> CreateListadoAsync(ListadoMaestro listado);
    Task<ListadoMaestro> UpdateListadoAsync(ListadoMaestro listado);
    Task<IEnumerable<DocumentoControlCampoDefinicion>> GetCamposPorListadoAsync(int listadoId);
    Task<DocumentoControlCampoDefinicion> CreateCampoAsync(DocumentoControlCampoDefinicion campo);
    Task<DocumentoControlCampoDefinicion> UpdateCampoAsync(DocumentoControlCampoDefinicion campo);
    Task DeleteCampoAsync(DocumentoControlCampoDefinicion campo);
    Task<bool> DeleteListadoAsync(int id);
    Task DeleteDocumentoAsync(DocumentoControl documento);
    Task<IEnumerable<DocumentoControl>> GetDocumentosIgnoreFiltersAsync(int documentoId);
    Task ExecuteInTransactionAsync(Func<Task> action);
}
