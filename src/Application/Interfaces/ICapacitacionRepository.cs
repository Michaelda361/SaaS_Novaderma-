using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface ICapacitacionRepository
{
    Task<IEnumerable<Capacitacion>> GetAllAsync();
    Task<Capacitacion?> GetByIdAsync(int id);
    Task<Capacitacion> CreateAsync(Capacitacion capacitacion);
    Task<Capacitacion> UpdateAsync(Capacitacion capacitacion);
    Task DeleteAsync(int id);
}
