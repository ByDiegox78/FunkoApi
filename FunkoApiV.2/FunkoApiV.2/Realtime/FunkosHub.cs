using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FunkoApiV._2.Realtime;

[Authorize]
public class FunkosHub(ILogger<FunkosHub> log) : Hub {
    
    public override async Task OnConnectedAsync() {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = Context.User?.IsInRole("ADMIN") ?? false;
        log.LogInformation("Cliente conectado: {ConnectionId}, UserId: {UserId}, IsAdmin: {IsAdmin}",
            Context.ConnectionId, userId, isAdmin);
        if (userId is not null) 
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        if (isAdmin)
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception) {
        if (exception != null)
            log.LogWarning(exception, "Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        else
            log.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    [Authorize(Roles = "ADMIN")]
    public async Task SendMessage(string message) {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
    
    
    
}