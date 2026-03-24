using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;

namespace TalentManagement.Client.Services;

public class OneDrivePickerService(IJSRuntime js, IAccessTokenProvider tokenProvider)
{
    private const string SharePointBase = "https://labnovaderma.sharepoint.com";
    private IJSObjectReference? _module;

    public async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>(
            "import", "./js/onedrivePicker.js");
        return _module;
    }

    public async Task<string?> ObtenerTokenSharePointAsync()
    {
        var result = await tokenProvider.RequestAccessToken(
            new AccessTokenRequestOptions
            {
                Scopes = [$"{SharePointBase}/AllSites.Read",
                          $"{SharePointBase}/MyFiles.Read"]
            });

        if (result.TryGetToken(out var token))
            return token.Value;

        return null;
    }

    public async Task<string?> ObtenerTokenGraphAsync()
    {
        var result = await tokenProvider.RequestAccessToken(
            new AccessTokenRequestOptions
            {
                Scopes = ["https://graph.microsoft.com/Files.Read",
                          "https://graph.microsoft.com/Files.ReadWrite"]
            });

        if (result.TryGetToken(out var token))
            return token.Value;

        return null;
    }
}
