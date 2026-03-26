using Microsoft.JSInterop;

namespace TalentManagement.Client.Services;

/// <summary>
/// Solo usado en Development. Persiste el email del usuario dev seleccionado
/// en localStorage y lo expone para que el HttpClient lo mande como X-Dev-User.
/// </summary>
public class DevAuthService(IJSRuntime js, IConfiguration config)
{
    private const string Key = "dev_user_email";

    public bool IsDevMode => config.GetValue<bool>("DevMode");

    public async Task<string?> GetEmailAsync() =>
        IsDevMode ? await js.InvokeAsync<string?>("localStorage.getItem", Key) : null;

    public async Task SetEmailAsync(string email) =>
        await js.InvokeVoidAsync("localStorage.setItem", Key, email);

    public async Task ClearAsync() =>
        await js.InvokeVoidAsync("localStorage.removeItem", Key);
}
