using System.Collections.Generic;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Rules;

public static class TrainingExperienceRules
{
    public static int Calculate(
        int completedSets,
        int totalSets,
        IReadOnlyList<CombatSetResult>? combatResults)
    {
        var safeTotalSets = System.Math.Max(1, totalSets);
        var experience = 4;

        if (combatResults is { Count: > 0 })
        {
            foreach (var result in combatResults)
            {
                experience += result.Result switch
                {
                    CompletionResult.Completed => 8,
                    CompletionResult.Partial => 5,
                    _ => 2,
                };

                if (result.RewardKind == BankedRewardKind.Chest)
                {
                    experience += 4;
                }

                if (result.IsBoss)
                {
                    experience += result.EnemyDefeated ? 12 : 6;
                }
            }

            return System.Math.Max(4, experience);
        }

        var safeCompletedSets = System.Math.Max(0, completedSets);
        experience += safeCompletedSets * 8;
        if (safeCompletedSets >= safeTotalSets)
        {
            experience += 10;
        }

        return System.Math.Max(4, experience);
    }
}
