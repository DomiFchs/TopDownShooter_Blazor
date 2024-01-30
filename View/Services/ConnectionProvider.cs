using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.Entities;

namespace View.Services;

public class ConnectionProvider : IAsyncDisposable {
    
    public Player LocalPlayer { get; set; } = null!;
    public HubConnection HubConnection { get; set; } = null!;
    
    public Action? OnGameJoined { get; set; }
    
    public async Task StartMatchmaking(string userName) {
        
        HubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5536/matchmakinghub")
            .Build();
        
        HubConnection.On("MatchFound", (string groupId) => {
            ConnectToGame(groupId, userName);
            LocalPlayer = new Player() { UserName = userName, GroupId = groupId};
        });

        HubConnection.On("ConnectionSet", (string connectionId) => {
            LocalPlayer.ConnectionId = connectionId;
        });
        
        await HubConnection.StartAsync();
        await HubConnection.SendAsync("TryConnect");
    }
    
    private async void ConnectToGame(string groupId, string userName) {
        var newHubConnection = new HubConnectionBuilder()
            .WithUrl($"http://localhost:5536/gamehub")
            .Build();
        
        newHubConnection.On("GameJoined", () => {
            HubConnection.DisposeAsync();
            HubConnection = newHubConnection;
            OnGameJoined?.Invoke();
        });
        
        
        await newHubConnection.StartAsync();
        await newHubConnection.SendAsync("JoinGame", groupId, userName);
    }

    public async ValueTask DisposeAsync() {
        await HubConnection.DisposeAsync();
    }
}