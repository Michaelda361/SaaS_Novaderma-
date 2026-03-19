using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IAreaRepository
{
    Task<IEnumerable<Area>> GetAllAsync();
    Task<Area?> GetByIdAsync(int id);
    Task<Area> CreateAsync(Area area);
    Task<Area> UpdateAsync(Area area);
    Task DeleteAsync(int id);
}
