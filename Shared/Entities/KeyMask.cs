namespace Shared.Entities;

[Flags]
public enum KeyMask {
    None = 0,
    Up = 1,
    Left = 2,
    Down = 4,
    Right = 8
}