using DungeonFit.Core.Models;

namespace DungeonFit.Core.Rules;

public static class DungeonProgressRules
{
    public static int CalculateExperience(RunSummary summary)
    {
        var completedSets = System.Math.Max(0, summary.CompletedSets);
        var experience = completedSets * 12;

        if (summary.CompletedSets >= summary.TotalSets && summary.TotalSets > 0)
        {
            experience += 18;
        }

        if (summary.CombatResults is not null)
        {
            foreach (var result in summary.CombatResults)
            {
                if (result.IsBoss)
                {
                    experience += result.EnemyDefeated ? 30 : 10;
                }
                else if (result.EnemyDefeated)
                {
                    experience += 4;
                }
            }
        }

        return System.Math.Max(0, experience);
    }

    public static int AddExperience(DungeonProgressEntry entry, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var levelsGained = 0;
        entry.Experience += amount;
        while (entry.Experience >= entry.ExperienceToNextLevel)
        {
            entry.Experience -= entry.ExperienceToNextLevel;
            entry.Level++;
            levelsGained++;
            entry.ExperienceToNextLevel = DungeonProgressEntry.GetExperienceToNextLevel(entry.Level);
        }

        return levelsGained;
    }
}
