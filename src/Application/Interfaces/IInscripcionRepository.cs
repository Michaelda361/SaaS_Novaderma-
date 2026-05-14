using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IInscripcionRepository
{
    Task<IEnumerable<Inscripcion>> GetByCapacitacionAsync(int capacitacionId);
    Task<IEnumerable<Inscripcion>> GetByCapacitacionIgnorandoFiltrosAsync(int capacitacionId);
    Task<IEnumerable<Inscripcion>> GetByColaboradorAsync(int colaboradorId);
    Task<Inscripcion?> GetByIdAsync(int id);
    Task<Inscripcion> CreateAsync(Inscripcion inscripcion);
    Task<Inscripcion> UpdateAsync(Inscripcion inscripcion);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExisteInscripcionAsync(int capacitacionId, int colaboradorId);

    /// <summary>
    /// Devuelve todas las inscripciones con su resultado de cuestionario en una sola query.
    /// Reemplaza el N+1 masivo de CargarHistorial en el cliente.
    /// </summary>
    Task<List<(Inscripcion inscripcion, RespuestaCuestionario? respuesta, Cuestionario? cuestionario)>>
        GetHistorialCompletoAsync();
}
