using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class AreaRepository(AppDbContext context, IMemoryCache cache) : IAreaRepository
{
    private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

    public async Task<IEnumerable<Area>> GetAllAsync()
    {
        if (cache.TryGetValue("areas_all", out IEnumerable<Area>? hit)) return hit!;
        var result = await context.Areas
            .Include(a => a.Cargos).Include(a => a.Colaboradores).Include(a => a.Jefe)
            .AsNoTracking().ToListAsync();
        cache.Set("areas_all", result, _ttl);
        return result;
    }

    public async Task<Area?> GetByIdAsync(int id) =>
        await context.Areas
            .Include(a => a.Cargos)
            .Include(a => a.Colaboradores).ThenInclude(c => c.Cargo)
            .Include(a => a.Jefe)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Area> CreateAsync(Area area)
    {
        context.Areas.Add(area);
        await context.SaveChangesAsync();
        cache.Remove("areas_all");
        return area;
    }

    public async Task<Area> UpdateAsync(Area area)
    {
        context.Areas.Update(area);
        await context.SaveChangesAsync();
        cache.Remove("areas_all");
        return area;
    }

    public async Task DeleteAsync(int id)
    {
        var area = await context.Areas.FindAsync(id);
        if (area is null) return;
        area.Activo = false;
        await context.SaveChangesAsync();
        cache.Remove("areas_all");
    }

    public async Task<IEnumerable<Area>> GetInactivasAsync() =>
        await context.Areas
            .IgnoreQueryFilters()
            .Where(a => !a.Activo)
            .AsNoTracking()
            .ToListAsync();

    public async Task RestaurarAsync(int id)
    {
        var area = await context.Areas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id);
        if (area is null) return;
        area.Activo = true;
        await context.SaveChangesAsync();
        cache.Remove("areas_all");
    }
}
