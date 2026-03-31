using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Client.Services;

public class PlantillaDocumentoApiService(HttpClient http)
{
    private const string Base = "api/v1/plantillasdocumento";

    public Task<List<PlantillaDocumentoDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<PlantillaDocumentoDto>>(Base);

    public async Task<List<PlantillaDocumentoDto>> GetDisponiblesAsync()
    {
        var response = await http.GetAsync($"{Base}/disponibles");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<PlantillaDocumentoDto>>() ?? [];
    }

    public Task<PlantillaDocumentoDto?> GetByIdAsync(int id) =>
        http.GetFromJsonAsync<PlantillaDocumentoDto>($"{Base}/{id}");

    public async Task<PlantillaDocumentoDto?> CreateAsync(CreatePlantillaDocumentoDto dto)
    {
        var r = await http.PostAsJsonAsync(Base, dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<PlantillaDocumentoDto>() : null;
    }

    public async Task<PlantillaDocumentoDto?> UpdateAsync(int id, CreatePlantillaDocumentoDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<PlantillaDocumentoDto>() : null;
    }

    public async Task<bool> DeleteAsync(int id) =>
        (await http.DeleteAsync($"{Base}/{id}")).IsSuccessStatusCode;

    public async Task<PrevisualizarDto?> PrevisualizarAsync(int id, GenerarPdfDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/{id}/previsualizar", dto);
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<PrevisualizarDto>();
    }

    public async Task<(byte[]? bytes, string? fileName)> GenerarPdfAsync(int id, Dictionary<string, string>? extras = null)
    {
        var dto = new GenerarPdfDto { Extras = extras ?? [] };
        var response = await http.PostAsJsonAsync($"{Base}/{id}/generar", dto);
        if (!response.IsSuccessStatusCode) return (null, null);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName
                    ?? "carta.pdf";
        return (bytes, fileName.Trim('"'));
    }

    public async Task<List<SolicitudDocumentoDto>> GetMisSolicitudesAsync()
    {
        var response = await http.GetAsync($"{Base}/mis-solicitudes");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<SolicitudDocumentoDto>>() ?? [];
    }
}
