using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Shared.Entities;
using View.Entities;

namespace View.Services;

public class GameConnectionProvider(IOptions<HubConfig> options) : IAsyncDisposable {
    private HubConfig HubConfig { get;} = options.Value;
    public Player? Owner { get; private set; }
    public Player? Enemy { get; private set; }
    public HubConnection? HubConnection { get; set; }
    public GameSettings? GameSettings { get; private set; }
    public SessionData? CurrentSession { get; set; }

    public event Action OnGameJoined = null!;
    public event Func<Player,Player,Task> OnPlayersInitialized = null!;
    public event Func<Player,Player, List<Projectile>, Task> OnGameDataUpdated = null!;
    public event Func<Player,Task> OnGameEnded = null!;
    public event Func<Task> OnGameStarted = null!;

    private string CurrentGroupId { get; set; } = null!;
    
    
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
    
    public async Task DisconnectServerRpc(CancellationToken ct = new()) {
        await HubConnection.SendAsync("Disconnect", CurrentGroupId,false, cancellationToken: ct);
        Disconnect();
    }
    
    public async Task DisconnectAsSpectatorServerRpc(CancellationToken ct = new()) {
        await HubConnection.SendAsync("Disconnect", CurrentGroupId,true, cancellationToken: ct);
        Disconnect();
    }

    private void Disconnect() {
        GameSettings = null!;
        CurrentSession = null!;
        Owner = null!;
        Enemy = null!;
        CurrentGroupId = null!;
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

    public async Task StartSpectatingAsync(string groupId) {
        HubConnection = new HubConnectionBuilder()
            .WithUrl(HubConfig.GameHubHost)
            .Build();
        
        CurrentGroupId = groupId;
        
        AddGameEventListeners();
        AddSpectatorEventListeners();
        await HubConnection.StartAsync();
        await HubConnection.SendAsync("SpectateGame", groupId);
    }

    public async Task<List<SessionData>> GetRunningGames() {
        HubConnection = new HubConnectionBuilder()
            .WithUrl(HubConfig.MatchmakingHubHost)
            .Build();
        
        await HubConnection.StartAsync();
        return await HubConnection.InvokeAsync<List<SessionData>>("GetRunningGames");
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
        
        HubConnection.On<Player,Player, SessionData>("PlayersInitialized", async (localPlayer,enemy, sessionData) => {
            Owner = localPlayer;
            Enemy = enemy;
            CurrentSession = sessionData;
            await OnPlayersInitialized.Invoke(localPlayer,enemy);
        });

        HubConnection.On<Player, Player, List<Projectile>, SessionData>("UpdateGameData",async (localPlayer, enemy, projectiles, sessionData) => {
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
            
            CurrentSession = sessionData;
            await OnGameDataUpdated.Invoke(localPlayer,enemy, projectiles);
        });
        
        HubConnection.On<Player>("GameOver", async winner => {
            await OnGameEnded.Invoke(winner);
        });
    }

    private void AddSpectatorEventListeners() {
        HubConnection.On<GameSettings, SessionData> ("SpectatorGameJoined", (gameSettings, currentSession) => {
            GameSettings = gameSettings;
            CurrentSession = currentSession;
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
        await HubConnection.SendAsync("RequestGameData", null);
    }
    
    public async Task FetchInitialSpectatorDataServerRpc() {
        await HubConnection.SendAsync("RequestGameData", CurrentGroupId);
    }
    
    #endregion

    public async ValueTask DisposeAsync() {
        await HubConnection.StopAsync();
        await HubConnection.DisposeAsync();
    }
}