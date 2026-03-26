using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class ColaboradorRepository(AppDbContext context) : IColaboradorRepository
{
    public async Task<IEnumerable<Colaborador>> GetAllAsync() =>
        await context.Colaboradores
            .Include(c => c.Area)
            .Include(c => c.Cargo)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Colaborador?> GetByIdAsync(int id) =>
        await context.Colaboradores
            .Include(c => c.Area)
            .Include(c => c.Cargo)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Colaborador?> GetByEmailAsync(string email) =>
        await context.Colaboradores
            .Include(c => c.Area)
            .Include(c => c.Cargo)
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());

    public async Task<Colaborador> CreateAsync(Colaborador colaborador)
    {
        context.Colaboradores.Add(colaborador);
        await context.SaveChangesAsync();
        return await GetByIdAsync(colaborador.Id) ?? colaborador;
    }

    public async Task<Colaborador> UpdateAsync(Colaborador colaborador)
    {
        context.Colaboradores.Update(colaborador);
        await context.SaveChangesAsync();
        return await GetByIdAsync(colaborador.Id) ?? colaborador;
    }

    public async Task DeleteAsync(int id)
    {
        var colaborador = await context.Colaboradores.FindAsync(id);
        if (colaborador is null) return;
        // Soft delete
        colaborador.Activo = false;
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id) =>
        await context.Colaboradores.AnyAsync(c => c.Id == id);
}
