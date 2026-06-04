using System.Globalization;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
using DungeonFit.Gameplay;

const int targetLevel = 20;
const int maxDays = 240;
const int fullHealCost = 140;
const int basicHealCost = 60;
const int smallPotionCost = 35;
const int smallPotionDailyPurchaseLimit = 4;
const int smallPotionCarryLimit = 4;
const int starterSmallPotionCount = 2;
const int lightEnhanceGoldReserve = 240;
const double roomRecoveryPercent = 0.25;
const double smallPotionHealPercent = 0.45;
const int recommendedRouteStages = 4;
const int fatiguedRewardPercent = 25;
const double fatiguedRewardMultiplier = 0.25;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("BALANCE_SIM_LEVEL20 start targetLevel=20 maxDays=240");

foreach (var strategy in new[]
{
    SimulationStrategy.Baseline,
    SimulationStrategy.NormalPlayer,
    SimulationStrategy.NormalPlayerWideRoute,
})
{
    var result = RunScenario(strategy);
    Console.WriteLine(FormatSummary(result));
    foreach (var checkpoint in result.Checkpoints)
    {
        Console.WriteLine(FormatCheckpoint(result.Strategy, checkpoint));
    }
}

Console.WriteLine("BALANCE_SIM_LEVEL20 end");

SimulationResult RunScenario(SimulationStrategy strategy)
{
    var player = new PlayerState();
    var routeRules = new DungeonRouteRules();
    var roomService = new RoomRunService();
    var runService = new DungeonRunService();
    var lootRoller = new LootRoller();
    var enemyCatalog = new EnemyCatalog();
    var dungeonProgress = new Dictionary<string, DungeonProgressEntry>();
    var checkpoints = new List<DaySnapshot>();
    var daySnapshots = new List<DaySnapshot>();
    var targetReachedDay = 0;
    var smallPotionCount = 0;

    for (var day = 1; day <= maxDays && player.Level < targetLevel; day++)
    {
        PrepareDay(player, strategy, ref smallPotionCount);
        EquipBestLoadout(player);
        LightEnhanceEquippedItems(player, strategy);

        var plan = CreateLeveledPlan(BuildRoute(strategy, routeRules), dungeonProgress);
        var run = runService.Start(plan, player.CurrentHp);
        var dayStartLevel = player.Level;
        var dayStartGold = player.Gold;
        var dayStartInventory = player.Inventory.Count;
        var dayStartHp = player.CurrentHp;
        var roomResults = new List<RoomDayResult>();

        while (!run.IsComplete)
        {
            var stage = run.CurrentStage;
            var room = roomService.Start(
                stage,
                player.CombatStats,
                enemyCatalog.GetForDungeon(stage.DungeonTypeId),
                run.CurrentPlayerHp);

            while (!room.IsComplete)
            {
                roomService.BeginActiveSet(room);
                for (var rep = 0; rep < stage.TargetReps; rep++)
                {
                    roomService.ResolveRepHit(room);
                    UsePotionIfNeeded(player, roomService, room, strategy, ref smallPotionCount);
                }

                roomService.ReportSet(room);
            }

            var reward = roomService.ResolveReward(room);
            var summary = new RunSummary(
                room.CombatResults.LastOrDefault(result => result.IsBoss && result.EnemyDefeated) is not null
                    ? "Boss Cleared"
                    : "Room Finished",
                stage.RoomName,
                room.Progress.CompletedSets,
                stage.TotalSets,
                reward,
                room.SetResults.ToArray(),
                room.CombatResults.ToArray(),
                room.CurrentPlayerHp,
                TrainingExperienceRules.Calculate(room.Progress.CompletedSets, stage.TotalSets, room.CombatResults));

            summary = ApplyRouteFatigue(run, summary);
            runService.RecordStageResult(run, summary);
            var levelsGained = player.AddExperience(summary.ExperienceGained);
            GrantLevelUpRewards(player, lootRoller, stage, levelsGained);
            player.SetCurrentHp(summary.RemainingPlayerHp ?? player.CurrentHp);
            ApplyRoomRecovery(player, run, summary);
            UpdateDungeonProgress(dungeonProgress, stage, summary);

            roomResults.Add(new RoomDayResult(
                stage.DungeonTypeId,
                stage.DungeonLevel,
                summary.CompletedSets,
                summary.TotalSets,
                summary.CombatResults?.Count(result => result.EnemyDefeated) ?? 0,
                summary.CombatResults?.Any(result => result.IsBoss && result.EnemyDefeated) == true,
                summary.RemainingPlayerHp ?? 0,
                summary.ExperienceGained,
                summary.ChestCount,
                summary.Reward.Gold));
        }

        var daily = new DailyRunSummary(run);
        foreach (var bankedReward in run.BankedRewards)
        {
            player.Apply(bankedReward.Reward);
        }

        EquipBestLoadout(player);

        var snapshot = new DaySnapshot(
            day,
            player.Level,
            player.Experience,
            player.ExperienceToNextLevel,
            player.CurrentHp,
            player.MaxHp,
            player.Attack,
            player.EquipmentScore,
            player.Gold,
            player.Inventory.Count,
            player.GetEquippedItems().Count,
            dayStartLevel,
            dayStartGold,
            dayStartInventory,
            dayStartHp,
            roomResults.Count,
            roomResults.Count(result => result.CompletedSets >= result.TotalSets),
            roomResults.Sum(result => result.DefeatedEnemies),
            roomResults.Count(result => result.BossDefeated),
            roomResults.Count(result => result.EndHp <= 0),
            roomResults.Sum(result => result.ExperienceGained),
            daily.TotalGold,
            daily.ChestCount,
            daily.BankedRewardCount,
            player.Inventory.Count(item => item.Rarity == "普通"),
            player.Inventory.Count(item => item.Rarity == "稀有"),
            player.Inventory.Count(item => item.Rarity == "史詩"));

        daySnapshots.Add(snapshot);
        if (ShouldRecordCheckpoint(snapshot))
        {
            checkpoints.Add(snapshot);
        }

        if (targetReachedDay == 0 && player.Level >= targetLevel)
        {
            targetReachedDay = day;
            checkpoints.Add(snapshot);
        }
    }

    return new SimulationResult(
        strategy,
        targetReachedDay,
        daySnapshots.LastOrDefault() ?? DaySnapshot.Empty,
        checkpoints
            .GroupBy(checkpoint => checkpoint.Day)
            .Select(group => group.First())
            .ToArray(),
        daySnapshots.Count(snapshot => snapshot.HpZeroRooms > 0),
        daySnapshots.Count(snapshot => snapshot.DailyChests <= 0),
        daySnapshots.Count(snapshot => snapshot.Level == snapshot.StartLevel && snapshot.InventoryCount == snapshot.StartInventoryCount),
        daySnapshots.Count == 0 ? 0 : daySnapshots.Average(snapshot => snapshot.DailyExperience),
        daySnapshots.Count == 0 ? 0 : daySnapshots.Average(snapshot => snapshot.DailyGold));
}

