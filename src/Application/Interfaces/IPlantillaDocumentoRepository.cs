using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;

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
    Task<SolicitudDocumento?> GetSolicitudByIdAsync(int id);
    Task<SolicitudDocumento> UpdateSolicitudAsync(SolicitudDocumento solicitud);
    Task<IEnumerable<SolicitudDocumento>> GetSolicitudesByColaboradorAsync(int colaboradorId);
    Task<IEnumerable<SolicitudDocumento>> GetTodasSolicitudesAsync();
    Task<IEnumerable<SolicitudDocumento>> GetSolicitudesPendientesAsync();
    Task<int> CountPendientesAsync();
    Task<bool> ExisteSolicitudPendienteAsync(int plantillaId, int colaboradorId);
    Task MarcarSolicitudesComoVistaAsync(int colaboradorId);
}
