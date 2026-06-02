namespace DungeonFit.Core.Models;

public sealed class DungeonProgressEntry
{
    public string DungeonTypeId { get; set; } = string.Empty;

    public int Level { get; set; } = 1;

    public int Experience { get; set; }

    public int ExperienceToNextLevel { get; set; } = GetExperienceToNextLevel(1);

    public int CompletedRooms { get; set; }

    public int BossClears { get; set; }

    public static int GetExperienceToNextLevel(int level)
    {
        var safeLevel = level <= 0 ? 1 : level;
        return 80 + ((safeLevel - 1) * 35);
    }
}
