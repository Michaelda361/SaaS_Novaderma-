using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class AreaRepository(AppDbContext context) : IAreaRepository
{
    public async Task<IEnumerable<Area>> GetAllAsync() =>
        await context.Areas
            .Include(a => a.Cargos)
            .Include(a => a.Colaboradores)
            .Include(a => a.Jefe)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Area?> GetByIdAsync(int id) =>
        await context.Areas
            .Include(a => a.Cargos)
            .Include(a => a.Colaboradores)
                .ThenInclude(c => c.Cargo)
            .Include(a => a.Jefe)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Area> CreateAsync(Area area)
    {
        context.Areas.Add(area);
        await context.SaveChangesAsync();
        return area;
    }

    public async Task<Area> UpdateAsync(Area area)
    {
        context.Areas.Update(area);
        await context.SaveChangesAsync();
        return area;
    }

    public async Task DeleteAsync(int id)
    {
        var area = await context.Areas.FindAsync(id);
        if (area is null) return;
        area.Activo = false;
        await context.SaveChangesAsync();
    }
}
