using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TalentManagement.Client.Services;

public class OneDriveItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool EsCarpeta { get; set; }
    public string? WebUrl { get; set; }
    public long? Size { get; set; }
}

public class OneDriveResult
{
    public List<OneDriveItem> Items { get; set; } = [];
    public string? Error { get; set; }
    public bool NecesitaConsentimiento { get; set; }
}

public class OneDriveGraphService(IJSRuntime js)
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private IJSObjectReference? _module;

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/graphAuth.js");
        return _module;
    }

    private async Task<(HttpClient? client, string? error, bool needsConsent)> CrearClienteAsync(bool usarPopup = false)
    {
        try
        {
            var module = await GetModuleAsync();

            TokenResult result;
            if (usarPopup)
                result = await module.InvokeAsync<TokenResult>("obtenerTokenGraphConPopup");
            else
                result = await module.InvokeAsync<TokenResult>("obtenerTokenGraph");

            if (result.Token is null)
            {
                bool needsConsent = result.NeedsConsent
                    || result.Error is null  // sin error = solo necesita popup
                    || result.Error.Contains("popup", StringComparison.OrdinalIgnoreCase)
                    || result.Error.Contains("consent", StringComparison.OrdinalIgnoreCase)
                    || result.Error.Contains("interaction", StringComparison.OrdinalIgnoreCase);
                return (null, result.Error, needsConsent);
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", result.Token);
            return (client, null, false);
        }
        catch (Exception ex)
        {
            return (null, $"Error al obtener token: {ex.Message}", false);
        }
    }

    public async Task<OneDriveResult> ListarCarpetaAsync(string? itemId, bool usarPopup = false)
    {
        var (client, error, needsConsent) = await CrearClienteAsync(usarPopup);

        if (needsConsent)
            return new OneDriveResult { NecesitaConsentimiento = true, Error = error };

        if (client is null)
            return new OneDriveResult { Error = error ?? "Error de autenticación." };

        var url = itemId is null
            ? $"{GraphBase}/me/drive/root/children?$select=id,name,folder,webUrl,size&$orderby=name"
            : $"{GraphBase}/me/drive/items/{itemId}/children?$select=id,name,folder,webUrl,size&$orderby=name";

        try
        {
            var response = await client.GetFromJsonAsync<GraphListResponse>(url);
            var items = response?.Value?.Select(i => new OneDriveItem
            {
                Id = i.Id,
                Name = i.Name,
                EsCarpeta = i.Folder is not null,
                WebUrl = i.WebUrl,
                Size = i.Size
            }).ToList() ?? [];
            return new OneDriveResult { Items = items };
        }
        catch (HttpRequestException ex)
        {
            var msg = ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Sin permisos para acceder a OneDrive."
                : $"Error al conectar con OneDrive: {ex.Message}";
            return new OneDriveResult { Error = msg };
        }
        catch (Exception ex)
        {
            return new OneDriveResult { Error = $"Error inesperado: {ex.Message}" };
        }
    }

    public async Task<string?> ObtenerUrlDescargaAsync(string itemId)
    {
        var (client, _, _) = await CrearClienteAsync();
        if (client is null) return null;
        try
        {
            var item = await client.GetFromJsonAsync<GraphItem>(
                $"{GraphBase}/me/drive/items/{itemId}?$select=id,name,webUrl");
            return item?.WebUrl;
        }
        catch { return null; }
    }

    private record TokenResult(
        [property: JsonPropertyName("token")] string? Token,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("needsConsent")] bool NeedsConsent);

    private class GraphListResponse
    {
        public List<GraphItem>? Value { get; set; }
    }

    private class GraphItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Folder { get; set; }
        public string? WebUrl { get; set; }
        public long? Size { get; set; }
    }
}
