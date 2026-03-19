using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface ICargoRepository
{
    Task<IEnumerable<Cargo>> GetAllAsync();
    Task<IEnumerable<Cargo>> GetByAreaAsync(int areaId);
    Task<Cargo?> GetByIdAsync(int id);
    Task<Cargo> CreateAsync(Cargo cargo);
    Task<Cargo> UpdateAsync(Cargo cargo);
    Task DeleteAsync(int id);
}