IEnumerable<DungeonRouteSlot> BuildRoute(SimulationStrategy strategy, DungeonRouteRules routeRules)
{
    var types = strategy == SimulationStrategy.NormalPlayerWideRoute
        ? new[] { "chest", "shoulders", "back", "legs", "core", "arms" }
        : new[] { "chest", "shoulders", "chest", "arms" };

    return types.Select(routeRules.CreateDefaultSlot).ToArray();
}

DungeonPlan CreateLeveledPlan(
    IEnumerable<DungeonRouteSlot> route,
    IReadOnlyDictionary<string, DungeonProgressEntry> dungeonProgress)
{
    var catalog = new TaskCatalog();
    var plan = catalog.CreateDungeonPlanFromRoute(route);
    var stages = plan.Stages
        .Select(stage => stage with
        {
            DungeonLevel = dungeonProgress.TryGetValue(stage.DungeonTypeId, out var progress)
                ? progress.Level
                : 1,
        })
        .ToArray();
    return new DungeonPlan(plan.Id, plan.DisplayName, stages);
}

void PrepareDay(PlayerState player, SimulationStrategy strategy, ref int smallPotionCount)
{
    player.ClearDailyBlessing();
    smallPotionCount = Math.Max(
        smallPotionCount,
        Math.Min(smallPotionCarryLimit, starterSmallPotionCount));

    if (strategy == SimulationStrategy.Baseline)
    {
        return;
    }

    player.SetDailyBlessing(DailyBlessing.BladeMoon);

    if (player.CurrentHp >= player.MaxHp)
    {
        return;
    }

    if (player.Gold >= fullHealCost)
    {
        player.SpendGold(fullHealCost);
        player.HealToFull();
        return;
    }

    if (player.Gold >= basicHealCost)
    {
        player.SpendGold(basicHealCost);
        player.HealPercent(0.4);
    }

    player.HealPercent(0.5);

    var boughtPotions = 0;
    while (smallPotionCount < smallPotionCarryLimit &&
        boughtPotions < smallPotionDailyPurchaseLimit &&
        player.Gold >= smallPotionCost)
    {
        player.SpendGold(smallPotionCost);
        smallPotionCount++;
        boughtPotions++;
    }
}

