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
    Task<RespuestaCuestionario> SaveRespuestaAsync(RespuestaCuestionario respuesta);
}
