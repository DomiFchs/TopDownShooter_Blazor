namespace Shared.Entities;

public class Player {
    public string UserName { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public float CurrentHealth { get; set; }
    public float CurrentStamina { get; set; }
    public float TicksLeftUntilStaminaRegen { get; set; } = 200;
    public bool AllowedToStaminaRegen { get; set; }
    public bool Connected { get; set; }
    public bool Ready { get; set; }
    public bool IsSprinting { get; set; }
    public string? GroupId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float PreviousX { get; set; }
    public float PreviousY { get; set; }
    public KeyMask KeyMask { get; set; }
    public KeyMask LastDirection { get; set; }
    public List<Projectile> Projectiles { get; set; } = new();
    public bool IsDead { get; set; }
    
    
    public void GetDamage(float damage) {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0) {
            IsDead = true;
        }
    }
}