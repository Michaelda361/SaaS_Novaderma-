using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Inscripciones;

namespace TalentManagement.Client.Services;

public class InscripcionApiService(HttpClient http)
{
    private const string Base = "api/v1/inscripciones";

    public async Task<List<InscripcionDto>> GetByCapacitacionAsync(int capacitacionId) =>
        await http.GetFromJsonAsync<List<InscripcionDto>>($"{Base}/capacitacion/{capacitacionId}") ?? [];

    // Para el historial del admin: incluye inscripciones de capacitaciones eliminadas
    public async Task<List<InscripcionDto>> GetByCapacitacionHistorialAsync(int capacitacionId) =>
        await http.GetFromJsonAsync<List<InscripcionDto>>($"{Base}/capacitacion/{capacitacionId}/historial") ?? [];

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

    public async Task<InscripcionDto?> MarcarRecursoVistoAsync(int id, int recursoId)
    {
        var response = await http.PostAsync($"{Base}/{id}/recursos/{recursoId}/visto", null);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<InscripcionDto>()
            : null;
    }

    /// <summary>
    /// Endpoint batch: devuelve historial completo (inscripciones + resultados) en una sola llamada.
    /// Reemplaza el N+1 masivo de CargarHistorial.
    /// </summary>
    public async Task<List<HistorialInscripcionDto>> GetHistorialCompletoAsync() =>
        await http.GetFromJsonAsync<List<HistorialInscripcionDto>>($"{Base}/historial-completo") ?? [];

    public async Task<byte[]?> ExportarTodoExcelAsync()
    {
        var response = await http.GetAsync($"{Base}/exportar-todo");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        return null;
    }

    public async Task<byte[]?> ExportarColaboradorExcelAsync(int colaboradorId)
    {
        var response = await http.GetAsync($"{Base}/exportar/colaborador/{colaboradorId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        return null;
    }

    private record ErrorResponse(string Message);
}
