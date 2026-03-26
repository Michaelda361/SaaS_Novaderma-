using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class PlantillaDocumentoRepository(AppDbContext context) : IPlantillaDocumentoRepository
{
    private IQueryable<PlantillaDocumento> WithAreas() =>
        context.PlantillasDocumento
            .Include(p => p.Areas).ThenInclude(a => a.Area);

    public async Task<IEnumerable<PlantillaDocumento>> GetAllAsync() =>
        await WithAreas().AsNoTracking().ToListAsync();

    public async Task<IEnumerable<PlantillaDocumento>> GetByAreaAsync(int areaId) =>
        await WithAreas()
            .Where(p => p.AplicaTodasAreas || p.Areas.Any(a => a.AreaId == areaId))
            .AsNoTracking()
            .ToListAsync();

    public async Task<PlantillaDocumento?> GetByIdAsync(int id) =>
        await WithAreas().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PlantillaDocumento> CreateAsync(PlantillaDocumento plantilla)
    {
        context.PlantillasDocumento.Add(plantilla);
        await context.SaveChangesAsync();
        return await GetByIdAsync(plantilla.Id) ?? plantilla;
    }

    public async Task<PlantillaDocumento> UpdateAsync(PlantillaDocumento plantilla)
    {
        // Eliminar áreas anteriores y reemplazar
        var areasExistentes = await context.PlantillaDocumentoAreas
            .Where(a => a.PlantillaDocumentoId == plantilla.Id)
            .ToListAsync();
        context.PlantillaDocumentoAreas.RemoveRange(areasExistentes);

        context.PlantillasDocumento.Update(plantilla);
        await context.SaveChangesAsync();
        return await GetByIdAsync(plantilla.Id) ?? plantilla;
    }

    public async Task DeleteAsync(int id)
    {
        var p = await context.PlantillasDocumento.FindAsync(id);
        if (p is null) return;
        p.Activo = false;
        await context.SaveChangesAsync();
    }

    public async Task<SolicitudDocumento> CreateSolicitudAsync(SolicitudDocumento solicitud)
    {
        context.SolicitudesDocumento.Add(solicitud);
        await context.SaveChangesAsync();
        return solicitud;
    }

    public async Task<IEnumerable<SolicitudDocumento>> GetSolicitudesByColaboradorAsync(int colaboradorId) =>
        await context.SolicitudesDocumento
            .Include(s => s.PlantillaDocumento)
            .Include(s => s.Colaborador)
            .Where(s => s.ColaboradorId == colaboradorId)
            .OrderByDescending(s => s.FechaSolicitud)
            .AsNoTracking()
            .ToListAsync();
}
