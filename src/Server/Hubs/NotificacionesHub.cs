using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TalentManagement.Server.Hubs;

[Authorize]
public class NotificacionesHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var email = ObtenerEmail();
        if (!string.IsNullOrEmpty(email))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{email}");
        await base.OnConnectedAsync();
    }

    public async Task JoinAdminGroup() =>
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

    public async Task LeaveAdminGroup() =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");

    private string? ObtenerEmail()
    {
        var user = Context.User;
        return user?.FindFirst("preferred_username")?.Value
            ?? user?.FindFirst("email")?.Value
            ?? user?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? user?.FindFirst("X-Dev-User")?.Value;
    }
}
