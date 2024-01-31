namespace Shared.Entities;

public class Projectile {
    public float X { get; set; }
    public float Y { get; set; }
    public KeyMask Direction { get; set; }
    public string OwnerConnectionId { get; set; } = null!;
    public float Size { get; } = 8;
    public float Speed { get; set; } = 15;

    public float Damage { get; set; } = 25f;
}