using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TalentManagement.Server.Hubs;

[AllowAnonymous]
public class NotificacionesHub(ILogger<NotificacionesHub> logger) : Hub
{
    // ColaboradorId → ConnectionId (una conexión activa por colaborador)
    private static readonly ConcurrentDictionary<int, string> _conexiones = new();
    // ConnectionId → ColaboradorId (para limpiar al desconectar)
    private static readonly ConcurrentDictionary<string, int> _inverso = new();

    public override Task OnConnectedAsync()
    {
        logger.LogInformation("[Hub] OnConnected — ConnectionId={Id}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;
        if (_inverso.TryRemove(connId, out var colaboradorId))
        {
            _conexiones.TryRemove(colaboradorId, out _);
            logger.LogInformation("[Hub] OnDisconnected — ColaboradorId={ColId} ConnectionId={Id}",
                colaboradorId, connId);
        }
        else
        {
            logger.LogInformation("[Hub] OnDisconnected — ConnectionId={Id} (sin colaborador registrado)", connId);
        }
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// El cliente llama este método al conectar para asociar su ColaboradorId con su ConnectionId.
    /// </summary>
    public Task RegistrarColaborador(int colaboradorId)
    {
        var connId = Context.ConnectionId;
        // Si el colaborador tenía una conexión anterior, limpiarla
        if (_conexiones.TryGetValue(colaboradorId, out var connAnterior))
            _inverso.TryRemove(connAnterior, out _);

        _conexiones[colaboradorId] = connId;
        _inverso[connId] = colaboradorId;

        logger.LogInformation("[Hub] RegistrarColaborador — ColaboradorId={ColId} ConnectionId={Id}",
            colaboradorId, connId);
        return Task.CompletedTask;
    }

    public async Task JoinAdminGroup()
    {
        logger.LogInformation("[Hub] JoinAdminGroup — ConnectionId={Id}", Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }

    public async Task LeaveAdminGroup() =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");

    /// <summary>
    /// Obtiene el ConnectionId activo de un colaborador, o null si no está conectado.
    /// </summary>
    public static string? GetConnectionId(int colaboradorId) =>
        _conexiones.TryGetValue(colaboradorId, out var connId) ? connId : null;

    /// <summary>
    /// Para diagnóstico en dev: retorna todas las conexiones activas.
    /// </summary>
    public static object GetConexionesActivas() =>
        _conexiones.Select(kv => new { colaboradorId = kv.Key, connectionId = kv.Value }).ToList();
}
