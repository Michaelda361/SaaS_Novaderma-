using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface ICuestionarioRepository
{
    Task<Cuestionario?> GetByCapacitacionAsync(int capacitacionId);
    Task<Cuestionario?> GetByIdAsync(int id);
    Task<Cuestionario> CreateAsync(Cuestionario cuestionario);
    Task<Cuestionario> UpdateAsync(Cuestionario cuestionario);
    Task DeleteAsync(int id);
    Task<RespuestaCuestionario?> GetRespuestaAsync(int cuestionarioId, int inscripcionId);
    Task<List<RespuestaCuestionario>> GetRespuestasAsync(int cuestionarioId, int inscripcionId);
    Task<RespuestaCuestionario> SaveRespuestaAsync(RespuestaCuestionario respuesta);

    /// <summary>
    /// Devuelve los CapacitacionIds de las capacitaciones cuyo cuestionario fue completado por el colaborador.
    /// Una sola query — reemplaza el N+1 de CargarAprobadas en el cliente.
    /// </summary>
    Task<List<int>> GetCapacitacionesAprobadasPorColaboradorAsync(int colaboradorId);

    /// <summary>
    /// Cuenta cuántos colaboradores inscritos en una capacitación han respondido su cuestionario.
    /// </summary>
    Task<int> ContarRespuestasCapacitacionAsync(int capacitacionId);
}
