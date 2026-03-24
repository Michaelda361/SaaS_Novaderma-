using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class RecursoRepository(AppDbContext context) : IRecursoRepository
{
    public async Task<IEnumerable<RecursoCapacitacion>> GetByCapacitacionAsync(int capacitacionId) =>
        await context.RecursosCapacitacion
            .AsNoTracking()
            .Where(r => r.CapacitacionId == capacitacionId)
            .OrderBy(r => r.Orden)
            .ToListAsync();

    public async Task<RecursoCapacitacion?> GetByIdAsync(int id) =>
        await context.RecursosCapacitacion.FindAsync(id);

    public async Task<RecursoCapacitacion> CreateAsync(RecursoCapacitacion recurso)
    {
        context.RecursosCapacitacion.Add(recurso);
        await context.SaveChangesAsync();
        return recurso;
    }

    public async Task<RecursoCapacitacion> UpdateAsync(RecursoCapacitacion recurso)
    {
        context.RecursosCapacitacion.Update(recurso);
        await context.SaveChangesAsync();
        return recurso;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var recurso = await context.RecursosCapacitacion.FindAsync(id);
        if (recurso is null) return false;
        recurso.Activo = false;
        await context.SaveChangesAsync();
        return true;
    }
}
