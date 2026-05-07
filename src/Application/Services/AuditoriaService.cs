using TalentManagement.Application.Interfaces;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Application.Services;

public class AuditoriaService(IAuditLogRepository auditRepository)
{
    public async Task<AuditLogPagedDto> GetPagedAsync(
        string? entidadTipo, string? accion, int? colaboradorId,
        DateTime? desde, DateTime? hasta,
        int pagina, int tamano)
    {
        tamano = Math.Clamp(tamano, 1, 100);
        pagina = Math.Max(1, pagina);

        var (items, total) = await auditRepository.GetPagedAsync(
            entidadTipo, accion, colaboradorId, desde, hasta, pagina, tamano);

        var dtos = items.Select(l => new AuditLogDto
        {
            Id = l.Id,
            EntidadTipo = l.EntidadTipo,
            EntidadId = l.EntidadId,
            EntidadNombre = l.EntidadNombre,
            Accion = l.Accion,
            ColaboradorId = l.ColaboradorId,
            ColaboradorNombre = l.ColaboradorNombre,
            FechaHora = l.FechaHora,
            Observaciones = l.Observaciones,
            CamposModificados = l.CamposModificados
        }).ToList();

        return new AuditLogPagedDto
        {
            Items = dtos,
            Total = total,
            Pagina = pagina,
            Tamano = tamano
        };
    }

    public async Task<byte[]> ExportarCsvAsync(
        string? entidadTipo, DateTime? desde, DateTime? hasta)
    {
        var (items, _) = await auditRepository.GetPagedAsync(
            entidadTipo, null, null, desde, hasta, 1, 5000);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Fecha,Entidad,ID,Nombre,Acción,Colaborador,Observaciones");

        foreach (var l in items)
        {
            var fecha = l.FechaHora.ToString("dd/MM/yyyy HH:mm");
            sb.AppendLine(
                $"\"{fecha}\"," +
                $"\"{l.EntidadTipo}\"," +
                $"{l.EntidadId}," +
                $"\"{l.EntidadNombre.Replace("\"", "\"\"")}\"," +
                $"\"{l.Accion}\"," +
                $"\"{l.ColaboradorNombre.Replace("\"", "\"\"")}\"," +
                $"\"{(l.Observaciones ?? string.Empty).Replace("\"", "\"\"")}\"");
        }

        return System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }
}
