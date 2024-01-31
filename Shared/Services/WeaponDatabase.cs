namespace Shared.Services;

public class WeaponDatabase{
    //make singelton
    private static WeaponDatabase? _instance = null!;

    public static WeaponDatabase Instance {
        get {
            _instance ??= new WeaponDatabase();
            return _instance;
        }
    }

}