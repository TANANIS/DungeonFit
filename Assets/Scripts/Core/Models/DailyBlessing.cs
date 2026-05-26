namespace DungeonFit.Core.Models;

public static class DailyBlessing
{
    public const string None = "";
    public const string MoonGuard = "moon_guard";
    public const string BladeMoon = "blade_moon";
    public const string StarlightGold = "starlight_gold";

    public static bool IsValid(string? blessingId)
    {
        return blessingId is MoonGuard or BladeMoon or StarlightGold;
    }
}
