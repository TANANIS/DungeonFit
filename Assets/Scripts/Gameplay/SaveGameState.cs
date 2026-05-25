using System.Collections.Generic;
using DungeonFit.Core.Models;

namespace DungeonFit.Gameplay;

public sealed class SaveGameState
{
    public const int CurrentVersion = 4;

    public int Version { get; set; } = CurrentVersion;

    public int Level { get; set; } = 1;

    public int Experience { get; set; } = 120;

    public int ExperienceToNextLevel { get; set; } = 300;

    public int Gold { get; set; }

    public List<EquipmentItem>? Inventory { get; set; } = new();

    public EquipmentLoadout? EquipmentLoadout { get; set; } = new();

    public List<DungeonRouteSlot>? SelectedDungeonRoute { get; set; } = new();

    public bool HasActiveRun { get; set; }

    public int? ActiveRunCurrentHp { get; set; }

    public List<SavedStageResult>? ActiveStageResults { get; set; } = new();

    public bool DailyRewardsClaimed { get; set; }

    public RunSummary? LastRunSummary { get; set; }

    public string NoticeBoardRefreshKey { get; set; } = string.Empty;

    public List<ActiveShortTermQuest>? ActiveShortTermQuests { get; set; } = new();

    public ActiveShortTermQuest? ActiveShortTermQuest { get; set; }
}

public sealed class SavedStageResult
{
    public RunSummary? Summary { get; set; }

    public List<BankedReward>? BankedRewards { get; set; } = new();
}
