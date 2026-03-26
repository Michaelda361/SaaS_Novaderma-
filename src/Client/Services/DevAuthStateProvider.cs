using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace TalentManagement.Client.Services;

/// <summary>
/// En Development: si hay un email dev en localStorage, devuelve un ClaimsPrincipal
/// autenticado con ese email (sin pasar por MSAL).
/// Si no hay email dev, devuelve anónimo — App.razor redirige a /dev-login.
/// </summary>
public class DevAuthStateProvider(IJSRuntime js) : AuthenticationStateProvider
{
    private const string Key = "dev_user_email";
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? email;
        try
        {
            email = await js.InvokeAsync<string?>("localStorage.getItem", Key);
        }
        catch
        {
            return Anonymous;
        }

        if (string.IsNullOrWhiteSpace(email))
            return Anonymous;

        var claims = new[]
        {
            new Claim("preferred_username", email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email),
        };
        var identity = new ClaimsIdentity(claims, "DevAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
