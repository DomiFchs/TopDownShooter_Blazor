using Microsoft.AspNetCore.SignalR;
using Shared.Entities;
using TopDownShooter.ApiService.Entities;

namespace TopDownShooter.ApiService.Services;

public class MatchmakingHub : Hub {
    
    private static List<Player> Players { get; } = [];
    public async Task TryConnect() {
        var player = new Player {ConnectionId = Context.ConnectionId };
        
        var waitingPlayers = Players.Where(p => !p.Connected).ToList();
        
        switch (waitingPlayers.Count % 2) {
            case 1: {
                var waitingPlayer = waitingPlayers[0];
                
                player.GroupId = waitingPlayer.GroupId;
                await Groups.AddToGroupAsync(player.ConnectionId, player.GroupId!);
                await Clients.Group(player.GroupId!).SendAsync("MatchFound", player.GroupId);
                
                await Groups.RemoveFromGroupAsync(player.ConnectionId, player.GroupId!);
                await Groups.RemoveFromGroupAsync(waitingPlayer.ConnectionId, waitingPlayer.GroupId!);

                player.Connected = true;
                waitingPlayer.Connected = true;
                break;
            }
            case 0: {
                var groupId = Guid.NewGuid().ToString()[..32];
                player.GroupId = groupId;
                await Groups.AddToGroupAsync(player.ConnectionId, groupId);
                break;
            }
        }
        Players.Add(player);
    }
    
}