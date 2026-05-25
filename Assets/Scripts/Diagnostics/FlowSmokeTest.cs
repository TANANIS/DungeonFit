using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Gameplay;

namespace DungeonFit.Diagnostics;

public static class FlowSmokeTest
{
    public static IReadOnlyList<string> RunDefaultPlanProgression()
    {
        var session = new GameSession(persistenceEnabled: false);
        var catalog = new TaskCatalog();
        var service = new DungeonRunService();
        var plan = catalog.GetDefaultPlan();
        session.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("shoulders", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("chest", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("arms", 4, 12, "Training Loop", 90),
        });
        var run = service.Start(plan, session.Player.MaxHp);
        var lines = new List<string>
        {
            FormatStage("START", run)
        };

        CompleteCurrentStage(service, run);
        lines.Add(FormatStage("AFTER_STAGE_1", run));

        CompleteCurrentStage(service, run);
        lines.Add(FormatStage("AFTER_STAGE_2", run));
        lines.Add($"PROGRESS completedStages={run.CompletedStages} bankedRewards={run.BankedRewards.Count}");
        session.StartOrGetActiveRun();
        session.RecordStageResult(new RunSummary(
            "Smoke Cleared",
            plan.Stages[0].RoomName,
            plan.Stages[0].TotalSets,
            plan.Stages[0].TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 50, null),
            CompletedResults(plan.Stages[0].TotalSets),
            ClearedCombatResults(plan.Stages[0].TotalSets),
            24));
        lines.Add($"SESSION_BEFORE_COMPLETE gold={session.Player.Gold}");
        session.ClaimDailyRewards();
        lines.Add($"SESSION_AFTER_CLAIM gold={session.Player.Gold}");
        var claimedEquipment = session.Player.Inventory.FirstOrDefault();
        lines.Add($"SESSION_EQUIPMENT inventory={session.Player.Inventory.Count} first={claimedEquipment?.DisplayName ?? "none"}");
        if (claimedEquipment is not null)
        {
            lines.Add($"SESSION_EQUIPMENT_DETAIL slot={claimedEquipment.Slot} rarity={claimedEquipment.Rarity} source={claimedEquipment.SourceDungeonTypeId} modifiers={claimedEquipment.Modifiers.Count}");
            lines.Add($"SESSION_EQUIP result={session.EquipItem(claimedEquipment.Id)} score={session.Player.EquipmentScore}");
            var tavern = session.BuildTavernEquipmentViewModel();
            lines.Add($"TAVERN_SUMMARY level={tavern.Character.Level} hp={tavern.Character.CurrentHp}/{tavern.Character.MaxHp} score={tavern.Character.EquipmentScore} inventory={tavern.InventoryCount}");
            lines.Add($"TAVERN_EQUIPPED weapon={tavern.EquippedSlots[0].Item?.DisplayName ?? "none"} armor={tavern.EquippedSlots[1].Item?.DisplayName ?? "none"} accessory={tavern.EquippedSlots[2].Item?.DisplayName ?? "none"}");
            var weaponFilterTavern = session.BuildTavernEquipmentViewModel(EquipmentInventoryFilter.Weapon);
            lines.Add($"TAVERN_FILTER_LOOKUP filtered={weaponFilterTavern.InventoryItems.Count} all={weaponFilterTavern.AllInventoryItems.Count} selectedAvailable={weaponFilterTavern.AllInventoryItems.Any(item => item.Id == claimedEquipment.Id)}");
            lines.Add($"TAVERN_SELL_EQUIPPED result={session.SellEquipment(claimedEquipment.Id)}");
            lines.Add($"TAVERN_UNEQUIP result={session.UnequipItem(claimedEquipment.Slot)}");
            lines.Add($"TAVERN_LOCK result={session.SetEquipmentLocked(claimedEquipment.Id, true)} sellLocked={session.SellEquipment(claimedEquipment.Id)}");
            lines.Add($"TAVERN_UNLOCK result={session.SetEquipmentLocked(claimedEquipment.Id, false)} sellUnlocked={session.SellEquipment(claimedEquipment.Id)} inventory={session.Player.Inventory.Count}");
        }

        session.CompleteDailyRun();
        lines.Add($"SESSION_AFTER_COMPLETE gold={session.Player.Gold}");
        lines.AddRange(RunSaveMigrationSmoke());

