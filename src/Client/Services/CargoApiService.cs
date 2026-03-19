using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Cargos;

namespace TalentManagement.Client.Services;

public class CargoApiService(HttpClient http)
{
    private const string Base = "api/v1/cargos";

    public Task<List<CargoDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<CargoDto>>(Base);

    public Task<List<CargoDto>?> GetByAreaAsync(int areaId) =>
        http.GetFromJsonAsync<List<CargoDto>>($"{Base}/area/{areaId}");

    public async Task<CargoDto?> CreateAsync(CreateCargoDto dto)
    {
        var r = await http.PostAsJsonAsync(Base, dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CargoDto>() : null;
    }

    public async Task<CargoDto?> UpdateAsync(int id, CreateCargoDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CargoDto>() : null;
    }

    public Task<bool> DeleteAsync(int id) =>
        http.DeleteAsync($"{Base}/{id}").ContinueWith(t => t.Result.IsSuccessStatusCode);
}
