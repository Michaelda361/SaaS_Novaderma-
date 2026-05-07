using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task<IEnumerable<AuditLog>> GetByEntidadAsync(string tipo, int entidadId) =>
        await context.AuditLogs
            .Where(a => a.EntidadTipo == tipo && a.EntidadId == entidadId)
            .OrderByDescending(a => a.FechaHora)
            .AsNoTracking()
            .ToListAsync();

    public async Task<(IEnumerable<AuditLog> Items, int Total)> GetPagedAsync(
        string? entidadTipo, string? accion, int? colaboradorId,
        DateTime? desde, DateTime? hasta,
        int pagina, int tamano)
    {
        var query = context.AuditLogs
            .Include(a => a.Colaborador)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entidadTipo))
            query = query.Where(a => a.EntidadTipo == entidadTipo);

        if (!string.IsNullOrWhiteSpace(accion))
            query = query.Where(a => a.Accion == accion);

        if (colaboradorId.HasValue)
            query = query.Where(a => a.ColaboradorId == colaboradorId);

        if (desde.HasValue)
            query = query.Where(a => a.FechaHora >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(a => a.FechaHora <= hasta.Value.AddDays(1));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.FechaHora)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return (items, total);
    }

    public async Task CreateAsync(AuditLog log)
    {
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();
    }
}