void EquipBestLoadout(PlayerState player)
{
    foreach (var slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Accessory })
    {
        var best = player.Inventory
            .Where(item => item.Slot == slot)
            .OrderByDescending(item => ScoreItem(item, player.Level))
            .ThenByDescending(item => item.GetEffectivePower(player.Level))
            .FirstOrDefault();

        if (best is not null)
        {
            player.Equip(best.Id);
        }
    }
}

void LightEnhanceEquippedItems(PlayerState player, SimulationStrategy strategy)
{
    if (strategy == SimulationStrategy.Baseline)
    {
        return;
    }

    foreach (var item in player.GetEquippedItems()
        .OrderByDescending(item => ScoreItem(item, player.Level))
        .ToArray())
    {
        if (player.Gold < lightEnhanceGoldReserve)
        {
            return;
        }

        if (item.EnhancementLevel < 2)
        {
            var cost = BlacksmithRules.GetEnhancementCost(item.EnhancementLevel);
            player.EnhanceEquipment(item.Id, cost, BlacksmithRules.MaxEnhancementLevel);
        }
    }
}

void UpdateDungeonProgress(
    IDictionary<string, DungeonProgressEntry> dungeonProgress,
    TaskTemplate completedStage,
    RunSummary summary)
{
    if (!dungeonProgress.TryGetValue(completedStage.DungeonTypeId, out var entry))
    {
        entry = new DungeonProgressEntry
        {
            DungeonTypeId = completedStage.DungeonTypeId,
            Level = 1,
            Experience = 0,
            ExperienceToNextLevel = DungeonProgressEntry.GetExperienceToNextLevel(1),
        };
        dungeonProgress[completedStage.DungeonTypeId] = entry;
    }

    entry.CompletedRooms++;
    if (summary.CombatResults?.Any(result => result.IsBoss && result.EnemyDefeated) == true)
    {
        entry.BossClears++;
    }

    DungeonProgressRules.AddExperience(entry, DungeonProgressRules.CalculateExperience(summary));
}

void UsePotionIfNeeded(
    PlayerState player,
    RoomRunService roomService,
    RoomRun room,
    SimulationStrategy strategy,
    ref int smallPotionCount)
{
    if (strategy == SimulationStrategy.Baseline ||
        smallPotionCount <= 0 ||
        room.CurrentPlayerHp > Math.Ceiling(player.MaxHp * 0.35))
    {
        return;
    }

    var healed = roomService.HealPlayer(room, (int)Math.Ceiling(player.MaxHp * smallPotionHealPercent));
    if (healed > 0)
    {
        smallPotionCount--;
    }
}

void ApplyRoomRecovery(PlayerState player, DungeonRun run, RunSummary summary)
{
    if (run.IsComplete ||
        summary.TotalSets <= 0 ||
        summary.CompletedSets < summary.TotalSets ||
        Enumerable.Range(1, summary.TotalSets).Any(set => summary.GetSetResult(set) == CompletionResult.Skipped))
    {
        return;
    }

    player.HealPercent(roomRecoveryPercent);
    run.RestorePlayerHp(player.CurrentHp);
}

RunSummary ApplyRouteFatigue(DungeonRun run, RunSummary summary)
{
    var completedStageNumber = run.CurrentStageIndex + 1;
    if (completedStageNumber <= recommendedRouteStages)
    {
        return summary;
    }

    var scaledCombatResults = summary.CombatResults?
        .Select(result => result with { Gold = ScaleFatiguedReward(result.Gold) })
        .ToArray();
    var scaledGold = scaledCombatResults is { Length: > 0 }
        ? scaledCombatResults.Sum(result => result.Gold)
        : ScaleFatiguedReward(summary.Reward.Gold);

    return summary with
    {
        Reward = summary.Reward with { Gold = scaledGold },
        CombatResults = scaledCombatResults,
        ExperienceGained = ScaleFatiguedReward(summary.ExperienceGained),
        FatigueRewardPercent = fatiguedRewardPercent,
    };
}

int ScaleFatiguedReward(int value)
{
    return value <= 0
        ? 0
        : Math.Max(1, (int)Math.Ceiling(value * fatiguedRewardMultiplier));
}

void GrantLevelUpRewards(
    PlayerState player,
    LootRoller lootRoller,
    TaskTemplate completedStage,
    int levelsGained)
{
    for (var level = 1; level <= levelsGained; level++)
    {
        var chest = new DungeonChest(
            $"{completedStage.Id}_level_up_{player.Level}_{level}",
            "Boss",
            completedStage.Id,
            completedStage.DungeonTypeId,
            $"balance_level_up_{completedStage.Id}_{player.Level}_{level}",
            CompletionResult.Completed,
            player.Level + level);
        player.Apply(lootRoller.RollDungeonChest(chest));
    }
}

