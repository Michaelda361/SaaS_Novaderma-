using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.Certificados;

namespace TalentManagement.Client.Services;

public class CertificadoApiService(HttpClient http)
{
    private const string Base = "api/v1/certificados";

    public Task<List<CertificadoDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<CertificadoDto>>(Base);

    public Task<List<CertificadoDto>?> GetByColaboradorAsync(int colaboradorId) =>
        http.GetFromJsonAsync<List<CertificadoDto>>($"{Base}/colaborador/{colaboradorId}");

    public Task<List<CertificadoDto>?> GetVencidosAsync() =>
        http.GetFromJsonAsync<List<CertificadoDto>>($"{Base}/vencidos");

    public Task<List<CertificadoDto>?> GetProximosAVencerAsync(int dias = 30) =>
        http.GetFromJsonAsync<List<CertificadoDto>>($"{Base}/proximos-a-vencer?dias={dias}");

    public async Task<CertificadoDto?> CreateAsync(CreateCertificadoDto dto)
    {
        var response = await http.PostAsJsonAsync(Base, dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CertificadoDto>()
            : null;
    }

    public async Task<CertificadoDto?> UpdateAsync(int id, CreateCertificadoDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CertificadoDto>() : null;
    }

    public Task<bool> DeleteAsync(int id) =>
        http.DeleteAsync($"{Base}/{id}").ContinueWith(t => t.Result.IsSuccessStatusCode);
}
