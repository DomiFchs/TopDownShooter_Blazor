using System.Data.Common;
using Domain.Repositories.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Model.Configurations;
using Model.Entities;
using MySqlConnector;
using Shared.Entities;

namespace TopDownShooter.ApiService.Services;

public class GameHub(IHighScoreRepository highScoreRepository, SessionHandler sessionHandler) : Hub {
    
    private static SemaphoreSlim _semaphore = new(1, 1);

    public async Task JoinGame(string groupId, string userName) {
        await _semaphore.WaitAsync();
        var player = new Player { UserName = userName, ConnectionId = Context.ConnectionId, GroupId = groupId, Connected = true};
        Console.WriteLine($"Player {userName} created! Adding to group {groupId}!");
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        
        var success = sessionHandler.TryAdd(groupId, new SessionData { Player1 = player, GameSettings = new GameSettings(), GroupId = groupId});
        var sessionData = sessionHandler.Get(groupId);
        if (success) {
            sessionData.Player1!.CurrentHealth = sessionData.GameSettings.MaxPlayerHealth;
            sessionData.Player1!.CurrentStamina = sessionData.GameSettings.MaxPlayerStamina;
            
            sessionData.Player1!.X = sessionData.GameSettings.P1StartX;
            sessionData.Player1!.Y = sessionData.GameSettings.P1StartY;
            Console.WriteLine(sessionHandler.Count());
            Console.WriteLine("Game created!");
            
        } else {
            if (sessionData.Player2 == null) {
                sessionData.Player2 = player;
                
                sessionData.Player2.CurrentHealth = sessionData.GameSettings.MaxPlayerHealth;
                sessionData.Player2.CurrentStamina = sessionData.GameSettings.MaxPlayerStamina;
                
                sessionData.Player2.X = sessionData.GameSettings.P2StartX;
                sessionData.Player2.Y = sessionData.GameSettings.P2StartY;
                
                await Clients.Group(groupId).SendAsync("GameJoined", sessionData.GameSettings);
                Console.WriteLine("Game joined!");
            }
        }

        _semaphore.Release();
    }
    
    public async Task RequestGameData(string? groupId) {
        Console.WriteLine("Game data Requested!");

        if (groupId is null) {
            groupId = sessionHandler.First(g => g.Value.Player1!.ConnectionId == Context.ConnectionId || g.Value.Player2!.ConnectionId == Context.ConnectionId).Key;
        }
        
        var sessionData = sessionHandler.Get(groupId);
        if (sessionData.Player1!.ConnectionId == Context.ConnectionId) {
            await Clients.Caller.SendAsync("PlayersInitialized", sessionData.Player1, sessionData.Player2, sessionData);
            return;
        }

        await Clients.Caller.SendAsync("PlayersInitialized", sessionData.Player2, sessionData.Player1, sessionData);
    }
    
    public async Task SpectateGame(string groupId) {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        var sessionData = sessionHandler.Get(groupId);
        sessionData.SpectatorCount++;
        await Clients.Caller.SendAsync("PlayersInitialized", sessionData.Player1, sessionData.Player2);
        await Clients.Caller.SendAsync("SpectatorGameJoined", sessionData.GameSettings, sessionData);
    }
    
    public async Task ChangeReadyState(bool ready, string groupId) {
        var sessionData = sessionHandler.Get(groupId);
        if (sessionData.Player1!.ConnectionId == Context.ConnectionId) {
            sessionData.Player1.Ready = ready;
        } else {
            sessionData.Player2!.Ready = ready;
        }
        
        if (sessionData.Player1.Ready && sessionData.Player2!.Ready) {
            Console.WriteLine("Game starting!");
            sessionData.GameStarted = true;
            await Clients.Group(groupId).SendAsync("StartGame");
            
        }
    }
    
    
    public async Task UpdatePlayers(string groupId)
    {
        var sessionData = sessionHandler.Get(groupId);
        MovePlayer(sessionData.Player1);
        MovePlayer(sessionData.Player2);
        await MoveProjectiles(sessionData.Player1, groupId);
        await MoveProjectiles(sessionData.Player2, groupId);
        
        var allProjectiles = new List<Projectile>();
        allProjectiles.AddRange(sessionData.Player1.Projectiles);
        allProjectiles.AddRange(sessionData.Player2!.Projectiles);
        
        UpdatePlayerStamina(sessionData);
        
        if(sessionData.Player1.ConnectionId == Context.ConnectionId) {
            await Clients.Group(groupId).SendAsync("UpdateGameData", sessionData.Player1, sessionData.Player2, allProjectiles, sessionData);
        }
        else {
            await Clients.Group(groupId).SendAsync("UpdateGameData", sessionData.Player2, sessionData.Player1, allProjectiles, sessionData);
        }
        
    }

