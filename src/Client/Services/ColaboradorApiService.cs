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
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ColaboradorDto>();
        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new InvalidOperationException(err?.Error ?? "Error al crear el colaborador.");
        }
        return null;
    }

    public async Task<ColaboradorDto?> UpdateAsync(int id, UpdateColaboradorDto dto)
    {
        var response = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ColaboradorDto>();
        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new InvalidOperationException(err?.Error ?? "Error al actualizar el colaborador.");
        }
        return null;
    }

    private record ErrorResponse(string? Error);

    public Task<bool> DeleteAsync(int id) =>
        http.DeleteAsync($"{Base}/{id}").ContinueWith(t => t.Result.IsSuccessStatusCode);

    public Task<bool> RestaurarAsync(int id) =>
        http.PatchAsync($"{Base}/{id}/restaurar", null).ContinueWith(t => t.Result.IsSuccessStatusCode);

    public Task<List<ColaboradorDto>?> GetInactivosAsync() =>
        http.GetFromJsonAsync<List<ColaboradorDto>>($"{Base}/inactivos");

    public async Task<ColaboradorDto?> CambiarRolAsync(int id, string rol)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}/rol", new CambiarRolDto { Rol = rol });
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ColaboradorDto>() : null;
    }
}
