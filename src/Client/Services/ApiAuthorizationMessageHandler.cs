using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;

namespace TalentManagement.Client.Services;

/// <summary>
/// Handler que captura AccessTokenNotAvailableException y redirige al login
/// en lugar de dejar que la excepción crashee el componente.
/// </summary>
public class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration config,
        IWebAssemblyHostEnvironment env)
        : base(provider, navigation)
    {
        var apiBaseUrl = config["ApiBaseUrl"] ?? env.BaseAddress;
        ConfigureHandler(
            authorizedUrls: [apiBaseUrl],
            scopes: ["api://60ea78c7-4add-4d9e-ba23-ac5d2c1ee4ac/access_as_user"]);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
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