    private void UpdatePlayerStamina(SessionData sessionData) {

        foreach (var player in new[]{sessionData.Player1, sessionData.Player2}) {
            
            if (player.IsSprinting) {
                player.CurrentStamina -= sessionData.GameSettings.PlayerSprintStaminaCostPerTick;
                
                if(player.CurrentStamina <= 0) {
                    player.CurrentStamina = 0;
                    player.IsSprinting = false;
                    player.AllowedToStaminaRegen = false;
                }
            }
            else {
                if (player.TicksLeftUntilStaminaRegen == 0) {
                    player.TicksLeftUntilStaminaRegen = sessionData.GameSettings.TicksUntilPlayerStaminaRegen;
                    player.AllowedToStaminaRegen = true;
                }

                if (player.AllowedToStaminaRegen) {
                    player.CurrentStamina += sessionData.GameSettings.PlayerStaminaRegenPerTick;
                }
        
                if (player.CurrentStamina > sessionData.GameSettings.MaxPlayerStamina) {
                    player.CurrentStamina = sessionData.GameSettings.MaxPlayerStamina;
                }
            }

            if (player.CurrentStamina <= 0) {
                player.TicksLeftUntilStaminaRegen--;
            }
                
            if (player.TicksLeftUntilStaminaRegen < 0) {
                player.TicksLeftUntilStaminaRegen = 0;
            }
        }
    }
    
    public void UpdateSprintState(string groupId, bool isSprinting) {
        var sessionData = sessionHandler.Get(groupId);
        if (sessionData.Player1!.ConnectionId == Context.ConnectionId) {
            if(sessionData.Player1.KeyMask == KeyMask.None) return;
            if (sessionData.Player1.CurrentStamina <= 0) {
                isSprinting = false;
            }
            sessionData.Player1.IsSprinting = isSprinting;
        } else {
            if(sessionData.Player2!.KeyMask == KeyMask.None) return;
            if (sessionData.Player2!.CurrentStamina <= 0) {
                isSprinting = false;
            }
            sessionData.Player2!.IsSprinting = isSprinting;
        }
    }
    
    public void UpdatePlayerInput(string groupId, KeyMask keyFlag) {
        var sessionData = sessionHandler.Get(groupId);
        if (sessionData.Player1!.ConnectionId == Context.ConnectionId) {
            sessionData.Player1.KeyMask = keyFlag;
        } else {
            sessionData.Player2!.KeyMask = keyFlag;
        }
    }
    
    private void MovePlayer(Player? player) {
        if (player == null) return;
        
        
        var gameSession = sessionHandler.Get(player.GroupId!);
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
        var gameSettings = sessionHandler.Get(player1.GroupId!).GameSettings;
        
        return (player1.X < player2.X + gameSettings.PlayerWidth &&
                player1.X + gameSettings.PlayerWidth > player2.X &&
                player1.Y < player2.Y + gameSettings.PlayerHeight &&
                player1.Y + gameSettings.PlayerHeight > player2.Y);
    }
    
    private bool IsColliding(Player player, Projectile projectile)
    {
        if (player.ConnectionId == projectile.OwnerConnectionId) return false;
        var gameSettings = sessionHandler.Get(player.GroupId!).GameSettings;
        return (player.X < projectile.X + projectile.Size &&
                player.X + gameSettings.PlayerWidth > projectile.X &&
                player.Y < projectile.Y + projectile.Size &&
                player.Y + gameSettings.PlayerHeight > projectile.Y);
    }
    
    private async Task MoveProjectiles(Player player, string groupId) {
        var gameSettings = sessionHandler.Get(groupId).GameSettings;
        
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
        
        var gameSession = sessionHandler.Get(groupId);
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
        var session = sessionHandler.Get(groupId);
        var gameSettings = session.GameSettings;
        var projectileSize = session.GameSettings.ProjectileSize;
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
        projectileX -= session.GameSettings.ProjectileSize / 2;
        projectileY -= session.GameSettings.ProjectileSize / 2;

        var projectile = new Projectile {
            X = projectileX,
            Y = projectileY,
            Direction = shootingPlayer.LastDirection,
            OwnerConnectionId = shootingPlayer.ConnectionId,
            Size = gameSettings.ProjectileSize
        };

        shootingPlayer.Projectiles.Add(projectile);
    }


    
    public async Task GameOver(string groupId) {
        var session = sessionHandler.Get(groupId);
        
        var winner = session.Player1.IsDead ? session.Player2 : session.Player1;
        
        await highScoreRepository.TryCreateHighScoreAsync(winner.UserName, CancellationToken.None);
        await Clients.Group(groupId).SendAsync("GameOver", winner);
    }
    
    public async Task Disconnect(string groupId, bool asSpectator) {
        var session = sessionHandler.Get(groupId);
        if (asSpectator) {
            session.SpectatorCount--;
            return;
        }
        if (session.Player1!.ConnectionId == Context.ConnectionId) {
            session.Player1.Connected = false;
        } else {
            session.Player2!.Connected = false;
        }
        
        if (!session.Player1.Connected && !session.Player2!.Connected) {
            sessionHandler.Remove(groupId);
        }
    }
    
}