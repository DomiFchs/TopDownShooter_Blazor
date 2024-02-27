namespace Shared.Entities;

public class SessionData {
    public Player Player1 { get; set; } = null!;
    public Player Player2 { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public bool GameStarted { get; set; }
    public int SpectatorCount { get; set; }
    public GameSettings GameSettings { get; set; } = null!;
}