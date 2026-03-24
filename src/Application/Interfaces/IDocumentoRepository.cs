using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IDocumentoRepository
{
    Task<IEnumerable<Documento>> GetAllAsync();
    Task<IEnumerable<Documento>> GetPublicadosAsync();
    Task<Documento?> GetByIdAsync(int id);
    Task<Documento?> GetByIdConDetallesAsync(int id);
    Task<Documento> CreateAsync(Documento documento);
    Task<Documento> UpdateAsync(Documento documento);
    Task DeleteAsync(int id);

    Task<IEnumerable<PropuestaModificacion>> GetPropuestasPendientesPorAreaAsync(int areaId);
    Task<int> CountPropuestasPendientesPorAreaAsync(int areaId);
    Task<PropuestaModificacion?> GetPropuestaByIdAsync(int id);
    Task<PropuestaModificacion> CreatePropuestaAsync(PropuestaModificacion propuesta);
    Task<PropuestaModificacion> UpdatePropuestaAsync(PropuestaModificacion propuesta);
    Task CreateVersionAsync(VersionDocumento version);
    Task CreateFlujoAsync(FlujoAprobacionDoc flujo);
}
