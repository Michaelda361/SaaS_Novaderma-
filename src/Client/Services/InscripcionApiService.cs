using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Inscripciones;

namespace TalentManagement.Client.Services;

public class InscripcionApiService(HttpClient http)
{
    private const string Base = "api/v1/inscripciones";

    public async Task<List<InscripcionDto>> GetByCapacitacionAsync(int capacitacionId) =>
        await http.GetFromJsonAsync<List<InscripcionDto>>($"{Base}/capacitacion/{capacitacionId}") ?? [];

    public async Task<List<InscripcionDto>> GetByColaboradorAsync(int colaboradorId) =>
        await http.GetFromJsonAsync<List<InscripcionDto>>($"{Base}/colaborador/{colaboradorId}") ?? [];

    public async Task<InscripcionDto?> CreateAsync(CreateInscripcionDto dto)
    {
        var response = await http.PostAsJsonAsync(Base, dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<InscripcionDto>()
            : null;
    }

    public async Task<string?> CreateWithErrorAsync(CreateInscripcionDto dto)
    {
        var response = await http.PostAsJsonAsync(Base, dto);
        if (response.IsSuccessStatusCode) return null;
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return err?.Message ?? "Error al inscribir.";
        }
        return "Error al inscribir.";
    }

    public async Task<InscripcionDto?> UpdateAsync(int id, UpdateInscripcionDto dto)
    {
        var response = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<InscripcionDto>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await http.DeleteAsync($"{Base}/{id}");
        return response.IsSuccessStatusCode;
    }

    private record ErrorResponse(string Message);
}
