using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;

namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Mock de IAuditExcelService para desarrollo — solo loguea en consola.
/// </summary>
public class MockAuditExcelService : IAuditExcelService
{
    public Task AppendRowAsync(AuditLog log)
    {
        Console.WriteLine($"[AuditExcel MOCK] {log.FechaHora:dd/MM/yyyy HH:mm} | " +
                          $"{log.Accion} | {log.EntidadNombre} | {log.ColaboradorNombre} | " +
                          $"{log.Observaciones}");
        return Task.CompletedTask;
    }
}
