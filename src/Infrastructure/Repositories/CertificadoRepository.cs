using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class CertificadoRepository(AppDbContext context) : ICertificadoRepository
{
    public async Task<IEnumerable<Certificado>> GetByColaboradorAsync(int colaboradorId) =>
        await context.Certificados
            .Include(c => c.Colaborador)
            .Where(c => c.ColaboradorId == colaboradorId)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Certificado?> GetByIdAsync(int id) =>
        await context.Certificados
            .Include(c => c.Colaborador)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Certificado> CreateAsync(Certificado certificado)
    {
        context.Certificados.Add(certificado);
        await context.SaveChangesAsync();
        return await GetByIdAsync(certificado.Id) ?? certificado;
    }

    public async Task<Certificado> UpdateAsync(Certificado certificado)
    {
        context.Certificados.Update(certificado);
        await context.SaveChangesAsync();
        return certificado;
    }

    public async Task DeleteAsync(int id)
    {
        var cert = await context.Certificados.FindAsync(id);
        if (cert is null) return;
        cert.Activo = false;
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Certificado>> GetVencidosAsync() =>
        await context.Certificados
            .IgnoreQueryFilters()
            .Include(c => c.Colaborador)
            .Where(c => c.Activo && c.FechaVencimiento.HasValue && c.FechaVencimiento.Value < DateTime.Today)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Certificado>> GetProximosAVencerAsync(int diasAlerta = 30) =>
        await context.Certificados
            .Include(c => c.Colaborador)
            .Where(c => c.FechaVencimiento.HasValue
                && c.FechaVencimiento.Value >= DateTime.Today
                && c.FechaVencimiento.Value <= DateTime.Today.AddDays(diasAlerta))
            .AsNoTracking()
            .ToListAsync();
}
