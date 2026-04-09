using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Json;

namespace TalentManagement.Client.Services;

/// <summary>
/// En Development: intenta añadir el Bearer token de MSAL, pero si no está disponible
/// envía el request con el header X-Dev-User para que el servidor lo autentique.
/// En producción: redirige al login si no hay token.
/// </summary>
public class DevAwareAuthorizationMessageHandler : AuthorizationMessageHandler
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;

    public DevAwareAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration config,
        IServiceProvider services)
        : base(provider, navigation)
    {
        _config = config;
        _services = services;
        ConfigureHandler(
            authorizedUrls: ["http://localhost:5194/"],
            scopes: ["api://60ea78c7-4add-4d9e-ba23-ac5d2c1ee4ac/access_as_user"]);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_config.GetValue<bool>("DevMode"))
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (AccessTokenNotAvailableException)
            {
                // Sin token MSAL — clonar el request y enviar con X-Dev-User
                var fallback = await CloneRequestAsync(request);
                fallback.Headers.Authorization = null;

                // Leer el usuario activo del servidor y adjuntarlo como header
                var devEmail = await GetActiveDevEmailAsync(cancellationToken);
                if (!string.IsNullOrEmpty(devEmail))
                    fallback.Headers.TryAddWithoutValidation("X-Dev-User", devEmail);

                using var invoker = new HttpMessageInvoker(InnerHandler!, disposeHandler: false);
                return await invoker.SendAsync(fallback, cancellationToken);
            }
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
    }

    private string? _cachedDevEmail;

    private async Task<string?> GetActiveDevEmailAsync(CancellationToken ct)
    {
        // Usar caché para no hacer una petición extra en cada request
        if (_cachedDevEmail is not null) return _cachedDevEmail;

        try
        {
            // Petición directa sin pasar por este handler (evita recursión)
            using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5194/") };
            var result = await http.GetFromJsonAsync<DevUsuarioActivoDto>(
                "api/v1/dev/usuario-activo", ct);
            _cachedDevEmail = result?.Email;
        }
        catch { }

        return _cachedDevEmail;
    }

    // Llamar esto cuando el usuario cambia para limpiar el caché
    public void InvalidateCache() => _cachedDevEmail = null;

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Version = original.Version;
        return clone;
    }

    private record DevUsuarioActivoDto(string? Email, bool Activo);
}
