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

    public async Task CreateAsync(AuditLog log)
    {
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();
    }
}
