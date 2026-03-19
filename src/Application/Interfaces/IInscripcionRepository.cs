using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IInscripcionRepository
{
    Task<IEnumerable<Inscripcion>> GetByCapacitacionAsync(int capacitacionId);
    Task<IEnumerable<Inscripcion>> GetByColaboradorAsync(int colaboradorId);
    Task<Inscripcion?> GetByIdAsync(int id);
    Task<Inscripcion> CreateAsync(Inscripcion inscripcion);
    Task<Inscripcion> UpdateAsync(Inscripcion inscripcion);
    Task<bool> DeleteAsync(int id);
}
