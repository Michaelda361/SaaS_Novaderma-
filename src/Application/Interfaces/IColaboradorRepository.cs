using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IColaboradorRepository
{
    Task<IEnumerable<Colaborador>> GetAllAsync();
    Task<Colaborador?> GetByIdAsync(int id);
    Task<Colaborador?> GetByEmailAsync(string email);
    Task<Colaborador> CreateAsync(Colaborador colaborador);
    Task<Colaborador> UpdateAsync(Colaborador colaborador);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> EsJefeDeAreaAsync(int colaboradorId);
    Task<IEnumerable<Colaborador>> GetInactivosAsync();
    Task RestaurarAsync(int id);
}
