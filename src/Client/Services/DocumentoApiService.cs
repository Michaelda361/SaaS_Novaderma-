using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Client.Services;

public class DocumentoApiService(HttpClient http)
{
    private const string Base = "api/v1/documentos";
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<DocumentoDto>> GetAllAsync(
        string? tipo = null, string? estado = null,
        int? areaId = null, string? busqueda = null)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(tipo))    qs.Add($"tipo={Uri.EscapeDataString(tipo)}");
        if (!string.IsNullOrWhiteSpace(estado))  qs.Add($"estado={Uri.EscapeDataString(estado)}");
        if (areaId.HasValue)                     qs.Add($"areaId={areaId}");
        if (!string.IsNullOrWhiteSpace(busqueda)) qs.Add($"busqueda={Uri.EscapeDataString(busqueda)}");

        var url = qs.Count > 0 ? $"{Base}?{string.Join("&", qs)}" : Base;
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<DocumentoDto>>(stream, JsonOptions) ?? [];
    }

    public async Task<DocumentoDetalleDto?> GetByIdAsync(int id)
    {
        var response = await http.GetAsync($"{Base}/{id}");
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<DocumentoDetalleDto>(stream, JsonOptions);
    }

    public async Task<DocumentoDto?> CreateAsync(CreateDocumentoDto dto, IBrowserFile archivo)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(dto.Titulo), "Titulo");
        content.Add(new StringContent(dto.TipoDocumento), "TipoDocumento");
        if (dto.AreaId.HasValue)
            content.Add(new StringContent(dto.AreaId.Value.ToString()), "AreaId");

        var fileContent = new StreamContent(archivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
        fileContent.Headers.ContentType = new(archivo.ContentType);
        content.Add(fileContent, "archivo", archivo.Name);

        var response = await http.PostAsync(Base, content);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<DocumentoDto>(stream, JsonOptions);
    }

    public async Task<DocumentoDto?> CreateDesdeUrlAsync(CreateDocumentoDto dto)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(dto.Titulo), "Titulo");
        content.Add(new StringContent(dto.TipoDocumento), "TipoDocumento");
        if (dto.AreaId.HasValue)
            content.Add(new StringContent(dto.AreaId.Value.ToString()), "AreaId");
        if (!string.IsNullOrWhiteSpace(dto.UrlExterna))
            content.Add(new StringContent(dto.UrlExterna), "UrlExterna");
        if (!string.IsNullOrWhiteSpace(dto.NombreArchivoExterno))
            content.Add(new StringContent(dto.NombreArchivoExterno), "NombreArchivoExterno");

        var response = await http.PostAsync(Base, content);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<DocumentoDto>(stream, JsonOptions);
    }

    public async Task<DocumentoDto?> UpdateMetadatosAsync(int id, UpdateDocumentoDto dto)
    {
        var response = await http.PutAsJsonAsync($"{Base}/{id}/metadatos", dto);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<DocumentoDto>(stream, JsonOptions);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await http.DeleteAsync($"{Base}/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<DocumentoDto?> SubirNuevaVersionAsync(
        int id, IBrowserFile archivo, bool incrementoMayor)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(archivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
        fileContent.Headers.ContentType = new(archivo.ContentType);
        content.Add(fileContent, "archivo", archivo.Name);

        var response = await http.PostAsync(
            $"{Base}/{id}/version?incrementoMayor={incrementoMayor}", content);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<DocumentoDto>(stream, JsonOptions);
    }

    public async Task<bool> AvanzarEstadoAsync(int id)
    {
        var response = await http.PostAsync($"{Base}/{id}/avanzar-estado", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> ObtenerUrlDescargaAsync(int id)
    {
        var detalle = await GetByIdAsync(id);
        return detalle?.SharePointUrl;
    }

    public async Task<string?> ObtenerUrlEdicionAsync(int id)
    {
        try
        {
            var response = await http.GetAsync($"{Base}/{id}/editar");
            if (!response.IsSuccessStatusCode) return null;
            var raw = await response.Content.ReadAsStringAsync();
            // El controller devuelve Ok(string) que serializa con comillas JSON
            return raw.Trim('"');
        }
        catch { return null; }
    }

    public async Task<bool> CrearPropuestaAsync(
        int documentoId, CreatePropuestaDto dto, IBrowserFile? archivo)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(dto.Descripcion), "Descripcion");

        if (archivo is not null)
        {
            var fileContent = new StreamContent(archivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
            fileContent.Headers.ContentType = new(archivo.ContentType);
            content.Add(fileContent, "archivo", archivo.Name);
        }

        var response = await http.PostAsync($"{Base}/{documentoId}/propuestas", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<PropuestaModificacionDto>> GetPropuestasPendientesAsync()
    {
        var response = await http.GetAsync($"{Base}/propuestas/pendientes");
        if (!response.IsSuccessStatusCode) return [];
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<PropuestaModificacionDto>>(stream, JsonOptions) ?? [];
    }

    public async Task<int> GetPropuestasPendientesCountAsync()
    {
        try
        {
            var response = await http.GetAsync($"{Base}/propuestas/pendientes/count");
            if (!response.IsSuccessStatusCode) return 0;
            await using var stream = await response.Content.ReadAsStreamAsync();
            var result = await System.Text.Json.JsonSerializer.DeserializeAsync<int>(stream, JsonOptions);
            return result;
        }
        catch { return 0; }
    }

    public async Task<bool> AprobarPropuestaAsync(int propuestaId)
    {
        var response = await http.PostAsync(
            $"{Base}/propuestas/{propuestaId}/aprobar", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RechazarPropuestaAsync(int propuestaId, string motivo)
    {
        var response = await http.PostAsJsonAsync(
            $"{Base}/propuestas/{propuestaId}/rechazar",
            new RechazarPropuestaDto { MotivoRechazo = motivo });
        return response.IsSuccessStatusCode;
    }

    public async Task<List<AuditLogDto>> GetAuditLogAsync(int documentoId)
    {
        var response = await http.GetAsync($"{Base}/{documentoId}/auditoria");
        if (!response.IsSuccessStatusCode) return [];
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await System.Text.Json.JsonSerializer.DeserializeAsync<List<AuditLogDto>>(stream, JsonOptions) ?? [];
    }
}
