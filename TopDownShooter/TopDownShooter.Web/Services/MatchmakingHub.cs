using Microsoft.AspNetCore.SignalR;

namespace TopDownShooter.Web.Services;

public class MatchmakingHub : Hub{

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

}