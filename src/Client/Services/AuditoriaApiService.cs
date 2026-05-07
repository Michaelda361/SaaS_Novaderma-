using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Client.Services;

public class AuditoriaApiService(HttpClient http)
{
    private const string Base = "api/v1/auditoria";

    public async Task<AuditLogPagedDto?> GetPagedAsync(
        string? entidadTipo = null,
        string? accion = null,
        int? colaboradorId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        int pagina = 1,
        int tamano = 25)
    {
        var qs = new List<string> { $"pagina={pagina}", $"tamano={tamano}" };
        if (!string.IsNullOrWhiteSpace(entidadTipo)) qs.Add($"entidadTipo={Uri.EscapeDataString(entidadTipo)}");
        if (!string.IsNullOrWhiteSpace(accion))      qs.Add($"accion={Uri.EscapeDataString(accion)}");
        if (colaboradorId.HasValue)                  qs.Add($"colaboradorId={colaboradorId}");
        if (desde.HasValue)                          qs.Add($"desde={desde.Value:yyyy-MM-dd}");
        if (hasta.HasValue)                          qs.Add($"hasta={hasta.Value:yyyy-MM-dd}");

        return await http.GetFromJsonAsync<AuditLogPagedDto>($"{Base}?{string.Join("&", qs)}");
    }

    public string GetExportarUrl(string? entidadTipo, DateTime? desde, DateTime? hasta)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(entidadTipo)) qs.Add($"entidadTipo={Uri.EscapeDataString(entidadTipo)}");
        if (desde.HasValue) qs.Add($"desde={desde.Value:yyyy-MM-dd}");
        if (hasta.HasValue) qs.Add($"hasta={hasta.Value:yyyy-MM-dd}");
        // BaseAddress del HttpClient apunta al servidor (5194), no al cliente (5185)
        var baseUrl = http.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5194";
        var path = "api/v1/auditoria/exportar";
        return qs.Count > 0 ? $"{baseUrl}/{path}?{string.Join("&", qs)}" : $"{baseUrl}/{path}";
    }
}
