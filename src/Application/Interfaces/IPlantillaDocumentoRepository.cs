using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IPlantillaDocumentoRepository
{
    Task<IEnumerable<PlantillaDocumento>> GetAllAsync();
    Task<IEnumerable<PlantillaDocumento>> GetByAreaAsync(int areaId);
    Task<PlantillaDocumento?> GetByIdAsync(int id);
    Task<PlantillaDocumento> CreateAsync(PlantillaDocumento plantilla);
    Task<PlantillaDocumento> UpdateAsync(PlantillaDocumento plantilla);
    Task DeleteAsync(int id);
    Task<SolicitudDocumento> CreateSolicitudAsync(SolicitudDocumento solicitud);
    Task<IEnumerable<SolicitudDocumento>> GetSolicitudesByColaboradorAsync(int colaboradorId);
}
