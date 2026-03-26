using TalentManagement.Domain.Entities;

namespace TalentManagement.Application.Interfaces;

/// <summary>
/// Escribe una fila de auditoría en el Excel de SharePoint.
/// </summary>
public interface IAuditExcelService
{
    Task AppendRowAsync(AuditLog log);
}
