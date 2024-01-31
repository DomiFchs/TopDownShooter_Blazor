namespace Shared.Entities;

public class GameSettings {
    public float FieldHeight { get; set; } = 800;
    public float FieldWidth { get; set; } = 800;
    public float PlayerHeight { get; set; } = 30;
    public float PlayerWidth { get; set; } = 30;
    public float MaxPlayerHealth { get; set; } = 100;
    public float PlayerSpeed { get; set; } = 3;
    public float PlayerSprintSpeed { get; set; } = 6;
    public float PlayerSprintStaminaCostPerTick { get; set; } = 0.5f;
    public float MaxPlayerStamina { get; set; } = 100;
    public float P1StartX { get; set; } = 100f;
    public float P1StartY { get; set; } = 100f;
    
    public float P2StartX { get; set; } = 700f;
    public float P2StartY { get; set; } = 700f;
}