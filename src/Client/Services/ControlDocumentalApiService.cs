using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.ControlDocumental;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Client.Services;

public class ControlDocumentalApiService(HttpClient http)
{
    private const string Base = "api/v1/control-documental";
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<ListadoMaestroDto>> GetListadosMaestrosAsync()
    {
        var response = await http.GetAsync($"{Base}/listados-maestros");
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<ListadoMaestroDto>>(stream, JsonOptions) ?? [];
    }

    public async Task<List<DocumentoControlDto>> GetDocumentosAsync(
        int listadoMaestroId,
        int? areaId = null,
        string? busqueda = null,
        string? codigo = null,
        string? proceso = null,
        string? estado = null)
    {
        var qs = new List<string> { $"listadoMaestroId={listadoMaestroId}" };
        if (areaId.HasValue) qs.Add($"areaId={areaId.Value}");
        if (!string.IsNullOrWhiteSpace(busqueda)) qs.Add($"busqueda={Uri.EscapeDataString(busqueda)}");
        if (!string.IsNullOrWhiteSpace(codigo)) qs.Add($"codigo={Uri.EscapeDataString(codigo)}");
        if (!string.IsNullOrWhiteSpace(proceso)) qs.Add($"proceso={Uri.EscapeDataString(proceso)}");
        if (!string.IsNullOrWhiteSpace(estado)) qs.Add($"estado={Uri.EscapeDataString(estado)}");

        var url = $"{Base}/documentos?{string.Join("&", qs)}";
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<DocumentoControlDto>>(stream, JsonOptions) ?? [];
    }

    public async Task<DocumentoControlDetalleDto?> GetDocumentoAsync(int id)
    {
        var response = await http.GetAsync($"{Base}/documentos/{id}");
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<DocumentoControlDetalleDto>(stream, JsonOptions);
    }

    public async Task<HttpResponseMessage> CreateDocumentoAsync(CreateDocumentoControlDto dto)
    {
        return await http.PostAsJsonAsync($"{Base}/documentos", dto);
    }

    public async Task<HttpResponseMessage> UpdateDocumentoAsync(int id, UpdateDocumentoControlDto dto)
    {
        return await http.PutAsJsonAsync($"{Base}/documentos/{id}", dto);
    }

    public async Task<HttpResponseMessage> CreateSolicitudCambioAsync(int documentoId, UpdateDocumentoControlDto dto)
    {
        return await http.PostAsJsonAsync($"{Base}/documentos/{documentoId}/solicitudes", dto);
    }

    public async Task<HttpResponseMessage> CreateListadoMaestroAsync(CreateListadoMaestroDto dto)
    {
        return await http.PostAsJsonAsync($"{Base}/listados-maestros", dto);
    }

    public async Task<HttpResponseMessage> UpdateListadoMaestroAsync(int id, CreateListadoMaestroDto dto)
    {
        return await http.PutAsJsonAsync($"{Base}/listados-maestros/{id}", dto);
    }

    public async Task<HttpResponseMessage> DeleteListadoMaestroAsync(int id)
    {
        return await http.DeleteAsync($"{Base}/listados-maestros/{id}");
    }

    public async Task<ListadoMaestroDto?> GetListadoMaestroAsync(int id)
    {
        var response = await http.GetAsync($"{Base}/listados-maestros/{id}");
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<ListadoMaestroDto>(stream, JsonOptions);
    }

    public async Task<HttpResponseMessage> ExportListadoAsync(int listadoId)
    {
        return await http.GetAsync($"{Base}/listados-maestros/{listadoId}/export");
    }

    public async Task<HttpResponseMessage> ImportListadoAsync(MultipartFormDataContent content)
    {
        return await http.PostAsync($"{Base}/listados-maestros/import", content);
    }

    public async Task<List<AuditLogDto>> GetHistorialAsync(int documentoId)
    {
        var response = await http.GetAsync($"{Base}/documentos/{documentoId}/auditoria");
        if (!response.IsSuccessStatusCode) return [];
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<AuditLogDto>>(stream, JsonOptions) ?? [];
    }

    public async Task<List<SolicitudCambioDocumentoControlDto>> GetSolicitudesPendientesAsync()
    {
        var response = await http.GetAsync($"{Base}/solicitudes/pendientes");
        if (!response.IsSuccessStatusCode) return [];
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<SolicitudCambioDocumentoControlDto>>(stream, JsonOptions) ?? [];
    }

    public async Task<List<SolicitudCambioDocumentoControlDto>> GetSolicitudesPorDocumentoAsync(int documentoId)
    {
        var response = await http.GetAsync($"{Base}/documentos/{documentoId}/solicitudes");
        if (!response.IsSuccessStatusCode) return [];
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<SolicitudCambioDocumentoControlDto>>(stream, JsonOptions) ?? [];
    }

    public async Task<int> CountSolicitudesPendientesAsync()
    {
        var response = await http.GetAsync($"{Base}/solicitudes/pendientes/count");
        if (!response.IsSuccessStatusCode) return 0;
        return await response.Content.ReadFromJsonAsync<int>(JsonOptions);
    }

    public async Task<HttpResponseMessage> AprobarSolicitudCambioAsync(int solicitudId)
    {
        return await http.PostAsync($"{Base}/solicitudes/{solicitudId}/aprobar", null);
    }

    public async Task<HttpResponseMessage> RechazarSolicitudCambioAsync(int solicitudId, RechazarSolicitudCambioDto dto)
    {
        return await http.PostAsJsonAsync($"{Base}/solicitudes/{solicitudId}/rechazar", dto);
    }

    // ────── Métodos de Permisos ──────

    public async Task<List<ListadoMaestroPermisoDto>> GetListadoPermisosAsync(int listadoId)
    {
        var response = await http.GetAsync($"{Base}/listados-maestros/{listadoId}/permisos");
        if (!response.IsSuccessStatusCode) return [];
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<ListadoMaestroPermisoDto>>(stream, JsonOptions) ?? [];
    }

    public async Task<ListadoMaestroPermisoDto?> GetListadoPermisosActualAsync(int listadoId)
    {
        var response = await http.GetAsync($"{Base}/listados-maestros/{listadoId}/permisos/actual");
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<ListadoMaestroPermisoDto>(stream, JsonOptions);
    }

    public async Task<HttpResponseMessage> UpdateListadoPermisosAsync(int listadoId, List<ListadoMaestroPermisoUpdateDto> permisos)
    {
        return await http.PostAsJsonAsync($"{Base}/listados-maestros/{listadoId}/permisos", permisos);
    }
}
