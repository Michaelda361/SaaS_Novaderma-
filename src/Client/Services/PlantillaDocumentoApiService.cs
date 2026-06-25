using System.Net.Http.Json;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Client.Services;

public class PlantillaDocumentoApiService(HttpClient http)
{
    private const string Base = "api/v1/plantillasdocumento";

    public Task<List<PlantillaDocumentoDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<PlantillaDocumentoDto>>(Base);

    public async Task<List<PlantillaDocumentoDto>> GetDisponiblesAsync()
    {
        var response = await http.GetAsync($"{Base}/disponibles");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<PlantillaDocumentoDto>>() ?? [];
    }

    public Task<PlantillaDocumentoDto?> GetByIdAsync(int id) =>
        http.GetFromJsonAsync<PlantillaDocumentoDto>($"{Base}/{id}");

    public async Task<PlantillaDocumentoDto?> CreateAsync(CreatePlantillaDocumentoDto dto)
    {
        var r = await http.PostAsJsonAsync(Base, dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<PlantillaDocumentoDto>() : null;
    }

    public async Task<PlantillaDocumentoDto?> UpdateAsync(int id, CreatePlantillaDocumentoDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<PlantillaDocumentoDto>() : null;
    }

    public async Task<bool> DeleteAsync(int id) =>
        (await http.DeleteAsync($"{Base}/{id}")).IsSuccessStatusCode;

    public async Task<PrevisualizarDto?> PrevisualizarAsync(int id, GenerarPdfDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/{id}/previsualizar", dto);
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<PrevisualizarDto>();
    }

    public async Task<EditorDocxDto?> GetCamposEditablesAsync(int id)
    {
        var r = await http.GetAsync($"{Base}/{id}/campos-editables");
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<EditorDocxDto>();
    }

    public async Task<(byte[]? bytes, string? fileName)> GenerarConEdicionAsync(int id, GenerarConEdicionDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/{id}/generar-con-edicion", dto);
        if (!r.IsSuccessStatusCode) return (null, null);
        var bytes = await r.Content.ReadAsByteArrayAsync();
        var fileName = r.Content.Headers.ContentDisposition?.FileNameStar
                    ?? r.Content.Headers.ContentDisposition?.FileName
                    ?? "carta.pdf";
        return (bytes, fileName.Trim('"'));
    }

    public async Task<PrevisualizarDto?> ObtenerHtmlEditableAsync(int id)
    {
        var r = await http.PostAsJsonAsync($"{Base}/{id}/editar", new GenerarPdfDto());
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<PrevisualizarDto>();
    }

    public async Task<(byte[]? bytes, string? fileName)> GenerarDesdeHtmlEditadoAsync(int id, GenerarDesdeHtmlDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/{id}/generar-desde-html", dto);
        if (!r.IsSuccessStatusCode) return (null, null);
        var bytes = await r.Content.ReadAsByteArrayAsync();
        var fileName = r.Content.Headers.ContentDisposition?.FileNameStar
                    ?? r.Content.Headers.ContentDisposition?.FileName
                    ?? "carta.pdf";
        return (bytes, fileName.Trim('"'));
    }

    /// <summary>Devuelve el PDF del .docx con variables aplicadas en base64, sin registrar solicitud.</summary>
    public async Task<string?> PrevisualizarDocxBase64Async(int id, Dictionary<string, string>? extras = null)
    {
        var dto = new GenerarPdfDto { Extras = extras ?? [] };
        var r = await http.PostAsJsonAsync($"{Base}/{id}/previsualizar-docx", dto);
        if (!r.IsSuccessStatusCode) return null;
        var bytes = await r.Content.ReadAsByteArrayAsync();
        return Convert.ToBase64String(bytes);
    }

    public async Task<(byte[]? bytes, string? fileName, string? contentType)> GenerarDocumentoAsync(int id, Dictionary<string, string>? extras = null)
    {
        var dto = new GenerarPdfDto { Extras = extras ?? [] };
        var response = await http.PostAsJsonAsync($"{Base}/{id}/generar", dto);
        if (!response.IsSuccessStatusCode) return (null, null, null);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName
                    ?? (contentType.Contains("wordprocessingml") ? "carta.docx" : "carta.pdf");
        return (bytes, fileName.Trim('"'), contentType);
    }

    public async Task<SolicitudDocumentoDto?> EnviarSolicitudAsync(int id, EnviarSolicitudDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/{id}/solicitar", dto);
        if (r.StatusCode == System.Net.HttpStatusCode.Conflict)
            throw new InvalidOperationException(await r.Content.ReadAsStringAsync());
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<SolicitudDocumentoDto>();
    }

    public async Task MarcarSolicitudesVistasAsync()
    {
        await http.PostAsync($"{Base}/mis-solicitudes/marcar-vistas", null);
    }

    public async Task<SolicitudDocumentoDto?> AprobarAsync(int solicitudId, ResolverSolicitudDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/solicitudes/{solicitudId}/aprobar", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<SolicitudDocumentoDto>() : null;
    }

    public async Task<SolicitudDocumentoDto?> RechazarAsync(int solicitudId, ResolverSolicitudDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/solicitudes/{solicitudId}/rechazar", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<SolicitudDocumentoDto>() : null;
    }

    public async Task<PrevisualizarResponseDto?> PrevisualizarSolicitudAsync(int solicitudId, ResolverSolicitudDto dto)
    {
        var r = await http.PostAsJsonAsync($"{Base}/solicitudes/{solicitudId}/previsualizar", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<PrevisualizarResponseDto>() : null;
    }

    public async Task<(byte[]? bytes, string? fileName)> DescargarAprobadaAsync(int solicitudId)
    {
        var r = await http.GetAsync($"{Base}/solicitudes/{solicitudId}/descargar");
        if (!r.IsSuccessStatusCode) return (null, null);
        var bytes = await r.Content.ReadAsByteArrayAsync();
        var fileName = r.Content.Headers.ContentDisposition?.FileNameStar
                    ?? r.Content.Headers.ContentDisposition?.FileName
                    ?? $"carta_{solicitudId}.pdf";
        return (bytes, fileName.Trim('"'));
    }

    public string GetPdfSolicitudUrl(int solicitudId) =>
        $"api/v1/plantillasdocumento/solicitudes/{solicitudId}/pdf";

    public async Task<string?> GetPdfSolicitudBase64Async(int solicitudId)
    {
        var r = await http.GetAsync($"api/v1/plantillasdocumento/solicitudes/{solicitudId}/pdf");
        if (!r.IsSuccessStatusCode) return null;
        var bytes = await r.Content.ReadAsByteArrayAsync();
        return Convert.ToBase64String(bytes);
    }

    public async Task<List<SolicitudDocumentoDto>> GetMisSolicitudesAsync()
    {
        var r = await http.GetAsync($"{Base}/mis-solicitudes");
        if (!r.IsSuccessStatusCode) return [];
        return await r.Content.ReadFromJsonAsync<List<SolicitudDocumentoDto>>() ?? [];
    }

    public async Task<List<SolicitudDocumentoDto>> GetTodasSolicitudesAsync()
    {
        var r = await http.GetAsync($"{Base}/solicitudes");
        if (!r.IsSuccessStatusCode) return [];
        return await r.Content.ReadFromJsonAsync<List<SolicitudDocumentoDto>>() ?? [];
    }

    public async Task<List<SolicitudDocumentoDto>> GetPendientesAsync()
    {
        var r = await http.GetAsync($"{Base}/solicitudes/pendientes");
        if (!r.IsSuccessStatusCode) return [];
        return await r.Content.ReadFromJsonAsync<List<SolicitudDocumentoDto>>() ?? [];
    }

    public async Task<int> CountPendientesAsync()
    {
        var r = await http.GetAsync($"{Base}/solicitudes/count-pendientes");
        if (!r.IsSuccessStatusCode) return 0;
        var obj = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return obj.GetProperty("count").GetInt32();
    }

    /// <summary>Admin genera PDF directamente para un colaborador sin flujo de solicitud.</summary>
    public async Task<(byte[]? bytes, string fileName)> GenerarParaColaboradorAsync(
        int plantillaId, int colaboradorId)
    {
        var r = await http.PostAsync($"{Base}/{plantillaId}/generar-para/{colaboradorId}", null);
        if (!r.IsSuccessStatusCode) return (null, string.Empty);
        var bytes = await r.Content.ReadAsByteArrayAsync();
        var fileName = r.Content.Headers.ContentDisposition?.FileNameStar
            ?? r.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"carta_{plantillaId}_{colaboradorId}.pdf";
        return (bytes, fileName);
    }

    public Task<Dictionary<string, string>?> GetValoresPerfilAsync() =>
        http.GetFromJsonAsync<Dictionary<string, string>>($"{Base}/valores-perfil");
}
