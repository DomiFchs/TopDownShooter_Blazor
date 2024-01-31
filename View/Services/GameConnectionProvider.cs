using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Shared.Entities;
using View.Entities;

namespace View.Services;

public class GameConnectionProvider(IOptions<HubConfig> options) : IAsyncDisposable {
    private HubConfig HubConfig { get;} = options.Value;
    public Player? Owner { get; private set; }
    public Player? Enemy { get; private set; }
    private HubConnection HubConnection { get; set; } = null!;
    public GameSettings GameSettings { get; set; } = null!;

    public event Action OnGameJoined = null!;
    public event Func<Player,Player,Task> OnPlayersInitialized = null!;
    public event Func<Player,Player, List<Projectile>, Task> OnGameDataUpdated = null!;
    public event Func<Player,Task> OnGameEnded = null!;
    public event Func<Task> OnGameStarted = null!;

    public string CurrentGroupId { get; set; } = null!;
    
    
    #region RPC Methods

    public async Task UpdateGameDataServerRpc(CancellationToken ct) {
        await HubConnection.SendAsync("UpdatePlayers", CurrentGroupId, ct);
    }
    
    public async Task ChangeReadyStateServerRpc(bool ready, CancellationToken ct) {
        await HubConnection.SendAsync("ChangeReadyState", ready, CurrentGroupId, cancellationToken: ct);
    }
    
    public async Task UpdatePlayerInputServerRpc(KeyMask keyMask, CancellationToken ct) {
        await HubConnection.SendAsync("UpdatePlayerInput", CurrentGroupId, keyMask,cancellationToken: ct);
    }
    
    public async Task SendSprintStateServerRpc(bool isSprinting, CancellationToken ct) {
        await HubConnection.SendAsync("UpdateSprintState", CurrentGroupId, isSprinting, cancellationToken: ct);
    }
    
    public async Task ShootServerRpc(CancellationToken ct) {
        await HubConnection.SendAsync("Shoot", CurrentGroupId, cancellationToken: ct);
    }
    
    public async Task DisconnectServerRpc(CancellationToken ct) {
        await HubConnection.SendAsync("Disconnect", CurrentGroupId, cancellationToken: ct);
    }
    
    
    
    #endregion
    
    #region Connection Methods
    public async Task StartMatchmakingAsync(string localUserName) {
        HubConnection = new HubConnectionBuilder()
            .WithUrl(HubConfig.MatchmakingHubHost)
            .Build();
        
        AddMatchmakingEventListeners(localUserName);
        
        await HubConnection.StartAsync();
        await HubConnection.SendAsync("TryConnect");
    }

    private void AddMatchmakingEventListeners(string userName) {
        HubConnection.On("MatchFound", async (string groupId) => {
            CurrentGroupId = groupId;
            await ConnectToGameAsync(userName);
            Owner = new Player() { UserName = userName, GroupId = groupId};
        });
    }

    private void AddGameEventListeners() {
        HubConnection.On("StartGame", async () =>
        {
            await OnGameStarted.Invoke();
        });
        
        HubConnection.On<Player,Player>("PlayersInitialized", async (localPlayer,enemy) => {
            Owner = localPlayer;
            Enemy = enemy;
            await OnPlayersInitialized.Invoke(localPlayer,enemy);
        });

        HubConnection.On<Player, Player, List<Projectile>>("UpdateGameData",async (localPlayer, enemy, projectiles) => {
            if (Owner == null || Enemy == null) {
                return;
            }
            
            Owner.X = localPlayer.X;
            Owner.Y = localPlayer.Y;
            Owner.CurrentHealth = localPlayer.CurrentHealth;
            Owner.CurrentStamina = localPlayer.CurrentStamina;
            Owner.LastDirection = localPlayer.LastDirection;
            
            Enemy.X = enemy.X;
            Enemy.Y = enemy.Y;
            Enemy.CurrentHealth = enemy.CurrentHealth;
            Enemy.CurrentStamina = enemy.CurrentStamina;
            Enemy.LastDirection = enemy.LastDirection;
            
            await OnGameDataUpdated.Invoke(localPlayer,enemy, projectiles);
        });
        
        HubConnection.On<Player>("GameOver", async winner => {
            await OnGameEnded.Invoke(winner);
        });
    }
    
    private async Task ConnectToGameAsync(string userName) {
        var newHubConnection = new HubConnectionBuilder()
            .WithUrl(HubConfig.GameHubHost)
            .Build();
        
        newHubConnection.On<GameSettings>("GameJoined", (gameSettings) => {
            HubConnection.DisposeAsync();
            HubConnection = newHubConnection;
            GameSettings = gameSettings;
            AddGameEventListeners();
            OnGameJoined.Invoke();
        });
        
        
        await newHubConnection.StartAsync();
        await newHubConnection.SendAsync("JoinGame", CurrentGroupId, userName);
    }
    
    public async Task FetchInitialGameDataServerRpc() {
        await HubConnection.SendAsync("RequestGameData");
    }
    
    #endregion

    public async ValueTask DisposeAsync() {
        await HubConnection.StopAsync();
        await HubConnection.DisposeAsync();
    }
}