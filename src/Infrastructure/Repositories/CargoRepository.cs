using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class CargoRepository(AppDbContext context, IMemoryCache cache) : ICargoRepository
{
    private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

    public async Task<IEnumerable<Cargo>> GetAllAsync()
    {
        if (cache.TryGetValue("cargos_all", out IEnumerable<Cargo>? hit)) return hit!;
        var result = await context.Cargos
            .Include(c => c.Area).Include(c => c.Colaboradores)
            .AsNoTracking().ToListAsync();
        cache.Set("cargos_all", result, _ttl);
        return result;
    }

    public async Task<IEnumerable<Cargo>> GetByAreaAsync(int areaId)
    {
        var key = $"cargos_area_{areaId}";
        if (cache.TryGetValue(key, out IEnumerable<Cargo>? hit)) return hit!;
        var result = await context.Cargos
            .Include(c => c.Area).Where(c => c.AreaId == areaId)
            .AsNoTracking().ToListAsync();
        cache.Set(key, result, _ttl);
        return result;
    }

    public async Task<Cargo?> GetByIdAsync(int id) =>
        await context.Cargos.Include(c => c.Area).AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Cargo> CreateAsync(Cargo cargo)
    {
        context.Cargos.Add(cargo);
        await context.SaveChangesAsync();
        InvalidarCache(cargo.AreaId);
        return cargo;
    }

    public async Task<Cargo> UpdateAsync(Cargo cargo)
    {
        // Fetch a tracked entity (no includes) and only update scalar fields
        // to avoid EF Core trying to re-attach navigation collections.
        var tracked = await context.Cargos.FindAsync(cargo.Id);
        if (tracked is null) return cargo;
        tracked.Nombre = cargo.Nombre;
        tracked.Descripcion = cargo.Descripcion;
        tracked.AreaId = cargo.AreaId;
        await context.SaveChangesAsync();
        InvalidarCache(tracked.AreaId);
        return tracked;
    }

    public async Task DeleteAsync(int id)
    {
        var cargo = await context.Cargos.FindAsync(id);
        if (cargo is null) return;
        cargo.Activo = false;
        await context.SaveChangesAsync();
        InvalidarCache(cargo.AreaId);
    }

    private void InvalidarCache(int areaId = 0)
    {
        cache.Remove("cargos_all");
        if (areaId > 0) cache.Remove($"cargos_area_{areaId}");
    }
}
