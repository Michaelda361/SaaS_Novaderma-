using Microsoft.AspNetCore.Components;
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

/// <summary>
/// Servicio Singleton de notificaciones en tiempo real via SignalR.
/// La conexión se establece una sola vez desde MainLayout con el email del usuario.
/// Las páginas solo se suscriben/desuscriben a eventos — nunca gestionan la conexión.
/// </summary>
public class NotificacionesService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly NavigationManager _nav;
    private int? _colaboradorId;
    private bool _esAdmin;

    private readonly List<NotificacionItem> _historial = [];
    public IReadOnlyList<NotificacionItem> Historial => _historial;
    public int NoLeidas => _historial.Count(n => !n.Leida);

    public event Action? OnCambio;
    public event Action<SolicitudDocumentoDto>? OnNuevaSolicitud;
    public event Action<SolicitudDocumentoDto>? OnSolicitudResuelta;
    public event Action<TalentManagement.Shared.DTOs.Cuestionarios.CuestionarioRespondidoDto>? OnCuestionarioRespondido;
    public event Action<TalentManagement.Shared.DTOs.Cuestionarios.CertificadoEmitidoDto>? OnCertificadoEmitido;
    public event Action<TalentManagement.Shared.DTOs.Inscripciones.InscripcionDto>? OnInscripcionCreada;
    public event Action<TalentManagement.Shared.DTOs.Capacitaciones.CapacitacionPublicadaDto>? OnCapacitacionPublicada;

    public bool Conectado => _hub?.State == HubConnectionState.Connected;

    public NotificacionesService(NavigationManager nav)
    {
        _nav = nav;
    }

    /// <summary>
    /// Llamado UNA SOLA VEZ desde MainLayout después de cargar el perfil.
    /// Si ya hay conexión activa, no hace nada.
    /// </summary>
    public async Task IniciarAsync(string email, int? colaboradorId = null, bool esAdmin = false)
    {
        if (_hub is not null) return;

        if (string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine("[Hub] IniciarAsync: email vacío, no se conecta");
            return;
        }

        _colaboradorId = colaboradorId;
        _esAdmin = esAdmin;

        var baseUrl = _nav.BaseUri
            .Replace("5185", "5194")
            .Replace("localhost:5000", "localhost:5194")
            .TrimEnd('/') + "/hubs/notificaciones";

        Console.WriteLine($"[Hub] Conectando a: {baseUrl}");

        _hub = new HubConnectionBuilder()
            .WithUrl(baseUrl)
            .WithAutomaticReconnect(new SignalRRetryPolicy())
            .Build();

        _hub.Closed += async ex =>
        {
            _hub = null;
            await Task.CompletedTask;
        };

        _hub.Reconnected += async id =>
        {
            if (_colaboradorId.HasValue && _hub is not null)
                await _hub.InvokeAsync("RegistrarColaborador", _colaboradorId.Value);
            if (_esAdmin && _hub is not null)
                await _hub.InvokeAsync("JoinAdminGroup");
        };

        RegistrarHandlers();

        try
        {
            await _hub.StartAsync();

            if (_colaboradorId.HasValue)
                await _hub.InvokeAsync("RegistrarColaborador", _colaboradorId.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Hub] ERROR al conectar: {ex.Message}");
            _hub = null;
        }
    }

    private void RegistrarHandlers()
    {
        _hub!.On<SolicitudDocumentoDto>("NuevaSolicitud", s =>
        {
            Console.WriteLine($"[Hub] NuevaSolicitud recibida: {s.ColaboradorNombre}");
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
            Console.WriteLine($"[Hub] SolicitudResuelta recibida: {s.Estado}");
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

        _hub.On<TalentManagement.Shared.DTOs.Cuestionarios.CuestionarioRespondidoDto>("CuestionarioRespondido", n =>
        {
            Console.WriteLine($"[Hub] CuestionarioRespondido recibido: {n.ColaboradorNombre}");
            var estado = n.Aprobado ? "✅ Aprobó" : "❌ No aprobó";
            AgregarNotificacion(new NotificacionItem(
                Id: Guid.NewGuid().ToString(),
                Titulo: $"Cuestionario respondido — {estado}",
                Cuerpo: $"{n.ColaboradorNombre} completó \"{n.CapacitacionNombre}\" con {n.Puntaje:0.#}% ({n.Correctas}/{n.TotalPreguntas} correctas)",
                Tipo: "cuestionario_respondido",
                Url: $"capacitaciones/{n.CapacitacionId}",
                Fecha: DateTime.Now));
            OnCuestionarioRespondido?.Invoke(n);
        });

        _hub.On<TalentManagement.Shared.DTOs.Cuestionarios.CertificadoEmitidoDto>("CertificadoEmitido", c =>
        {
            Console.WriteLine($"[Hub] CertificadoEmitido recibido: {c.NombreCertificado}");
            AgregarNotificacion(new NotificacionItem(
                Id: Guid.NewGuid().ToString(),
                Titulo: "🏆 Certificado emitido",
                Cuerpo: $"Aprobaste \"{c.CapacitacionNombre}\" con {c.Puntaje:0.#}%. Tu certificado \"{c.NombreCertificado}\" ya está disponible.",
                Tipo: "certificado_emitido",
                Url: "certificados",
                Fecha: DateTime.Now));
            OnCertificadoEmitido?.Invoke(c);
        });

        _hub.On<TalentManagement.Shared.DTOs.Inscripciones.InscripcionDto>("InscripcionCreada", i =>
        {
            Console.WriteLine($"[Hub] InscripcionCreada recibida: {i.CapacitacionNombre} para {i.ColaboradorEmail}");
            AgregarNotificacion(new NotificacionItem(
                Id: Guid.NewGuid().ToString(),
                Titulo: "📚 Nueva inscripción",
                Cuerpo: $"Fuiste inscrito en la capacitación \"{i.CapacitacionNombre}\".",
                Tipo: "inscripcion_creada",
                Url: $"capacitaciones/{i.CapacitacionId}",
                Fecha: DateTime.Now));
            OnInscripcionCreada?.Invoke(i);
        });

        _hub.On<TalentManagement.Shared.DTOs.Capacitaciones.CapacitacionPublicadaDto>("CapacitacionPublicada", c =>
        {
            AgregarNotificacion(new NotificacionItem(
                Id: Guid.NewGuid().ToString(),
                Titulo: "🎓 Capacitación disponible",
                Cuerpo: $"La capacitación \"{c.CapacitacionNombre}\" ya está disponible.",
                Tipo: "capacitacion_publicada",
                Url: $"capacitaciones/{c.CapacitacionId}",
                Fecha: DateTime.Now));
            OnCapacitacionPublicada?.Invoke(c);
        });
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
        {
            await _hub.DisposeAsync();
            _hub = null;
        }
    }
}

/// <summary>
/// Política de reconexión indefinida con backoff progresivo.
/// </summary>
internal class SignalRRetryPolicy : IRetryPolicy
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
    ];

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        var idx = (int)retryContext.PreviousRetryCount;
        return idx < Delays.Length ? Delays[idx] : TimeSpan.FromSeconds(60);
    }
}
