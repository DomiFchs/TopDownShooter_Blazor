namespace Shared.Entities;

public class SessionData {
    public Player? Player1 { get; set; }
    public Player? Player2 { get; set; }
    public GameSettings GameSettings { get; set; } = null!;
}