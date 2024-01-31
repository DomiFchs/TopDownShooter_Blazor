using Shared.Entities;

namespace Shared.Interfaces.Weapons;

public interface IWeapon {
    public string Name { get;}
    public string Description { get;}
    public int Damage { get; }
    public int FireRate { get; }
    
    public Projectile ProjectileInstance { get; }
    public int ProjectileCount { get; }
    public int ProjectileSpread { get; }

    public float SizeX { get; set; }
    public float SizeY { get; set; }

    public int CurrentAmmo { get; }
    public int AmmoPerMag { get; set; }
    public float ReloadTime { get; set; }
    
    public void Shoot();
}