using Microsoft.AspNetCore.SignalR;
using Shared.Entities;
using TopDownShooter.ApiService.Entities;

namespace TopDownShooter.ApiService.Services;

public class MatchmakingHub : Hub {
    
    //semaphore
    private static List<Player> Players { get; } = [];
    //remove players later
    public async Task TryConnect() {
        var player = new Player {ConnectionId = Context.ConnectionId };
        
        var waitingPlayers = Players.Where(p => !p.Connected).ToList();
        Console.WriteLine($"A player is trying to connect!");
        Console.WriteLine($"There are {waitingPlayers.Count} waiting players!");
        await Clients.Caller.SendAsync("ConnectionSet", Context.ConnectionId);
        switch (waitingPlayers.Count % 2) {
            case 1: {
                Console.WriteLine("Match found!");
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
                Console.WriteLine("No match found, creating new group!");
                var groupId = Guid.NewGuid().ToString()[..16];
                player.GroupId = groupId;
                await Groups.AddToGroupAsync(player.ConnectionId, groupId);
                break;
            }
        }
        Players.Add(player);
    }
    
}