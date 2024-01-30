using System.Numerics;
using Microsoft.AspNetCore.SignalR;
using Shared.Entities;
using TopDownShooter.ApiService.Entities;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TopDownShooter.ApiService.Services;

public class GameHub : Hub{
    
    private const int Width = 800;
    private const int Height = 800;
    private const int PlayerSize = 30;
    private const int Speed = 3;
    private const int SprintSpeed = 6;
    
    private const int P1StartX = 100;
    private const int P1StartY = 100;
    
    private const int P2StartX = 700;
    private const int P2StartY = 700;
    
    private static readonly Dictionary<string, SessionData> PlayGroups = new();
    
    public async Task JoinGame(string groupId, string userName) {
        Console.WriteLine($"Player {userName} trying to join game {groupId}!");
        var player = new Player { UserName = userName, ConnectionId = Context.ConnectionId, GroupId = groupId, Connected = true};
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        
        if (!PlayGroups.TryGetValue(groupId, out var value)) {
            player.X = P1StartX;
            player.Y = P1StartY;
            PlayGroups.Add(groupId, new SessionData(){Player1 = player});
            Console.WriteLine("Game created!");
            
        } else {
            if (value?.Player2 == null) {
                value!.Player2  = player;
                player.X = P2StartX;
                player.Y = P2StartY;
                await Clients.Group(groupId).SendAsync("GameJoined");
                Console.WriteLine("Game joined!");
            }
        }
    }
    
    public async Task RequestGameData() {
        Console.WriteLine("Game data Requested!");
        var groupId = PlayGroups.First(g => g.Value.Player1!.ConnectionId == Context.ConnectionId || g.Value.Player2!.ConnectionId == Context.ConnectionId).Key;
        
        var players = PlayGroups[groupId];
        if (players.Player1!.ConnectionId == Context.ConnectionId) {
            await Clients.Caller.SendAsync("GetLocalPlayer", players.Player1);
            await Clients.Caller.SendAsync("GetEnemy", players.Player2);
        } else {
            await Clients.Caller.SendAsync("GetLocalPlayer", players.Player2);
            await Clients.Caller.SendAsync("GetEnemy", players.Player1);
        }
    }
    
    public async Task SetReady(bool ready, string groupId) {
        Console.WriteLine("Player is ready!");
        var players = PlayGroups[groupId];
        if (players.Player1!.ConnectionId == Context.ConnectionId) {
            players.Player1.Ready = ready;
        } else {
            players.Player2!.Ready = ready;
        }
        
        if (players.Player1.Ready && players.Player2!.Ready) {
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

        await Clients.Group(groupId).SendAsync("UpdatePlayer", players.Player1);
        await Clients.Group(groupId).SendAsync("UpdatePlayer", players.Player2);
        var allProjectiles = new List<Projectile>();
        allProjectiles.AddRange(players.Player1.Projectiles);
        allProjectiles.AddRange(players.Player2!.Projectiles);
        await Clients.Group(groupId).SendAsync("UpdateProjectiles", allProjectiles);
    }
    
    public void UpdateFlag(string groupId, string connectionId, KeyMask keyFlag) {
        var players = PlayGroups[groupId];
        if (players.Player1!.ConnectionId == connectionId) {
            players.Player1.KeyMask = keyFlag;
        } else {
            players.Player2!.KeyMask = keyFlag;
        }
    }
    
    private void MovePlayer(Player? player) {
        if (player == null) return;
        
        var otherPlayer = player == PlayGroups[player.GroupId!].Player1 ? PlayGroups[player.GroupId!].Player2 : PlayGroups[player.GroupId!].Player1;
        
        player.PreviousX = player.X;
        player.PreviousY = player.Y;
        
        var currentSpeed = player.KeyMask.HasFlag(KeyMask.Sprint) ? SprintSpeed : Speed;
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
        if (player.X > Width - PlayerSize) {
            player.X = Width - PlayerSize;
        }
        
        if (player.Y < 0) {
            player.Y = 0;
        }
        
        if (player.Y > Height - PlayerSize) {
            player.Y = Height - PlayerSize;
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
        return (player1.X < player2.X + PlayerSize &&
                player1.X + PlayerSize > player2.X &&
                player1.Y < player2.Y + PlayerSize &&
                player1.Y + PlayerSize > player2.Y);
    }
    
    private bool IsColliding(Player player, Projectile projectile)
    {
        return (player.X < projectile.X + projectile.Size &&
                player.X + PlayerSize > projectile.X &&
                player.Y < projectile.Y + projectile.Size &&
                player.Y + PlayerSize > projectile.Y);
    }
    
    private async Task MoveProjectiles(Player player, string groupId) {
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
            if (projectile.X < 0 || projectile.X > Width || projectile.Y < 0 || projectile.Y > Height) {
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
    
    private async Task CheckProjectileCollisions(string groupId, Player player, List<Projectile> projectiles)
    {
        var projectilesToRemove = new List<Projectile>();

        foreach (var projectile in projectiles) {
            if (!IsColliding(player, projectile)) continue;
            player.IsDead = true;
            await GameOver(groupId);

            projectilesToRemove.Add(projectile);
        }

        foreach (var projectileToRemove in projectilesToRemove)
        {
            projectiles.Remove(projectileToRemove);
        }
    }
    
    public async Task Shoot(string groupId, string connectionId) {
        var players = PlayGroups[groupId];

        Player shootingPlayer;
        if (players.Player1!.ConnectionId == connectionId) {
            shootingPlayer = players.Player1;
        } else {
            shootingPlayer = players.Player2!;
        }

        var additionalXPos = 0f;
        if (shootingPlayer.LastDirection.HasFlag(KeyMask.Left)) {
            additionalXPos = -PlayerSize / 2;
        } else if (shootingPlayer.LastDirection.HasFlag(KeyMask.Right)) {
            additionalXPos = PlayerSize + PlayerSize / 2;
        }
        else {
            additionalXPos = PlayerSize / 2;
        }
        
        var additionalYPos = 0f;
        if (shootingPlayer.LastDirection.HasFlag(KeyMask.Up)) {
            additionalYPos = -PlayerSize / 2;
        } else if (shootingPlayer.LastDirection.HasFlag(KeyMask.Down)) {
            additionalYPos = PlayerSize + PlayerSize / 2;
        }
        else {
            additionalYPos = PlayerSize / 2;
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
    
    public async Task Disconnect(string groupId, string connectionId) {
        var session = PlayGroups[groupId];
        if (session.Player1!.ConnectionId == connectionId) {
            session.Player1.Connected = false;
        } else {
            session.Player2!.Connected = false;
        }
        
        if (!session.Player1.Connected && !session.Player2!.Connected) {
            PlayGroups.Remove(groupId);
        }
    }
    
}