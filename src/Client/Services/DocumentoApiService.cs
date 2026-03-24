using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Documentos;

namespace TalentManagement.Client.Services;

public class DocumentoApiService(HttpClient http)
{
    private const string Base = "api/v1/documentos";

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
        return await http.GetFromJsonAsync<List<DocumentoDto>>(url) ?? [];
    }

    public async Task<DocumentoDetalleDto?> GetByIdAsync(int id) =>
        await http.GetFromJsonAsync<DocumentoDetalleDto>($"{Base}/{id}");

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
        return await response.Content.ReadFromJsonAsync<DocumentoDto>();
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
        return await response.Content.ReadFromJsonAsync<DocumentoDto>();
    }

    public async Task<DocumentoDto?> UpdateMetadatosAsync(int id, UpdateDocumentoDto dto)
    {
        var response = await http.PutAsJsonAsync($"{Base}/{id}/metadatos", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DocumentoDto>();
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
        return await response.Content.ReadFromJsonAsync<DocumentoDto>();
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

    public async Task<List<PropuestaModificacionDto>> GetPropuestasPendientesAsync() =>
        await http.GetFromJsonAsync<List<PropuestaModificacionDto>>(
            $"{Base}/propuestas/pendientes") ?? [];

    public async Task<int> GetPropuestasPendientesCountAsync()
    {
        try { return await http.GetFromJsonAsync<int>($"{Base}/propuestas/pendientes/count"); }
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
}
