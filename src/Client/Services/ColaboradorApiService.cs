using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Colaboradores;

namespace TalentManagement.Client.Services;

public class ColaboradorApiService(HttpClient http)
{
    private const string Base = "api/v1/colaboradores";

    public Task<List<ColaboradorDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<ColaboradorDto>>(Base);

    public Task<ColaboradorDto?> GetByIdAsync(int id) =>
        http.GetFromJsonAsync<ColaboradorDto>($"{Base}/{id}");

    public async Task<ColaboradorDto?> CreateAsync(CreateColaboradorDto dto)
    {
        var response = await http.PostAsJsonAsync(Base, dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ColaboradorDto>()
            : null;
    }

    public async Task<ColaboradorDto?> UpdateAsync(int id, UpdateColaboradorDto dto)
    {
        var response = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ColaboradorDto>()
            : null;
    }

    public Task<bool> DeleteAsync(int id) =>
        http.DeleteAsync($"{Base}/{id}").ContinueWith(t => t.Result.IsSuccessStatusCode);
}
