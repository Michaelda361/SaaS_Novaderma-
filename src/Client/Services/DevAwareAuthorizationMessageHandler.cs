using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace TalentManagement.Client.Services;

/// <summary>
/// En Development: intenta añadir el Bearer token de MSAL, pero si no está disponible
/// deja pasar el request sin token (el servidor usa DevUserStore para autenticar).
/// En producción: redirige al login si no hay token.
/// </summary>
public class DevAwareAuthorizationMessageHandler : AuthorizationMessageHandler
{
    private readonly IConfiguration _config;

    public DevAwareAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration config)
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
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (AccessTokenNotAvailableException)
            {
                // Sin token MSAL — enviar sin Authorization header via InnerHandler
                request.Headers.Authorization = null;
                using var client = new HttpClient(InnerHandler!, disposeHandler: false);
                return await client.SendAsync(request, cancellationToken);
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
}
