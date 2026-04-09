using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Client.Services;

public class NotificacionesService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly NavigationManager _nav;
    private readonly IAccessTokenProvider _tokenProvider;

    public event Action<SolicitudDocumentoDto>? OnNuevaSolicitud;
    public event Action<SolicitudDocumentoDto>? OnSolicitudResuelta;

    public bool Conectado => _hub?.State == HubConnectionState.Connected;

    public NotificacionesService(NavigationManager nav, IAccessTokenProvider tokenProvider)
    {
        _nav = nav;
        _tokenProvider = tokenProvider;
    }

    public async Task ConectarAsync()
    {
        if (_hub is not null) return;

        var hubUrl = _nav.BaseUri
            .Replace("5185", "5194")
            .Replace("localhost:5000", "localhost:5194")
            .TrimEnd('/') + "/hubs/notificaciones";

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var result = await _tokenProvider.RequestAccessToken();
                    return result.TryGetToken(out var token) ? token.Value : null;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<SolicitudDocumentoDto>("NuevaSolicitud",
            s => OnNuevaSolicitud?.Invoke(s));

        _hub.On<SolicitudDocumentoDto>("SolicitudResuelta",
            s => OnSolicitudResuelta?.Invoke(s));

        try { await _hub.StartAsync(); }
        catch { /* En dev sin MSAL puede fallar — la app sigue funcionando sin notificaciones */ }
    }

    public async Task UnirseGrupoAdminAsync()
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("JoinAdminGroup");
    }

    public async Task SalirGrupoAdminAsync()
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("LeaveAdminGroup");
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
