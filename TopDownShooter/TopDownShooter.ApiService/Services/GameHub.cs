using System.Numerics;
using Microsoft.AspNetCore.SignalR;
using Shared.Entities;
using TopDownShooter.ApiService.Entities;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TopDownShooter.ApiService.Services;

public class GameHub : Hub{
    
    
    private static readonly Dictionary<string, SessionData> PlayGroups = new();
    
    public async Task JoinGame(string groupId, string userName) {
        Console.WriteLine($"Player {userName} trying to join game {groupId}!");
        var player = new Player { UserName = userName, ConnectionId = Context.ConnectionId, GroupId = groupId, Connected = true};
        Console.WriteLine($"Player created! Adding to group {groupId}!");
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        
        if (!PlayGroups.TryGetValue(groupId, out var value)) {
            var sessionData = new SessionData() { Player1 = player, GameSettings = new GameSettings() };
            player.CurrentHealth = sessionData.GameSettings.MaxPlayerHealth;
            player.CurrentStamina = sessionData.GameSettings.MaxPlayerStamina;
            
            player.X = sessionData.GameSettings.P1StartX;
            player.Y = sessionData.GameSettings.P1StartY;
            
            PlayGroups.Add(groupId, sessionData);
            
        } else {
            if (value.Player2 == null) {
                value.Player2  = player;
                
                player.CurrentHealth = value.GameSettings.MaxPlayerHealth;
                player.CurrentStamina = value.GameSettings.MaxPlayerStamina;
                
                player.X = value.GameSettings.P2StartX;
                player.Y = value.GameSettings.P2StartY;
                
                await Clients.Group(groupId).SendAsync("GameJoined", value.GameSettings);
            }
        }
    }
    
    public async Task RequestGameData() {
        Console.WriteLine("Game data Requested!");
        var groupId = PlayGroups.First(g => g.Value.Player1!.ConnectionId == Context.ConnectionId || g.Value.Player2!.ConnectionId == Context.ConnectionId).Key;
        
        var players = PlayGroups[groupId];
        if (players.Player1!.ConnectionId == Context.ConnectionId) {
            await Clients.Caller.SendAsync("PlayersInitialized", players.Player1, players.Player2);
            return;
        }

        await Clients.Caller.SendAsync("PlayersInitialized", players.Player2, players.Player1);
    }
    
    public async Task ChangeReadyState(bool ready, string groupId) {
        Console.WriteLine("Player is ready!");
        Console.WriteLine(groupId);
        var players = PlayGroups[groupId];
        if (players.Player1!.ConnectionId == Context.ConnectionId) {
            players.Player1.Ready = ready;
        } else {
            players.Player2!.Ready = ready;
        }
        
        if (players.Player1.Ready && players.Player2!.Ready) {
            Console.WriteLine("Game starting!");
            await Clients.Group(groupId).SendAsync("StartGame");
            
        }
    }
    
    
    public async Task UpdatePlayers(string groupId)
    {
        var players = PlayGroups[groupId];
        MovePlayer(players.Player1);
        MovePlayer(players.Player2);
        await MoveProjectiles(players.Player1, groupId);
        await MoveProjectiles(players.Player2, groupId);
        
        var allProjectiles = new List<Projectile>();
        allProjectiles.AddRange(players.Player1.Projectiles); //TODO: anders vllt in der GameSession
        allProjectiles.AddRange(players.Player2!.Projectiles);
        
        
        if(players.Player1.ConnectionId == Context.ConnectionId) {
            await Clients.Group(groupId).SendAsync("UpdateGameData", players.Player1, players.Player2, allProjectiles);
        }
        else {
            await Clients.Group(groupId).SendAsync("UpdateGameData", players.Player2, players.Player1, allProjectiles);
        }
        
    }
    
    public void UpdateSprintState(string groupId, bool isSprinting) {
        var players = PlayGroups[groupId];
        if (players.Player1!.ConnectionId == Context.ConnectionId) {
            players.Player1.IsSprinting = isSprinting;
        } else {
            players.Player2!.IsSprinting = isSprinting;
        }
    }
    
