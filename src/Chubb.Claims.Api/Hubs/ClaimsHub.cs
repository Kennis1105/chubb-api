using Microsoft.AspNetCore.SignalR;

namespace Chubb.Claims.Api.Hubs;

public sealed class ClaimsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("connected", new
        {
            connectionId = Context.ConnectionId,
            serverTimeUtc = DateTime.UtcNow,
            message = "Connected to Chubb APAC Real-Time Claims & Exposure Hub"
        });
        await base.OnConnectedAsync();
    }
}
