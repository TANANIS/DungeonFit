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
        for (var set = 1; set <= summary.CompletedSets; set++)
        {
            var combatResult = summary.GetCombatSetResult(set);
            var result = combatResult?.Result ?? summary.GetSetResult(set);
            var chestTier = combatResult?.ChestTier ?? (set == summary.TotalSets ? "Boss" : "Normal");
            var rewardKind = combatResult?.RewardKind ?? GetLegacyRewardKind(summary, set);
            var chest = new DungeonChest(
                $"{stage.Id}_set_{set}",
                chestTier,
                stage.Id,
                stage.DungeonTypeId,
                $"{run.RunId}_{stage.Id}_set_{set}",
                result,
                set);
            var reward = combatResult is null
                ? BuildLegacyReward(summary, set, rewardKind)
                : rewardKind == BankedRewardKind.Chest
                ? _lootRoller.RollDungeonChest(chest)
                : new RewardBundle(RewardSource.DungeonRoom, combatResult?.Gold ?? 0, null);

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

    private static BankedRewardKind GetLegacyRewardKind(RunSummary summary, int set)
    {
        if (summary.HasCombatResults)
        {
            return BankedRewardKind.GoldOnly;
        }

        return summary.Reward.Equipment is not null && set == summary.TotalSets
            ? BankedRewardKind.Chest
            : BankedRewardKind.GoldOnly;
    }

    private static RewardBundle BuildLegacyReward(RunSummary summary, int set, BankedRewardKind rewardKind)
    {
        var completedSets = System.Math.Max(1, summary.CompletedSets);
        var baseGold = summary.Reward.Gold / completedSets;
        var remainder = summary.Reward.Gold % completedSets;
        var gold = baseGold + (set <= remainder ? 1 : 0);
        var equipment = rewardKind == BankedRewardKind.Chest && set == summary.TotalSets
            ? summary.Reward.Equipment
            : null;

        return new RewardBundle(RewardSource.DungeonRoom, gold, equipment);
    }
}
