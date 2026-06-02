using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Colaboradores;

namespace TalentManagement.Client.Services;

public class ColaboradorCampoApiService(HttpClient http)
{
    private const string Base = "api/v1/colaboradores/campos";

    public Task<List<ColaboradorCampoDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<ColaboradorCampoDto>>(Base);

    public async Task<ColaboradorCampoDto?> CreateAsync(CreateColaboradorCampoDto dto)
    {
        var response = await http.PostAsJsonAsync(Base, dto);
        return response.IsSuccessStatusCode ?
            await response.Content.ReadFromJsonAsync<ColaboradorCampoDto>() : null;
    }

    public async Task<ColaboradorCampoDto?> UpdateAsync(int id, UpdateColaboradorCampoDto dto)
    {
        var response = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return response.IsSuccessStatusCode ?
            await response.Content.ReadFromJsonAsync<ColaboradorCampoDto>() : null;
    }

    public Task<bool> DeleteAsync(int id) =>
        http.DeleteAsync($"{Base}/{id}").ContinueWith(t => t.Result.IsSuccessStatusCode);
}
