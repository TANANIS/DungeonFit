using System;

namespace DungeonFit.Gameplay;

public static class BlacksmithRules
{
    public const int MaxEnhancementLevel = 5;

    private static readonly int[] EnhancementCosts = { 50, 100, 150, 200, 250 };

    public static int ClampEnhancementLevel(int level)
    {
        return Math.Clamp(level, 0, MaxEnhancementLevel);
    }

    public static int GetEnhancementCost(int currentLevel)
    {
        var safeLevel = ClampEnhancementLevel(currentLevel);
        return safeLevel >= MaxEnhancementLevel ? 0 : EnhancementCosts[safeLevel];
    }

    public static int GetTotalEnhancementCost(int currentLevel)
    {
        var safeLevel = ClampEnhancementLevel(currentLevel);
        var total = 0;
        for (var index = 0; index < safeLevel; index++)
        {
            total += EnhancementCosts[index];
        }

        return total;
    }

    public static int GetDismantleRefund(int currentLevel)
    {
        return GetTotalEnhancementCost(currentLevel) / 2;
    }
}