    public void UpdatePlayerInput(string groupId, KeyMask keyFlag) {
        var players = PlayGroups[groupId];
        if (players.Player1!.ConnectionId == Context.ConnectionId) {
            players.Player1.KeyMask = keyFlag;
        } else {
            players.Player2!.KeyMask = keyFlag;
        }
    }
    
    private void MovePlayer(Player? player) {
        if (player == null) return;
        
        
        var gameSession = PlayGroups[player.GroupId!];
        var gameSettings = gameSession.GameSettings;
        
        var otherPlayer = player == gameSession.Player1 ? gameSession.Player2 : gameSession.Player1;
        
        player.PreviousX = player.X;
        player.PreviousY = player.Y;
        
        var currentSpeed = player.IsSprinting ? gameSettings.PlayerSprintSpeed : gameSettings.PlayerSpeed;
        if (player.KeyMask.HasFlag(KeyMask.Up)) {
            player.Y -= currentSpeed;
        }
        if (player.KeyMask.HasFlag(KeyMask.Down)) {
            player.Y += currentSpeed;
        }
        if (player.KeyMask.HasFlag(KeyMask.Left)) {
            player.X -= currentSpeed;
        }
        if (player.KeyMask.HasFlag(KeyMask.Right)) {
            player.X += currentSpeed;
        }
        if (player.KeyMask != KeyMask.None) player.LastDirection = player.KeyMask;
        
        CheckPlayerCollisions(player, otherPlayer);

        if (player.X < 0) {
            player.X = 0;
        }
        if (player.X > gameSettings.FieldWidth - gameSettings.PlayerWidth) {
            player.X = gameSettings.FieldWidth - gameSettings.PlayerWidth;
        }
        
        if (player.Y < 0) {
            player.Y = 0;
        }
        
        if (player.Y > gameSettings.FieldHeight - gameSettings.PlayerHeight) {
            player.Y = gameSettings.FieldHeight - gameSettings.PlayerHeight;
        }
    }

    private void CheckPlayerCollisions(Player? p1, Player? p2) {
        if(p1 == null || p2 == null) return;
        if (!IsColliding(p1, p2)) return;
        p1.X = p1.PreviousX;
        p1.Y = p1.PreviousY;

        p2.X = p2.PreviousX;
        p2.Y = p2.PreviousY;
        
    }

    private bool IsColliding(Player player1, Player player2) {
        var gameSettings = PlayGroups[player1.GroupId!].GameSettings;
        
        return (player1.X < player2.X + gameSettings.PlayerWidth &&
                player1.X + gameSettings.PlayerWidth > player2.X &&
                player1.Y < player2.Y + gameSettings.PlayerHeight &&
                player1.Y + gameSettings.PlayerHeight > player2.Y);
    }
    
    private bool IsColliding(Player player, Projectile projectile)
    {
        if (player.ConnectionId == projectile.OwnerConnectionId) return false;
        var gameSettings = PlayGroups[player.GroupId!].GameSettings;
        return (player.X < projectile.X + projectile.Size &&
                player.X + gameSettings.PlayerWidth > projectile.X &&
                player.Y < projectile.Y + projectile.Size &&
                player.Y + gameSettings.PlayerHeight > projectile.Y);
    }
    