int ScoreItem(EquipmentItem item, int playerLevel)
{
    var score = item.GetEffectivePower(playerLevel) * 10;
    foreach (var modifier in item.Modifiers)
    {
        var value = item.GetEffectiveModifierValue(modifier, playerLevel);
        score += modifier.StatType switch
        {
            EquipmentStatType.Attack => value * 14,
            EquipmentStatType.MaxHp => value * 4,
            EquipmentStatType.DungeonGoldBonusPercent => value,
            _ => value,
        };
    }

    return score;
}

bool ShouldRecordCheckpoint(DaySnapshot snapshot)
{
    return snapshot.Day <= 7 ||
        snapshot.Day % 10 == 0 ||
        snapshot.Level is 5 or 10 or 15 or 20 ||
        snapshot.HpZeroRooms > 0 && snapshot.Day <= 30;
}

string FormatSummary(SimulationResult result)
{
    var final = result.FinalDay;
    return string.Format(
        CultureInfo.InvariantCulture,
        "BALANCE_SUMMARY mode={0} daysTo20={1} finalLevel={2} exp={3}/{4} hp={5}/{6} atk={7} equipScore={8} gold={9} inventory={10} equipped={11} avgDailyExp={12:0.0} avgDailyGold={13:0.0} hpZeroDays={14} noChestDays={15} flatDays={16} rarity=common:{17},rare:{18},epic:{19}",
        result.Strategy,
        result.DaysToTarget == 0 ? "not_reached" : result.DaysToTarget.ToString(CultureInfo.InvariantCulture),
        final.Level,
        final.Experience,
        final.ExperienceToNext,
        final.CurrentHp,
        final.MaxHp,
        final.Attack,
        final.EquipmentScore,
        final.Gold,
        final.InventoryCount,
        final.EquippedCount,
        result.AverageDailyExperience,
        result.AverageDailyGold,
        result.HpZeroDays,
        result.NoChestDays,
        result.FlatDays,
        final.CommonItems,
        final.RareItems,
        final.EpicItems);
}

string FormatCheckpoint(SimulationStrategy strategy, DaySnapshot snapshot)
{
    return string.Format(
        CultureInfo.InvariantCulture,
        "BALANCE_DAY mode={0} day={1} level={2} exp={3}/{4} hp={5}/{6} atk={7} equipScore={8} gold={9} inventory={10} equipped={11} rooms={12}/{13} defeated={14} bosses={15} hpZeroRooms={16} dailyExp={17} dailyGold={18} chests={19} rewards={20} rarity=common:{21},rare:{22},epic:{23}",
        strategy,
        snapshot.Day,
        snapshot.Level,
        snapshot.Experience,
        snapshot.ExperienceToNext,
        snapshot.CurrentHp,
        snapshot.MaxHp,
        snapshot.Attack,
        snapshot.EquipmentScore,
        snapshot.Gold,
        snapshot.InventoryCount,
        snapshot.EquippedCount,
        snapshot.CompletedRooms,
        snapshot.Rooms,
        snapshot.DefeatedEnemies,
        snapshot.BossesDefeated,
        snapshot.HpZeroRooms,
        snapshot.DailyExperience,
        snapshot.DailyGold,
        snapshot.DailyChests,
        snapshot.DailyRewards,
        snapshot.CommonItems,
        snapshot.RareItems,
        snapshot.EpicItems);
}

enum SimulationStrategy
{
    Baseline,
    NormalPlayer,
    NormalPlayerWideRoute,
}

sealed record SimulationResult(
    SimulationStrategy Strategy,
    int DaysToTarget,
    DaySnapshot FinalDay,
    IReadOnlyList<DaySnapshot> Checkpoints,
    int HpZeroDays,
    int NoChestDays,
    int FlatDays,
    double AverageDailyExperience,
    double AverageDailyGold);

sealed record RoomDayResult(
    string DungeonTypeId,
    int DungeonLevel,
    int CompletedSets,
    int TotalSets,
    int DefeatedEnemies,
    bool BossDefeated,
    int EndHp,
    int ExperienceGained,
    int ChestCount,
    int Gold);

sealed record DaySnapshot(
    int Day,
    int Level,
    int Experience,
    int ExperienceToNext,
    int CurrentHp,
    int MaxHp,
    int Attack,
    int EquipmentScore,
    int Gold,
    int InventoryCount,
    int EquippedCount,
    int StartLevel,
    int StartGold,
    int StartInventoryCount,
    int StartHp,
    int Rooms,
    int CompletedRooms,
    int DefeatedEnemies,
    int BossesDefeated,
    int HpZeroRooms,
    int DailyExperience,
    int DailyGold,
    int DailyChests,
    int DailyRewards,
    int CommonItems,
    int RareItems,
    int EpicItems)
{
    public static DaySnapshot Empty { get; } = new(
        0, 1, 0, 300, 0, 24, 3, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
