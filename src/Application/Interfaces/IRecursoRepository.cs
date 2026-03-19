using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IRecursoRepository
{
    Task<IEnumerable<RecursoCapacitacion>> GetByCapacitacionAsync(int capacitacionId);
    Task<RecursoCapacitacion?> GetByIdAsync(int id);
    Task<RecursoCapacitacion> CreateAsync(RecursoCapacitacion recurso);
    Task<RecursoCapacitacion> UpdateAsync(RecursoCapacitacion recurso);
    Task<bool> DeleteAsync(int id);
}