    private async Task MoveProjectiles(Player player, string groupId) {
        var gameSettings = PlayGroups[groupId].GameSettings;
        
        foreach (var projectile in player.Projectiles) {
            if (projectile.Direction.HasFlag(KeyMask.Up)) {
                projectile.Y -= projectile.Speed;
            }
            if (projectile.Direction.HasFlag(KeyMask.Down)) {
                projectile.Y += projectile.Speed;
            }
            if (projectile.Direction.HasFlag(KeyMask.Left)) {
                projectile.X -= projectile.Speed;
            }
            if (projectile.Direction.HasFlag(KeyMask.Right)) {
                projectile.X += projectile.Speed;
            }
        }
        
        var projectilesToRemove = new List<Projectile>();
        foreach (var projectile in player.Projectiles) {
            if (projectile.X < 0 || projectile.X > gameSettings.FieldWidth || projectile.Y < 0 || projectile.Y > gameSettings.FieldHeight) {
                projectilesToRemove.Add(projectile);
            }
        }

        foreach (var projectileToRemove in projectilesToRemove) {
            player.Projectiles.Remove(projectileToRemove);
        }
        
        var gameSession = PlayGroups[groupId];
        await CheckProjectileCollisions(groupId,gameSession.Player1, player.Projectiles);
        await CheckProjectileCollisions(groupId, gameSession.Player2, player.Projectiles);
    }
    
    private async Task CheckProjectileCollisions(string groupId, Player? player, List<Projectile> projectiles)
    {
        if (player == null) return;
        
        var projectilesToRemove = new List<Projectile>();

        foreach (var projectile in projectiles) {
            if (!IsColliding(player, projectile)) continue;
            player.GetDamage(projectile.Damage);
            projectilesToRemove.Add(projectile);


            if (player.IsDead) {
                await GameOver(groupId);
            }
        }

        foreach (var projectileToRemove in projectilesToRemove)
        {
            projectiles.Remove(projectileToRemove);
        }
    }
    
    public async Task Shoot(string groupId) {
        var session = PlayGroups[groupId];
        var gameSettings = session.GameSettings;
        var projectileSize = new Projectile().Size; //Todo: anders vllt in der GameSession
        Player shootingPlayer;
        if (session.Player1!.ConnectionId == Context.ConnectionId) {
            shootingPlayer = session.Player1;
        } else {
            shootingPlayer = session.Player2!;
        }

        var additionalXPos = 0f;
        if (shootingPlayer.LastDirection.HasFlag(KeyMask.Left)) {
            additionalXPos = -gameSettings.PlayerWidth / 2;
        } else if (shootingPlayer.LastDirection.HasFlag(KeyMask.Right)) {
            additionalXPos = gameSettings.PlayerWidth + gameSettings.PlayerWidth / 2;
        }
        else {
            additionalXPos = projectileSize / 2;
        }
        
        var additionalYPos = 0f;
        if (shootingPlayer.LastDirection.HasFlag(KeyMask.Up)) {
            additionalYPos = -gameSettings.PlayerHeight / 2;
        } else if (shootingPlayer.LastDirection.HasFlag(KeyMask.Down)) {
            additionalYPos = gameSettings.PlayerHeight + gameSettings.PlayerHeight / 2;
        }
        else {
            additionalYPos = projectileSize/2;
        }

        // Calculate the initial position of the projectile based on player size and direction
        float projectileX = shootingPlayer.X + additionalXPos;
        float projectileY = shootingPlayer.Y + additionalYPos;

        // Adjust the projectile's position based on player size
        projectileX -= new Projectile().Size / 2;
        projectileY -= new Projectile().Size / 2;

        var projectile = new Projectile {
            X = projectileX,
            Y = projectileY,
            Direction = shootingPlayer.LastDirection,
            OwnerConnectionId = shootingPlayer.ConnectionId
        };

        shootingPlayer.Projectiles.Add(projectile);
    }


    
    public async Task GameOver(string groupId) {
        var session = PlayGroups[groupId];
        
        var winner = session.Player1.IsDead ? session.Player2 : session.Player1;
        
        await Clients.Group(groupId).SendAsync("GameOver", winner);
    }
    
    public async Task Disconnect(string groupId) {
        var session = PlayGroups[groupId];
        if (session.Player1!.ConnectionId == Context.ConnectionId) {
            session.Player1.Connected = false;
        } else {
            session.Player2!.Connected = false;
        }
        
        if (!session.Player1.Connected && !session.Player2!.Connected) {
            PlayGroups.Remove(groupId);
        }
    }
    
}