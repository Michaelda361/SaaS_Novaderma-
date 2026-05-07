using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetByEntidadAsync(string tipo, int entidadId);
    Task<(IEnumerable<AuditLog> Items, int Total)> GetPagedAsync(
        string? entidadTipo, string? accion, int? colaboradorId,
        DateTime? desde, DateTime? hasta,
        int pagina, int tamano);
    Task CreateAsync(AuditLog log);
}