        var earlyExitSession = new GameSession(persistenceEnabled: false);
        earlyExitSession.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("back", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        earlyExitSession.StartOrGetActiveRun();
        var earlyStage = earlyExitSession.ActiveRun!.CurrentStage;
        earlyExitSession.RecordStageResult(new RunSummary(
            "Smoke Cleared",
            earlyStage.RoomName,
            earlyStage.TotalSets,
            earlyStage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 80, null),
            CompletedResults(earlyStage.TotalSets)));
        var earlyDailySummary = earlyExitSession.BuildDailySummary();
        lines.Add($"EARLY_END_BEFORE_CLAIM gold={earlyExitSession.Player.Gold} bankedGold={earlyDailySummary?.TotalGold}");
        earlyExitSession.ClaimDailyRewards();
        lines.Add($"EARLY_END_AFTER_CLAIM gold={earlyExitSession.Player.Gold}");
        lines.AddRange(RunLootProfileSmoke());
        lines.AddRange(RunCombatSmoke());

        return lines;
    }

    private static string FormatStage(string label, DungeonRun run)
    {
        var stage = run.CurrentStage;
        return $"{label} index={run.CurrentStageIndex} room={stage.RoomName} challenge={stage.ChallengeName} action={stage.ActionName}";
    }

    private static void CompleteCurrentStage(DungeonRunService service, DungeonRun run)
    {
        var stage = run.CurrentStage;
        var reward = new RewardBundle(RewardSource.DungeonRoom, 50, null);
        var summary = new RunSummary("Smoke Cleared", stage.RoomName, stage.TotalSets, stage.TotalSets, reward, CompletedResults(stage.TotalSets), ClearedCombatResults(stage.TotalSets), 24);
        service.RecordStageResult(run, summary);
    }

    private static IReadOnlyList<CompletionResult> CompletedResults(int totalSets)
    {
        return Enumerable.Repeat(CompletionResult.Completed, totalSets).ToArray();
    }

    private static IReadOnlyList<CombatSetResult> ClearedCombatResults(int totalSets)
    {
        return Enumerable.Range(1, totalSets)
            .Select(set => new CombatSetResult(
                set,
                set == totalSets,
                CompletionResult.Completed,
                BankedRewardKind.Chest,
                set == totalSets ? "Boss" : "Normal",
                set == totalSets ? 20 : 10,
                24,
                24,
                set == totalSets ? 18 : 8,
                0,
                set == totalSets ? 4 : 2,
                32,
                0,
                true,
                false))
            .ToArray();
    }

    private static IEnumerable<string> RunLootProfileSmoke()
    {
        var loot = new Core.Rules.LootTable();

        var chestBoss = new DungeonChest(
            "loot_smoke_chest_boss",
            "Boss",
            "chest_stage",
            "chest",
            "run_a_chest_stage_set_4",
            CompletionResult.Completed,
            4);
        var chestReward = loot.RollDungeonChest(chestBoss);
        yield return $"LOOT_CHEST_BOSS equipment={chestReward.Equipment?.DefinitionId ?? "none"} source={chestReward.Equipment?.SourceDungeonTypeId ?? "none"} rarity={chestReward.Equipment?.Rarity ?? "none"}";

        var legsBoss = new DungeonChest(
            "loot_smoke_legs_boss",
            "Boss",
            "legs_stage",
            "legs",
            "run_a_legs_stage_set_4",
            CompletionResult.Completed,
            4);
        var legsReward = loot.RollDungeonChest(legsBoss);
        yield return $"LOOT_LEGS_BOSS equipment={legsReward.Equipment?.DefinitionId ?? "none"} source={legsReward.Equipment?.SourceDungeonTypeId ?? "none"} rarity={legsReward.Equipment?.Rarity ?? "none"}";

        var normalChest = new DungeonChest(
            "loot_smoke_normal",
            "Normal",
            "chest_stage",
            "chest",
            "run_a_chest_stage_set_1",
            CompletionResult.Completed,
            1);
        var normalReward = loot.RollDungeonChest(normalChest);
        yield return $"LOOT_NORMAL gold={normalReward.Gold} equipment={normalReward.Equipment?.DefinitionId ?? "none"}";

        var partialBoss = new DungeonChest(
            "loot_smoke_partial_boss",
            "Boss",
            "core_stage",
            "core",
            "run_a_core_stage_set_4",
            CompletionResult.Partial,
            4);
        var partialReward = loot.RollDungeonChest(partialBoss);
        yield return $"LOOT_PARTIAL_BOSS gold={partialReward.Gold} equipment={partialReward.Equipment?.DefinitionId ?? "none"} rarity={partialReward.Equipment?.Rarity ?? "none"} modifiers={partialReward.Equipment?.Modifiers.Count ?? 0}";

        var duplicateStableChestA = new DungeonChest(
            "loot_smoke_unique",
            "Boss",
            "arms_stage",
            "arms",
            "run_a_arms_stage_set_4",
            CompletionResult.Completed,
            4);
        var duplicateStableChestB = duplicateStableChestA with { InstanceIdPrefix = "run_b_arms_stage_set_4" };
        var uniqueA = loot.RollDungeonChest(duplicateStableChestA).Equipment;
        var uniqueB = loot.RollDungeonChest(duplicateStableChestB).Equipment;
        yield return $"LOOT_INSTANCE_UNIQUE unique={uniqueA?.Id != uniqueB?.Id} first={uniqueA?.Id ?? "none"} second={uniqueB?.Id ?? "none"}";

        var catalog = new TaskCatalog();
        var service = new DungeonRunService();
        var route = catalog.CreateDungeonPlanFromRoute(new[]
        {
            new DungeonRouteSlot("legs", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("core", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("back", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("arms", 4, 12, "Training Loop", 90),
        });
        var run = service.Start(route, 24);
        var stage = run.CurrentStage;
        var partialSummary = new RunSummary(
            "Partial Boss",
            stage.RoomName,
            stage.TotalSets,
            stage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 0, null),
            new[] { CompletionResult.Completed, CompletionResult.Completed, CompletionResult.Completed, CompletionResult.Partial });
        var partialSetSummary = service.RecordStageResult(run, partialSummary);
        var bossReward = partialSetSummary.BankedRewards.FirstOrDefault(reward => reward.ChestTier == "Boss");
        yield return $"LOOT_RUN_PARTIAL bossResult={bossReward?.Result.ToString() ?? "none"} tier={bossReward?.ChestTier ?? "none"} equipment={bossReward?.Reward.Equipment?.DefinitionId ?? "none"}";

        var earlyRun = service.Start(route, 24);
        var earlyStage = earlyRun.CurrentStage;
        var earlySummary = new RunSummary(
            "Early Exit",
            earlyStage.RoomName,
            2,
            earlyStage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 0, null),
            new[] { CompletionResult.Completed, CompletionResult.Completed });
        var earlySetSummary = service.RecordStageResult(earlyRun, earlySummary);
        yield return $"LOOT_RUN_EARLY bossCount={earlySetSummary.BankedRewards.Count(reward => reward.ChestTier == "Boss")} normalCount={earlySetSummary.BankedRewards.Count(reward => reward.ChestTier == "Normal")}";
    }

    private static IEnumerable<string> RunSaveMigrationSmoke()
    {
        var missingLoadoutState = new SaveGameState
        {
            Version = 0,
            Experience = -10,
            ExperienceToNextLevel = 0,
            Inventory = null,
            EquipmentLoadout = null,
            SelectedDungeonRoute = null,
            ActiveStageResults = null,
            ActiveShortTermQuests = null,
        };
        var missingChanged = GameSession.NormalizeSaveState(missingLoadoutState);
        yield return $"MIGRATION_MISSING changed={missingChanged} version={missingLoadoutState.Version} inventory={missingLoadoutState.Inventory?.Count} exp={missingLoadoutState.Experience}/{missingLoadoutState.ExperienceToNextLevel}";

        var duplicateOne = new EquipmentItem(
            "duplicate_item",
            "moon_iron_shortsword",
            "Duplicate Sword",
            EquipmentSlot.Weapon,
            "chest",
            "\u666e\u901a",
            5,
            80,
            new[] { new EquipmentModifier(EquipmentStatType.Attack, 5, string.Empty) });
        var duplicateTwo = new EquipmentItem(
            "duplicate_item",
            "moon_iron_shortsword",
            "Duplicate Sword",
            EquipmentSlot.Weapon,
            "chest",
            "\u666e\u901a",
            5,
            80,
            new[] { new EquipmentModifier(EquipmentStatType.Attack, 5, string.Empty) });
        var invalidLoadoutState = new SaveGameState
        {
            Version = 0,
            Inventory = new List<EquipmentItem> { duplicateOne, duplicateTwo },
            EquipmentLoadout = new EquipmentLoadout
            {
                WeaponId = "missing_item",
                AccessoryId = "duplicate_item",
            },
        };
        var invalidChanged = GameSession.NormalizeSaveState(invalidLoadoutState);
        var distinctIds = invalidLoadoutState.Inventory?.Select(item => item.Id).Distinct().Count() ?? 0;
        yield return $"MIGRATION_INVALID changed={invalidChanged} version={invalidLoadoutState.Version} distinctIds={distinctIds} weapon={invalidLoadoutState.EquipmentLoadout?.WeaponId ?? "none"} accessory={invalidLoadoutState.EquipmentLoadout?.AccessoryId ?? "none"}";
    }

    private static IEnumerable<string> RunCombatSmoke()
    {
        var catalog = new TaskCatalog();
        var enemyCatalog = new EnemyCatalog();
        var roomService = new RoomRunService();
        var route = catalog.CreateDungeonPlanFromRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("legs", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("core", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("arms", 4, 12, "Training Loop", 90),
        });
        var stage = route.Stages[0];
        var enemy = enemyCatalog.GetForDungeon(stage.DungeonTypeId);
        var noGearRoom = roomService.Start(stage, new PlayerCombatStats(24, 3, 0), enemy, 24);
        var normalReps = ResolveFullSet(roomService, noGearRoom, stage.TargetReps);
        var normalResult = roomService.ReportSet(noGearRoom)!;
        ResolveFullSet(roomService, noGearRoom, stage.TargetReps);
        roomService.ReportSet(noGearRoom);
        ResolveFullSet(roomService, noGearRoom, stage.TargetReps);
        roomService.ReportSet(noGearRoom);
        var bossNoGearReps = ResolveFullSet(roomService, noGearRoom, stage.TargetReps);
        var bossNoGear = roomService.ReportSet(noGearRoom)!;
        yield return $"COMBAT_NORMAL killed={normalResult.EnemyDefeated} kind={normalResult.RewardKind} gold={normalResult.Gold} hp={normalResult.PlayerHpAfter}";
        yield return $"COMBAT_REP_DAMAGE firstEnemyHp={normalReps.First().EnemyHpAfter} lastEnemyHp={normalReps.Last().EnemyHpAfter} stopped={normalReps.Last().EnemyDefeated}";
        yield return $"COMBAT_BOSS_NO_GEAR killed={bossNoGear.EnemyDefeated} kind={bossNoGear.RewardKind} gold={bossNoGear.Gold} hp={bossNoGear.PlayerHpAfter}";
        yield return $"COMBAT_BOSS_REP enemyHp={bossNoGearReps.Last().EnemyHpAfter}/{bossNoGearReps.Last().EnemyMaxHp}";

        var gearedRoom = roomService.Start(stage, new PlayerCombatStats(24, 8, 8), enemy, 24);
        ResolveFullSet(roomService, gearedRoom, stage.TargetReps);
        roomService.ReportSet(gearedRoom);
        ResolveFullSet(roomService, gearedRoom, stage.TargetReps);
        roomService.ReportSet(gearedRoom);
        ResolveFullSet(roomService, gearedRoom, stage.TargetReps);
        roomService.ReportSet(gearedRoom);
        ResolveFullSet(roomService, gearedRoom, stage.TargetReps);
        var bossGeared = roomService.ReportSet(gearedRoom)!;
        yield return $"COMBAT_BOSS_GEARED killed={bossGeared.EnemyDefeated} kind={bossGeared.RewardKind} hp={bossGeared.PlayerHpAfter}";

        var evadingRoom = roomService.Start(stage, new PlayerCombatStats(24, 3, 0), enemy, 0);
        ResolveFullSet(roomService, evadingRoom, stage.TargetReps);
        var evading = roomService.ReportSet(evadingRoom)!;
        yield return $"COMBAT_EVADE evading={evading.WasEvading} killed={evading.EnemyDefeated} kind={evading.RewardKind} gold={evading.Gold} hp={evading.PlayerHpAfter}";

        var floorRoom = roomService.Start(stage, new PlayerCombatStats(24, 3, 0), enemy, -23);
        ResolveFullSet(roomService, floorRoom, stage.TargetReps);
        var floor = roomService.ReportSet(floorRoom)!;
        yield return $"COMBAT_HP_FLOOR hp={floor.PlayerHpAfter} min={-24}";

        var deterministicRoomA = roomService.Start(stage, new PlayerCombatStats(24, 3, 0), enemy, 24);
        var deterministicRoomB = roomService.Start(stage, new PlayerCombatStats(24, 3, 0), enemy, 24);
        var deterministicA = ResolveFullSet(roomService, deterministicRoomA, stage.TargetReps);
        var deterministicB = ResolveFullSet(roomService, deterministicRoomB, stage.TargetReps);
        var enemyAttack = deterministicA.FirstOrDefault(result => result.EnemyAttacked);
        yield return $"COMBAT_DETERMINISTIC attacksA={deterministicA.Count(result => result.EnemyAttacked)} attacksB={deterministicB.Count(result => result.EnemyAttacked)} same={deterministicA.Last().PlayerHpAfter == deterministicB.Last().PlayerHpAfter}";
        yield return $"COMBAT_ENEMY_ATTACK attacked={enemyAttack?.EnemyAttacked == true} damageTaken={enemyAttack?.DamageTaken ?? 0} playerHp={enemyAttack?.PlayerHpAfter ?? 0}";
        yield return $"COMBAT_REPS_8 killed={ResolveNormalSetForReps(roomService, catalog, enemyCatalog, 8).EnemyDefeated}";
        yield return $"COMBAT_REPS_10 killed={ResolveNormalSetForReps(roomService, catalog, enemyCatalog, 10).EnemyDefeated}";
        yield return $"COMBAT_REPS_12_CHEST killed={ResolveNormalSetForReps(roomService, catalog, enemyCatalog, 12, "chest").EnemyDefeated}";
        yield return $"COMBAT_REPS_12_CORE killed={ResolveNormalSetForReps(roomService, catalog, enemyCatalog, 12, "core").EnemyDefeated}";

        var levelingPlayer = new PlayerState();
        levelingPlayer.Load(0, null, level: 1, experience: 292, experienceToNextLevel: 300);
        var levelsGained = levelingPlayer.AddExperience(44);
        yield return $"COMBAT_LEVEL gained={levelsGained} level={levelingPlayer.Level} exp={levelingPlayer.Experience}/{levelingPlayer.ExperienceToNextLevel} hp={levelingPlayer.MaxHp} attack={levelingPlayer.Attack}";

        var expSession = new GameSession(persistenceEnabled: false);
        expSession.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("legs", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("core", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("arms", 4, 12, "Training Loop", 90),
        });
        expSession.StartOrGetActiveRun();
        var expStage = expSession.ActiveRun!.CurrentStage;
        expSession.RecordStageResult(new RunSummary(
            "EXP Smoke",
            expStage.RoomName,
            1,
            expStage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 3, null),
            new[] { CompletionResult.Partial },
            new[] { evading },
            evading.PlayerHpAfter,
            44));
        yield return $"COMBAT_SESSION_EXP level={expSession.Player.Level} exp={expSession.Player.Experience}/{expSession.Player.ExperienceToNextLevel}";

        var service = new DungeonRunService();
        var run = service.Start(route, 24);
        var mixedSummary = new RunSummary(
            "Combat Mixed",
            stage.RoomName,
            2,
            stage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, normalResult.Gold + evading.Gold, null),
            new[] { normalResult.Result, evading.Result },
            new[] { normalResult, evading },
            evading.PlayerHpAfter);
        service.RecordStageResult(run, mixedSummary);
        var dailySummary = new DailyRunSummary(run);
        yield return $"COMBAT_DAILY totalGold={dailySummary.TotalGold} chests={dailySummary.ChestCount} rewards={dailySummary.BankedRewardCount}";
    }

    private static IReadOnlyList<CombatRepResult> ResolveFullSet(RoomRunService roomService, RoomRun room, int reps)
    {
        roomService.BeginActiveSet(room);
        var results = new List<CombatRepResult>();
        for (var rep = 0; rep < reps; rep++)
        {
            var result = roomService.ResolveRepHit(room);
            if (result is not null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    private static CombatSetResult ResolveNormalSetForReps(
        RoomRunService roomService,
        TaskCatalog catalog,
        EnemyCatalog enemyCatalog,
        int reps,
        string dungeonTypeId = "chest")
    {
        var route = catalog.CreateDungeonPlanFromRoute(new[]
        {
            new DungeonRouteSlot(dungeonTypeId, 4, reps, "Training Loop", 90),
            new DungeonRouteSlot("legs", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("core", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("arms", 4, 12, "Training Loop", 90),
        });
        var stage = route.Stages[0];
        var room = roomService.Start(stage, new PlayerCombatStats(24, 3, 0), enemyCatalog.GetForDungeon(stage.DungeonTypeId), 24);
        ResolveFullSet(roomService, room, reps);
        return roomService.ReportSet(room)!;
    }
}
