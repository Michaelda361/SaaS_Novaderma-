using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface ICertificadoRepository
{
    Task<IEnumerable<Certificado>> GetAllAsync();
    Task<IEnumerable<Certificado>> GetByColaboradorAsync(int colaboradorId);
    Task<Certificado?> GetByIdAsync(int id);
    Task<Certificado> CreateAsync(Certificado certificado);
    Task<Certificado> UpdateAsync(Certificado certificado);
    Task DeleteAsync(int id);
    Task<IEnumerable<Certificado>> GetVencidosAsync();
    Task<IEnumerable<Certificado>> GetProximosAVencerAsync(int diasAlerta = 30);
}
