using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class CapacitacionRepository(AppDbContext context) : ICapacitacionRepository
{
    public async Task<IEnumerable<Capacitacion>> GetAllAsync() =>
        await context.Capacitaciones
            .Include(c => c.Inscripciones)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Capacitacion?> GetByIdAsync(int id) =>
        await context.Capacitaciones
            .Include(c => c.Inscripciones)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Capacitacion> CreateAsync(Capacitacion capacitacion)
    {
        context.Capacitaciones.Add(capacitacion);
        await context.SaveChangesAsync();
        return capacitacion;
    }

    public async Task<Capacitacion> UpdateAsync(Capacitacion capacitacion)
    {
        context.Capacitaciones.Update(capacitacion);
        await context.SaveChangesAsync();
        return capacitacion;
    }

    public async Task DeleteAsync(int id)
    {
        var cap = await context.Capacitaciones.FindAsync(id);
        if (cap is null) return;
        cap.Activo = false;
        await context.SaveChangesAsync();
    }
}
