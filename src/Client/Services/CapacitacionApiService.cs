using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Capacitaciones;

namespace TalentManagement.Client.Services;

public class CapacitacionApiService(HttpClient http)
{
    private const string Base = "api/v1/capacitaciones";

    public Task<List<CapacitacionDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<CapacitacionDto>>(Base);

    public async Task<CapacitacionDto?> GetByIdAsync(int id)
    {
        var resp = await http.GetAsync($"{Base}/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new System.UnauthorizedAccessException("Acceso restringido");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<CapacitacionDto>();
    }

    public Task<List<CapacitacionDto>?> GetByAreaAsync(int areaId) =>
        http.GetFromJsonAsync<List<CapacitacionDto>>($"{Base}/area/{areaId}");

    public Task<List<CapacitacionDto>?> GetByColaboradorAsync(int colaboradorId) =>
        http.GetFromJsonAsync<List<CapacitacionDto>>($"{Base}/colaborador/{colaboradorId}");

    public async Task<CapacitacionDto?> CreateAsync(CreateCapacitacionDto dto)
    {
        var r = await http.PostAsJsonAsync(Base, dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CapacitacionDto>() : null;
    }

    public async Task<CapacitacionDto?> UpdateAsync(int id, CreateCapacitacionDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CapacitacionDto>() : null;
    }

    public async Task<CapacitacionDto?> ConfigurarCertificadoAsync(int id, ConfigurarCertificadoDto dto)
    {
        var r = await http.PatchAsJsonAsync($"{Base}/{id}/certificado", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CapacitacionDto>() : null;
    }

    public async Task<CapacitacionDto?> PublicarAsync(int id)
    {
        var r = await http.PatchAsync($"{Base}/{id}/publicar", null);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CapacitacionDto>() : null;
    }

    public async Task<CapacitacionDto?> DespublicarAsync(int id)
    {
        var r = await http.PatchAsync($"{Base}/{id}/despublicar", null);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CapacitacionDto>() : null;
    }

    public Task<bool> DeleteAsync(int id) =>
        http.DeleteAsync($"{Base}/{id}").ContinueWith(t => t.Result.IsSuccessStatusCode);
}

