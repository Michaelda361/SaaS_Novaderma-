using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetByEntidadAsync(string tipo, int entidadId);
    Task CreateAsync(AuditLog log);
}
