using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;

namespace DungeonFit.Gameplay;

public sealed class DungeonRunService
{
    private readonly LootRoller _lootRoller = new();

    public DungeonRun Start(DungeonPlan plan, int initialPlayerHp)
    {
        return new DungeonRun(plan, initialPlayerHp);
    }

    public SetSummary RecordStageResult(DungeonRun run, RunSummary summary)
    {
        var completedStageNumber = run.CurrentStageIndex + 1;
        var bankedRewards = BuildBankedRewards(run, run.CurrentStage, summary).ToArray();
        run.RecordStageResult(summary, bankedRewards);
        var nextStage = run.HasNextStage ? run.CurrentStage : null;
        return new SetSummary(summary, bankedRewards, completedStageNumber, run.Plan.Stages.Count, nextStage);
    }

    private IEnumerable<BankedReward> BuildBankedRewards(DungeonRun run, TaskTemplate stage, RunSummary summary)
    {
        var guaranteedChestSet = GetGuaranteedChestSet(summary);
        var aliveBonusChestSet = GetAliveBonusChestSet(summary, guaranteedChestSet);

        for (var set = 1; set <= summary.CompletedSets; set++)
        {
            var combatResult = summary.GetCombatSetResult(set);
            var isGuaranteedChest = set == guaranteedChestSet;
            var isAliveBonusChest = set == aliveBonusChestSet;
            var isChest = isGuaranteedChest || isAliveBonusChest;
            var result = GetRewardResult(summary, combatResult, set, isGuaranteedChest);
            var chestTier = isGuaranteedChest ? "Boss" : combatResult?.ChestTier ?? "Normal";
            var rewardKind = isChest
                ? BankedRewardKind.Chest
                : BankedRewardKind.GoldOnly;
            var chest = new DungeonChest(
                $"{stage.Id}_set_{set}",
                chestTier,
                stage.Id,
                stage.DungeonTypeId,
                $"{run.RunId}_{stage.Id}_set_{set}",
                result,
                set);
            var reward = combatResult is null
                ? rewardKind == BankedRewardKind.Chest
                    ? RollChestReward(chest, CalculateLegacyGold(summary, set))
                    : BuildLegacyReward(summary, set, rewardKind)
                : rewardKind == BankedRewardKind.Chest
                    ? RollChestReward(chest, combatResult.Gold)
                    : new RewardBundle(RewardSource.DungeonRoom, combatResult.Gold, null);

            yield return new BankedReward(
                stage.Id,
                stage.ChallengeName,
                set,
                result,
                chest.Tier,
                reward,
                rewardKind);
        }
    }

    private static int GetGuaranteedChestSet(RunSummary summary)
    {
        return IsCompletedRoom(summary)
            ? System.Math.Min(summary.CompletedSets, summary.TotalSets)
            : 0;
    }

    private static int GetAliveBonusChestSet(RunSummary summary, int guaranteedChestSet)
    {
        if (guaranteedChestSet <= 0 ||
            summary.FatigueRewardPercent < 100 ||
            summary.CompletedSets <= 1 ||
            (summary.RemainingPlayerHp ?? 0) <= 0)
        {
            return 0;
        }

        return System.Math.Max(1, guaranteedChestSet - 1);
    }

    private static bool IsCompletedRoom(RunSummary summary)
    {
        return summary.TotalSets > 0 &&
            summary.CompletedSets >= summary.TotalSets &&
            Enumerable.Range(1, summary.TotalSets)
                .All(set => summary.GetSetResult(set) != CompletionResult.Skipped);
    }

    private static CompletionResult GetRewardResult(
        RunSummary summary,
        CombatSetResult? combatResult,
        int set,
        bool isGuaranteedChest)
    {
        if (!isGuaranteedChest)
        {
            return combatResult?.Result ?? summary.GetSetResult(set);
        }

        if (summary.FatigueRewardPercent < 100)
        {
            return CompletionResult.Partial;
        }

        return (summary.RemainingPlayerHp ?? 0) > 0
            ? CompletionResult.Completed
            : CompletionResult.Partial;
    }

    private static RewardBundle BuildLegacyReward(RunSummary summary, int set, BankedRewardKind rewardKind)
    {
        var gold = CalculateLegacyGold(summary, set);
        var equipment = rewardKind == BankedRewardKind.Chest && set == summary.TotalSets
            ? summary.Reward.Equipment
            : null;

        return new RewardBundle(RewardSource.DungeonRoom, gold, equipment);
    }

    private static int CalculateLegacyGold(RunSummary summary, int set)
    {
        var completedSets = System.Math.Max(1, summary.CompletedSets);
        var baseGold = summary.Reward.Gold / completedSets;
        var remainder = summary.Reward.Gold % completedSets;
        return baseGold + (set <= remainder ? 1 : 0);
    }

    private RewardBundle RollChestReward(DungeonChest chest, int gold)
    {
        var reward = _lootRoller.RollDungeonChest(chest);
        return reward with { Gold = gold };
    }
}
