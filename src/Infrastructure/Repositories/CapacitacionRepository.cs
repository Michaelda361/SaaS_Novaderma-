using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class CapacitacionRepository(AppDbContext context) : ICapacitacionRepository
{
    private IQueryable<Capacitacion> WithIncludes() =>
        context.Capacitaciones
            .Include(c => c.Area)
            .Include(c => c.Colaborador);

    private IQueryable<Capacitacion> WithIncludesFull() =>
        context.Capacitaciones
            .Include(c => c.Area)
            .Include(c => c.Colaborador)
            .Include(c => c.Inscripciones);

    public async Task<IEnumerable<Capacitacion>> GetAllAsync() =>
        await WithIncludes().AsNoTracking().ToListAsync();

    public async Task<IEnumerable<Capacitacion>> GetByAreaAsync(int areaId) =>
        await WithIncludes().AsNoTracking()
            .Where(c => c.AreaId == areaId)
            .ToListAsync();

    public async Task<IEnumerable<Capacitacion>> GetByColaboradorAsync(int colaboradorId) =>
        await WithIncludes().AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId)
            .ToListAsync();

    public async Task<Capacitacion?> GetByIdAsync(int id) =>
        await WithIncludesFull().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

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

    public async Task<IEnumerable<Capacitacion>> GetInactivasAsync() =>
        await context.Capacitaciones
            .IgnoreQueryFilters()
            .Where(c => !c.Activo)
            .Include(c => c.Area)
            .Include(c => c.Colaborador)
            .AsNoTracking()
            .ToListAsync();

    public async Task RestaurarAsync(int id)
    {
        var cap = await context.Capacitaciones
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cap is null) return;
        cap.Activo = true;
        await context.SaveChangesAsync();
    }
}
