using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Recursos;

namespace TalentManagement.Client.Services;

public class RecursoApiService(HttpClient http)
{
    private const string Base = "api/v1/recursos";

    public async Task<List<RecursoDto>> GetByCapacitacionAsync(int capacitacionId) =>
        await http.GetFromJsonAsync<List<RecursoDto>>($"{Base}/capacitacion/{capacitacionId}") ?? [];

    public async Task<RecursoDto?> CreateAsync(CreateRecursoDto dto)
    {
        var r = await http.PostAsJsonAsync(Base, dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<RecursoDto>() : null;
    }

    public async Task<RecursoDto?> UpdateAsync(int id, CreateRecursoDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<RecursoDto>() : null;
    }

    public async Task<bool> DeleteAsync(int id) =>
        (await http.DeleteAsync($"{Base}/{id}")).IsSuccessStatusCode;
}
