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

    public Task<bool> ExisteInscripcionAsync(int capacitacionId, int colaboradorId) =>
        context.Inscripciones.AnyAsync(i =>
            i.CapacitacionId == capacitacionId && i.ColaboradorId == colaboradorId);

    public async Task<List<(Inscripcion inscripcion, RespuestaCuestionario? respuesta, Cuestionario? cuestionario)>>
        GetHistorialCompletoAsync()
    {
        // 1. Todas las inscripciones activas con navegaciones — una sola query
        var inscripciones = await context.Inscripciones
            .IgnoreQueryFilters()
            .Include(i => i.Colaborador).ThenInclude(c => c.Area)
            .Include(i => i.Colaborador).ThenInclude(c => c.Cargo)
            .Include(i => i.Capacitacion)
            .Where(i => i.Activo && i.Colaborador.Activo)
            .AsNoTracking()
            .ToListAsync();

        if (inscripciones.Count == 0) return [];

        // 2. Cuestionarios de las capacitaciones involucradas — una sola query
        var capacitacionIds = inscripciones.Select(i => i.CapacitacionId).Distinct().ToList();
        var cuestionarios = await context.Cuestionarios
            .Where(c => capacitacionIds.Contains(c.CapacitacionId))
            .AsNoTracking()
            .ToListAsync();

        // 3. Respuestas de las inscripciones involucradas — una sola query
        var inscripcionIds = inscripciones.Select(i => i.Id).ToList();
        var cuestionarioIds = cuestionarios.Select(c => c.Id).ToList();
        var respuestas = await context.RespuestasCuestionario
            .Where(r => inscripcionIds.Contains(r.InscripcionId)
                     && cuestionarioIds.Contains(r.CuestionarioId))
            .AsNoTracking()
            .ToListAsync();

        // 4. Lookup O(1) para cruzar los datos en memoria
        var cuestionarioPorCapacitacion = cuestionarios.ToDictionary(c => c.CapacitacionId);
        var respuestaPorInscripcionYCuestionario = respuestas
            .ToDictionary(r => (r.InscripcionId, r.CuestionarioId));

        return inscripciones.Select(i =>
        {
            cuestionarioPorCapacitacion.TryGetValue(i.CapacitacionId, out var cuestionario);
            RespuestaCuestionario? respuesta = null;
            if (cuestionario is not null)
                respuestaPorInscripcionYCuestionario.TryGetValue((i.Id, cuestionario.Id), out respuesta);
            return (i, respuesta, cuestionario);
        }).ToList();
    }
}
