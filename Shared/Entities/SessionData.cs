namespace Shared.Entities;

public class SessionData {
    public Player Player1 { get; set; } = null!;
    public Player Player2 { get; set; } = null!;
    public bool GameStarted { get; set; }
    public bool GameEnded { get; set; }
}