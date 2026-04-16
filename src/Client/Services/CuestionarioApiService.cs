using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Cuestionarios;

namespace TalentManagement.Client.Services;

public class CuestionarioApiService(HttpClient http)
{
    private const string Base = "api/v1/cuestionarios";

    public Task<CuestionarioDto?> GetByCapacitacionAsync(int capacitacionId) =>
        http.GetFromJsonAsync<CuestionarioDto>($"{Base}/capacitacion/{capacitacionId}");

    public async Task<CuestionarioDto?> CreateAsync(CreateCuestionarioDto dto)
    {
        var r = await http.PostAsJsonAsync(Base, dto);
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<CuestionarioDto>();
    }

    public async Task<CuestionarioDto?> UpdateAsync(int id, CreateCuestionarioDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<CuestionarioDto>();
    }

    public async Task<bool> DeleteAsync(int id) =>
        (await http.DeleteAsync($"{Base}/{id}")).IsSuccessStatusCode;

    public async Task<ResultadoCuestionarioDto?> ResponderAsync(ResponderCuestionarioDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/responder", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ResultadoCuestionarioDto>() : null;
    }

    public Task<ResultadoCuestionarioDto?> GetResultadoAsync(int cuestionarioId, int inscripcionId) =>
        http.GetFromJsonAsync<ResultadoCuestionarioDto>($"{Base}/{cuestionarioId}/resultado/{inscripcionId}");
}
