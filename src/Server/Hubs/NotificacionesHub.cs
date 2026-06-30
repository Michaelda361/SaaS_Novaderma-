using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Server.Hubs;

[Authorize]
public class NotificacionesHub(
    IColaboradorRepository colaboradorRepo,
    ILogger<NotificacionesHub> logger) : Hub
{
    private readonly IColaboradorRepository _colaboradorRepo = colaboradorRepo;

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
    public async Task RegistrarColaborador(int colaboradorId)
    {
        var email = Context.User?.FindFirst("preferred_username")?.Value 
                    ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            throw new HubException("No autorizado. Email no encontrado en el token.");
        }

        var colaborador = await _colaboradorRepo.GetByEmailAsync(email);
        if (colaborador is null || colaborador.Id != colaboradorId)
        {
            logger.LogWarning("[Hub] RegistrarColaborador denegado para Email={Email} ColaboradorId={ColId}", email, colaboradorId);
            throw new HubException("No autorizado.");
        }

        var connId = Context.ConnectionId;
        // Si el colaborador tenía una conexión anterior, limpiarla
        if (_conexiones.TryGetValue(colaboradorId, out var connAnterior))
            _inverso.TryRemove(connAnterior, out _);

        _conexiones[colaboradorId] = connId;
        _inverso[connId] = colaboradorId;

        logger.LogInformation("[Hub] RegistrarColaborador — ColaboradorId={ColId} ConnectionId={Id}",
            colaboradorId, connId);
    }

    public async Task JoinAdminGroup()
    {
        var email = Context.User?.FindFirst("preferred_username")?.Value 
                    ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            throw new HubException("No autorizado. Email no encontrado en el token.");
        }

        var colaborador = await _colaboradorRepo.GetByEmailAsync(email);
        if (colaborador is null || (colaborador.Rol != RolUsuario.Admin && colaborador.Rol != RolUsuario.Jefe))
        {
            throw new HubException("No autorizado.");
        }

        logger.LogInformation("[Hub] JoinAdminGroup — ConnectionId={Id} Email={Email}", Context.ConnectionId, email);
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
