using System.Collections.Generic;

namespace DungeonFit.Core.Models;

public sealed class DungeonRun
{
    private readonly List<RunSummary> _stageSummaries = new();
    private readonly List<BankedReward> _bankedRewards = new();

    public DungeonRun(DungeonPlan plan, int initialPlayerHp)
    {
        Plan = plan;
        RunId = System.Guid.NewGuid().ToString("N");
        CurrentPlayerHp = System.Math.Max(0, initialPlayerHp);
    }

    public DungeonPlan Plan { get; }

    public string RunId { get; }

    public int CurrentPlayerHp { get; private set; }

    public int CurrentStageIndex => _stageSummaries.Count;

    public TaskTemplate CurrentStage => HasNextStage
        ? Plan.Stages[CurrentStageIndex]
        : throw new System.InvalidOperationException("Cannot read CurrentStage after the dungeon route is complete.");

    public IReadOnlyList<RunSummary> StageSummaries => _stageSummaries;

    public IReadOnlyList<BankedReward> BankedRewards => _bankedRewards;

    public bool HasStarted => _stageSummaries.Count > 0;

    public bool IsComplete => CurrentStageIndex >= Plan.Stages.Count;

    public bool HasNextStage => !IsComplete;

    public int CompletedStages => _stageSummaries.Count;

    public void RecordStageResult(RunSummary summary, IEnumerable<BankedReward> bankedRewards)
    {
        if (IsComplete)
        {
            return;
        }

        _stageSummaries.Add(summary);
        _bankedRewards.AddRange(bankedRewards);
        CurrentPlayerHp = System.Math.Max(0, summary.RemainingPlayerHp ?? CurrentPlayerHp);
    }

    public void RestoreStageResult(RunSummary summary, IEnumerable<BankedReward> bankedRewards)
    {
        if (IsComplete)
        {
            return;
        }

        _stageSummaries.Add(summary);
        _bankedRewards.AddRange(bankedRewards);
        CurrentPlayerHp = System.Math.Max(0, summary.RemainingPlayerHp ?? CurrentPlayerHp);
    }

    public void RestorePlayerHp(int playerHp)
    {
        CurrentPlayerHp = System.Math.Max(0, playerHp);
    }
}
