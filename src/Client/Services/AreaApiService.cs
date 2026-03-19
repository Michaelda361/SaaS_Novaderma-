using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Areas;

namespace TalentManagement.Client.Services;

public class AreaApiService(HttpClient http)
{
    private const string Base = "api/v1/areas";

    public Task<List<AreaDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<AreaDto>>(Base);

    public Task<AreaDto?> GetByIdAsync(int id) =>
        http.GetFromJsonAsync<AreaDto>($"{Base}/{id}");

    public async Task<AreaDto?> CreateAsync(CreateAreaDto dto)
    {
        var r = await http.PostAsJsonAsync(Base, dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<AreaDto>() : null;
    }

    public async Task<AreaDto?> UpdateAsync(int id, CreateAreaDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<AreaDto>() : null;
    }

    public Task<bool> DeleteAsync(int id) =>
        http.DeleteAsync($"{Base}/{id}").ContinueWith(t => t.Result.IsSuccessStatusCode);
}
