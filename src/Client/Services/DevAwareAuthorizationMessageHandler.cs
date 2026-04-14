using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace TalentManagement.Client.Services;

/// <summary>
/// En Development: envía X-Dev-User directamente desde la config del cliente.
/// No depende de MSAL ni de peticiones al servidor para autenticarse.
/// En producción: usa el Bearer token de MSAL normalmente.
/// </summary>
public class DevAwareAuthorizationMessageHandler : AuthorizationMessageHandler
{
    private readonly IConfiguration _config;

    public DevAwareAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration config,
        IServiceProvider services)
        : base(provider, navigation)
    {
        _config = config;
        ConfigureHandler(
            authorizedUrls: ["http://localhost:5194/"],
            scopes: ["api://60ea78c7-4add-4d9e-ba23-ac5d2c1ee4ac/access_as_user"]);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_config.GetValue<bool>("DevMode"))
        {
            // Dev: añadir X-Dev-User directamente — sin MSAL, sin petición extra
            var devEmail = _config["DevUser"] ?? "dev.jefe@test.local";
            request.Headers.Remove("X-Dev-User");
            request.Headers.TryAddWithoutValidation("X-Dev-User", devEmail);

            using var invoker = new HttpMessageInvoker(InnerHandler!, disposeHandler: false);
            return await invoker.SendAsync(request, cancellationToken);
        }

        // Producción: flujo MSAL normal
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

    public void InvalidateCache() { /* sin caché */ }
}
