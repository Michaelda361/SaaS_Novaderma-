using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class CargoRepository(AppDbContext context) : ICargoRepository
{
    public async Task<IEnumerable<Cargo>> GetAllAsync() =>
        await context.Cargos
            .Include(c => c.Area)
            .Include(c => c.Colaboradores)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Cargo>> GetByAreaAsync(int areaId) =>
        await context.Cargos
            .Include(c => c.Area)
            .Where(c => c.AreaId == areaId)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Cargo?> GetByIdAsync(int id) =>
        await context.Cargos
            .Include(c => c.Area)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Cargo> CreateAsync(Cargo cargo)
    {
        context.Cargos.Add(cargo);
        await context.SaveChangesAsync();
        return cargo;
    }

    public async Task<Cargo> UpdateAsync(Cargo cargo)
    {
        context.Cargos.Update(cargo);
        await context.SaveChangesAsync();
        return cargo;
    }

    public async Task DeleteAsync(int id)
    {
        var cargo = await context.Cargos.FindAsync(id);
        if (cargo is null) return;
        cargo.Activo = false;
        await context.SaveChangesAsync();
    }
}
