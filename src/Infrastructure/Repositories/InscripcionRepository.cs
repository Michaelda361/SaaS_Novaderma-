using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class InscripcionRepository(AppDbContext context) : IInscripcionRepository
{
    // WithIncludes para gestión (solo capacitaciones activas)
    private IQueryable<Inscripcion> WithIncludes() =>
        context.Inscripciones
            .Include(i => i.Colaborador).ThenInclude(c => c.Area)
            .Include(i => i.Colaborador).ThenInclude(c => c.Cargo)
            .Include(i => i.Capacitacion);

    public async Task<IEnumerable<Inscripcion>> GetByCapacitacionAsync(int capacitacionId) =>
        await WithIncludes().AsNoTracking()
            .Where(i => i.CapacitacionId == capacitacionId)
            .ToListAsync();

    // Para el historial del admin: incluye inscripciones aunque la capacitacion este eliminada
    public async Task<IEnumerable<Inscripcion>> GetByCapacitacionIgnorandoFiltrosAsync(int capacitacionId) =>
        await context.Inscripciones
            .IgnoreQueryFilters()
            .Include(i => i.Colaborador).ThenInclude(c => c.Area)
            .Include(i => i.Colaborador).ThenInclude(c => c.Cargo)
            .Include(i => i.Capacitacion)
            .Where(i => i.CapacitacionId == capacitacionId && i.Activo)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Inscripcion>> GetByColaboradorAsync(int colaboradorId) =>
        // IgnoreQueryFilters para incluir capacitaciones eliminadas (soft delete)
        // asi el historial del colaborador no pierde registros cuando se elimina una capacitacion
        await context.Inscripciones
            .IgnoreQueryFilters()
            .Include(i => i.Colaborador).ThenInclude(c => c.Area)
            .Include(i => i.Colaborador).ThenInclude(c => c.Cargo)
            .Include(i => i.Capacitacion)
            .Where(i => i.ColaboradorId == colaboradorId && i.Activo && i.Colaborador.Activo)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Inscripcion?> GetByIdAsync(int id) =>
        await WithIncludes().FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Inscripcion> CreateAsync(Inscripcion inscripcion)
    {
        context.Inscripciones.Add(inscripcion);
        await context.SaveChangesAsync();
        return (await GetByIdAsync(inscripcion.Id))!;
    }

    public async Task<Inscripcion> UpdateAsync(Inscripcion inscripcion)
    {
        context.Inscripciones.Update(inscripcion);
        await context.SaveChangesAsync();
        return (await GetByIdAsync(inscripcion.Id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var inscripcion = await context.Inscripciones.FindAsync(id);
        if (inscripcion is null) return false;
        inscripcion.Activo = false;
        await context.SaveChangesAsync();
        return true;
    }
}
