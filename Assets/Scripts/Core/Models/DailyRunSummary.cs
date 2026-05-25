using System.Collections.Generic;
using System.Linq;

namespace DungeonFit.Core.Models;

public sealed class DailyRunSummary
{
    public DailyRunSummary(DungeonRun run)
    {
        PlanName = run.Plan.DisplayName;
        CompletedStages = run.CompletedStages;
        TotalStages = run.Plan.Stages.Count;
        CompletedSets = run.StageSummaries.Sum(summary => summary.CompletedSets);
        TotalSets = run.Plan.TotalSets;
        BankedRewardCount = run.BankedRewards.Count;
        ChestCount = run.BankedRewards.Count(reward => reward.IsChest);
        BankedRewardGoldPreview = run.BankedRewards.Sum(reward => reward.GoldPreview);
        TotalGold = BankedRewardGoldPreview;
        EquipmentRewards = run.BankedRewards
            .Select(reward => reward.Reward.Equipment)
            .Where(equipment => equipment is not null)
            .Cast<EquipmentItem>()
            .ToArray();
        BankedRewards = run.BankedRewards.ToArray();
    }

    public string PlanName { get; }

    public int CompletedStages { get; }

    public int TotalStages { get; }

    public int CompletedSets { get; }

    public int TotalSets { get; }

    public int TotalGold { get; }

    public int BankedRewardCount { get; }

    public int ChestCount { get; }

    public int BankedRewardGoldPreview { get; }

    public IReadOnlyList<EquipmentItem> EquipmentRewards { get; }

    public IReadOnlyList<BankedReward> BankedRewards { get; }
}
