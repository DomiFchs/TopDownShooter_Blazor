namespace Shared.Entities;

public class Player {
    public string UserName { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public bool Connected { get; set; }
    public bool Ready { get; set; }
    public string? GroupId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }

    public float PreviousX { get; set; }
    public float PreviousY { get; set; }
    public KeyMask KeyMask { get; set; }
    
    public KeyMask LastDirection { get; set; }
    
    public List<Projectile> Projectiles { get; set; } = new();

    public bool IsDead { get; set; }
}