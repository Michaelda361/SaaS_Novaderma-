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

    Task<List<ColaboradorCampoDefinicion>> GetCamposDefinicionAsync();
    Task<ColaboradorCampoDefinicion?> GetCampoDefinicionAsync(int id);
    Task<ColaboradorCampoDefinicion> CreateCampoDefinicionAsync(ColaboradorCampoDefinicion campo);
    Task<ColaboradorCampoDefinicion> UpdateCampoDefinicionAsync(ColaboradorCampoDefinicion campo);
    Task DeleteCampoDefinicionAsync(int id);

    Task<Dictionary<string, string?>> GetValoresPorColaboradorAsync(int colaboradorId);
    Task SetValoresAdicionalesAsync(int colaboradorId, Dictionary<string, string?> valores);

    Task<IEnumerable<Colaborador>> GetInactivosAsync();
    Task RestaurarAsync(int id);
}
