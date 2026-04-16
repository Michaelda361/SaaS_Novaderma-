using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Client.Services;

public record NotificacionItem(
    string Id,
    string Titulo,
    string Cuerpo,
    string Tipo,
    string? Url,
    DateTime Fecha,
    bool Leida = false);

public class NotificacionesService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly NavigationManager _nav;
    private readonly IAccessTokenProvider _tokenProvider;

    private readonly List<NotificacionItem> _historial = [];
    public IReadOnlyList<NotificacionItem> Historial => _historial;
    public int NoLeidas => _historial.Count(n => !n.Leida);

    public event Action? OnCambio;
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

        _hub.On<SolicitudDocumentoDto>("NuevaSolicitud", s =>
        {
            AgregarNotificacion(new NotificacionItem(
                Id: Guid.NewGuid().ToString(),
                Titulo: "Nueva solicitud de carta",
                Cuerpo: $"{s.ColaboradorNombre} solicitó \"{s.PlantillaNombre}\"",
                Tipo: "solicitud_nueva",
                Url: "cartas/admin",
                Fecha: DateTime.Now));
            OnNuevaSolicitud?.Invoke(s);
        });

        _hub.On<SolicitudDocumentoDto>("SolicitudResuelta", s =>
        {
            var aprobada = s.Estado == "Aprobada";
            AgregarNotificacion(new NotificacionItem(
                Id: Guid.NewGuid().ToString(),
                Titulo: aprobada ? "Carta aprobada ✅" : "Carta rechazada ❌",
                Cuerpo: aprobada
                    ? $"Tu solicitud de \"{s.PlantillaNombre}\" fue aprobada. Ya puedes descargarla."
                    : $"Tu solicitud de \"{s.PlantillaNombre}\" fue rechazada.{(string.IsNullOrWhiteSpace(s.ComentarioAdmin) ? "" : $" Motivo: {s.ComentarioAdmin}")}",
                Tipo: "solicitud_resuelta",
                Url: "cartas",
                Fecha: DateTime.Now));
            OnSolicitudResuelta?.Invoke(s);
        });

        try { await _hub.StartAsync(); }
        catch { /* En dev sin MSAL puede fallar */ }
    }

    public void MarcarTodasLeidas()
    {
        for (int i = 0; i < _historial.Count; i++)
            _historial[i] = _historial[i] with { Leida = true };
        OnCambio?.Invoke();
    }

    public void MarcarLeida(string id)
    {
        var idx = _historial.FindIndex(n => n.Id == id);
        if (idx >= 0) _historial[idx] = _historial[idx] with { Leida = true };
        OnCambio?.Invoke();
    }

    public void LimpiarHistorial()
    {
        _historial.Clear();
        OnCambio?.Invoke();
    }

    private void AgregarNotificacion(NotificacionItem item)
    {
        _historial.Insert(0, item);
        if (_historial.Count > 50) _historial.RemoveAt(_historial.Count - 1);
        OnCambio?.Invoke();
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
