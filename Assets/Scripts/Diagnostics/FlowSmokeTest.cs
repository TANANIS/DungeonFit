using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Gameplay;
using DungeonFit.UI;

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
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("shoulders", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
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
        lines.AddRange(RunBodyProfileSmoke());
        lines.AddRange(RunIdleRewardSmoke());
        lines.AddRange(RunBlacksmithSmoke());
        lines.AddRange(RunBetaContentSmoke());
        lines.AddRange(RunActorVisualSmoke());
        lines.AddRange(RunRunInterruptionSmoke());
        lines.AddRange(RunChurchSmoke());
        lines.AddRange(RunQuestProgressSmoke());
        lines.AddRange(RunRecoveryAndSupplySmoke());

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
        yield return $"LOOT_CHEST_BOSS equipment={chestReward.Equipment?.DefinitionId ?? "none"} source={chestReward.Equipment?.SourceDungeonTypeId ?? "none"} rarity={chestReward.Equipment?.Rarity ?? "none"} icon={(string.IsNullOrWhiteSpace(chestReward.Equipment?.IconPath) ? "missing" : "set")}";

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
        yield return $"LOOT_NORMAL gold={normalReward.Gold} equipment={normalReward.Equipment?.DefinitionId ?? "none"} rarity={normalReward.Equipment?.Rarity ?? "none"}";

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
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("back", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
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
        yield return $"LOOT_RUN_EARLY bossCount={earlySetSummary.BankedRewards.Count(reward => reward.ChestTier == "Boss")} normalCount={earlySetSummary.BankedRewards.Count(reward => reward.ChestTier == "Normal")} equipment={earlySetSummary.BankedRewards.Count(reward => reward.Reward.Equipment is not null)}";
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
        yield return $"MIGRATION_HP currentHp={missingLoadoutState.CurrentHp} dailyKey={(string.IsNullOrWhiteSpace(missingLoadoutState.DailyStateKey) ? "none" : "set")}";
        yield return $"MIGRATION_IDLE unclaimed={missingLoadoutState.UnclaimedIdleGold} timestamp={missingLoadoutState.IdleLastCalculatedAtUtc.HasValue}";
        yield return $"MIGRATION_CHURCH active={(missingLoadoutState.ActiveLongTermQuest is null ? "none" : missingLoadoutState.ActiveLongTermQuest.QuestId)} claimed={missingLoadoutState.ClaimedLongTermQuestIds?.Count ?? -1} titles={missingLoadoutState.UnlockedTitles?.Count ?? -1}";
        yield return $"MIGRATION_BODY profileCompleted={missingLoadoutState.Profile?.HasCompletedOnboarding} metrics={missingLoadoutState.BodyMetrics?.Count ?? -1}";
        yield return $"MIGRATION_DUNGEON_PROGRESS count={missingLoadoutState.DungeonProgress?.Count ?? -1}";

        var duplicateOne = new EquipmentItem(
            "duplicate_item",
            "moon_iron_shortsword",
            "Duplicate Sword",
            EquipmentSlot.Weapon,
            string.Empty,
            "chest",
            "\u666e\u901a",
            1,
            5,
            5,
            80,
            new[] { new EquipmentModifier(EquipmentStatType.Attack, 5, string.Empty) });
        var duplicateTwo = new EquipmentItem(
            "duplicate_item",
            "moon_iron_shortsword",
            "Duplicate Sword",
            EquipmentSlot.Weapon,
            string.Empty,
            "chest",
            "\u666e\u901a",
            1,
            5,
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
        invalidLoadoutState.Inventory[0].EnhancementLevel = 9;
        invalidLoadoutState.Inventory[1].EnhancementLevel = -2;
        var invalidChanged = GameSession.NormalizeSaveState(invalidLoadoutState);
        var distinctIds = invalidLoadoutState.Inventory?.Select(item => item.Id).Distinct().Count() ?? 0;
        yield return $"MIGRATION_INVALID changed={invalidChanged} version={invalidLoadoutState.Version} distinctIds={distinctIds} weapon={invalidLoadoutState.EquipmentLoadout?.WeaponId ?? "none"} accessory={invalidLoadoutState.EquipmentLoadout?.AccessoryId ?? "none"}";
        yield return $"MIGRATION_ENHANCEMENT first={invalidLoadoutState.Inventory![0].EnhancementLevel} second={invalidLoadoutState.Inventory[1].EnhancementLevel}";
    }

    private static IEnumerable<string> RunBodyProfileSmoke()
    {
        var session = new GameSession(persistenceEnabled: false);
        var invalidHeight = session.UpdatePlayerProfile(90, FitnessGoal.MuscleGain);
        var savedProfile = session.UpdatePlayerProfile(172, "bad_goal");
        var profile = session.BuildBodyProfileViewModel(new DateTime(2026, 6, 2));
        yield return $"BODY_PROFILE invalidHeight={invalidHeight} saved={savedProfile} completed={profile.HasCompletedOnboarding} height={profile.HeightCm} goal={profile.GoalId}";

        var firstWeight = session.RecordTodayWeight(72.44, new DateTime(2026, 6, 2));
        var secondWeight = session.RecordTodayWeight(71.96, new DateTime(2026, 6, 2));
        var invalidWeight = session.RecordTodayWeight(251, new DateTime(2026, 6, 2));
        var body = session.BuildBodyProfileViewModel(new DateTime(2026, 6, 2));
        yield return $"BODY_WEIGHT first={firstWeight} second={secondWeight} invalid={invalidWeight} count={session.BodyMetrics.Count} today={body.TodayWeightKg:0.0}";

        var badState = new SaveGameState
        {
            Profile = new PlayerProfile
            {
                HeightCm = 10,
                GoalId = "unknown",
                HasCompletedOnboarding = true,
            },
            BodyMetrics = new List<BodyMetricEntry>
            {
                new()
                {
                    DateKey = "2026-06-02",
                    WeightKg = 72.444,
                    RecordedAtUtc = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    DateKey = "2026-06-02",
                    WeightKg = 70.0,
                    RecordedAtUtc = new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    DateKey = "bad",
                    WeightKg = 10,
                    RecordedAtUtc = DateTime.UtcNow,
                },
            },
        };
        var normalized = GameSession.NormalizeSaveState(badState);
        yield return $"BODY_NORMALIZE changed={normalized} completed={badState.Profile?.HasCompletedOnboarding} goal={badState.Profile?.GoalId} metrics={badState.BodyMetrics?.Count ?? -1} latest={badState.BodyMetrics?.FirstOrDefault()?.WeightKg:0.0}";
    }

    private static IEnumerable<string> RunIdleRewardSmoke()
    {
        var session = new GameSession(persistenceEnabled: false);
        var start = DateTime.UtcNow;
        session.RefreshIdleRewards(start);
        var fresh = session.BuildIdleRewardViewModel(start);
        yield return $"IDLE_FRESH gold={fresh.UnclaimedGold} canClaim={fresh.CanClaim}";

        session.RefreshIdleRewards(start.AddMinutes(10));
        var tenMinutes = session.BuildIdleRewardViewModel(start.AddMinutes(10));
        yield return $"IDLE_TEN_MINUTES gold={tenMinutes.UnclaimedGold} canClaim={tenMinutes.CanClaim}";

        var firstClaim = session.ClaimIdleRewards(start.AddMinutes(10));
        var secondClaim = session.ClaimIdleRewards(start.AddMinutes(10));
        yield return $"IDLE_CLAIM first={firstClaim} second={secondClaim} playerGold={session.Player.Gold}";

        session.RefreshIdleRewards(start.AddHours(24));
        var capped = session.BuildIdleRewardViewModel(start.AddHours(24));
        yield return $"IDLE_CAP gold={capped.UnclaimedGold}/{capped.MaxUnclaimedGold}";

        session.ClaimIdleRewards(start.AddHours(24));
        session.RefreshIdleRewards(start.AddHours(24).AddMinutes(9));
        var partial = session.BuildIdleRewardViewModel(start.AddHours(24).AddMinutes(9));
        session.RefreshIdleRewards(start.AddHours(24).AddMinutes(10));
        var nextTick = session.BuildIdleRewardViewModel(start.AddHours(24).AddMinutes(10));
        yield return $"IDLE_PARTIAL before={partial.UnclaimedGold} after={nextTick.UnclaimedGold}";
    }

    private static IEnumerable<string> RunBlacksmithSmoke()
    {
        var empty = new GameSession(persistenceEnabled: false);
        var emptyModel = empty.BuildBlacksmithViewModel();
        yield return $"BLACKSMITH_EMPTY items={emptyModel.Items.Count} canEnhance={emptyModel.CanEnhance}";

        var item = new EquipmentItem(
            "blacksmith_smoke_sword",
            "moon_iron_shortsword",
            "Smoke Sword",
            EquipmentSlot.Weapon,
            "res://Assets/Art/Items/Weapons/moon_blade.png",
            "chest",
            "\u666e\u901a",
            1,
            5,
            5,
            80,
            new[] { new EquipmentModifier(EquipmentStatType.Attack, 5, string.Empty) });
        var session = new GameSession(persistenceEnabled: false);
        session.Player.Load(40, new[] { item });
        yield return $"BLACKSMITH_NO_GOLD enhance={session.EnhanceEquipment(item.Id)} gold={session.Player.Gold} level={item.EnhancementLevel}";

        session.Player.Load(1000, new[] { item });
        session.EquipItem(item.Id);
        session.SetEquipmentLocked(item.Id, true);
        var scoreBefore = session.Player.EquipmentScore;
        var enhanceOne = session.EnhanceEquipment(item.Id);
        yield return $"BLACKSMITH_ENHANCE equippedLocked={enhanceOne} gold={session.Player.Gold} level={item.EnhancementLevel} power={item.Power} score={scoreBefore}->{session.Player.EquipmentScore}";

        while (session.EnhanceEquipment(item.Id))
        {
        }

        yield return $"BLACKSMITH_CAP level={item.EnhancementLevel} power={item.Power} extra={session.EnhanceEquipment(item.Id)}";
        var extensionBefore = item.EffectiveRecommendedLevelMax;
        var extend = session.ExtendEquipmentLevelRange(item.Id);
        yield return $"BLACKSMITH_EXTEND result={extend} max={extensionBefore}->{item.EffectiveRecommendedLevelMax} gold={session.Player.Gold}";

        var serialized = JsonSerializer.Serialize(new SaveGameState
        {
            Inventory = new List<EquipmentItem> { item },
        });
        var restored = JsonSerializer.Deserialize<SaveGameState>(serialized);
        var restoredItem = restored?.Inventory?.FirstOrDefault();
        yield return $"BLACKSMITH_SAVE_ROUNDTRIP level={restoredItem?.EnhancementLevel ?? -1} power={restoredItem?.Power ?? -1}";

        var goldBeforeDismantle = session.Player.Gold;
        var dismantle = session.DismantleEnhancement(item.Id);
        yield return $"BLACKSMITH_DISMANTLE result={dismantle} level={item.EnhancementLevel} power={item.Power} refund={session.Player.Gold - goldBeforeDismantle}";
        yield return $"BLACKSMITH_DISMANTLE_ZERO result={session.DismantleEnhancement(item.Id)} level={item.EnhancementLevel}";
    }

    private static IEnumerable<string> RunBetaContentSmoke()
    {
        var equipment = new EquipmentCatalog();
        var lootProfiles = new DungeonLootProfileCatalog();
        var exercises = new ExerciseCatalog();
        var shortQuests = new ShortTermQuestCatalog();
        var longQuests = new LongTermQuestCatalog();
        yield return $"BETA_CONTENT equipment={equipment.GetAll().Count} lootProfiles={lootProfiles.GetAll().Count} exercises={exercises.GetAll().Count} shortDaily={shortQuests.GetDailyBoard().Count} longQuests={longQuests.GetAll().Count}";
        var iconCount = equipment.GetAll().Count(definition => !string.IsNullOrWhiteSpace(definition.IconPath));
        var uniqueNames = equipment.GetAll().Select(definition => definition.DisplayName).Distinct().Count();
        yield return $"EQUIPMENT_POOL definitions={equipment.GetAll().Count} icons={iconCount} uniqueNames={uniqueNames}";

        foreach (var profile in lootProfiles.GetAll())
        {
            var dungeonDefinitions = equipment.GetForDungeon(profile.DungeonTypeId);
            var slots = string.Join(",", dungeonDefinitions.Select(definition => definition.Slot).Distinct().OrderBy(slot => slot));
            yield return $"BETA_LOOT_PROFILE dungeon={profile.DungeonTypeId} definitions={profile.EquipmentDefinitionIds.Count} generated={dungeonDefinitions.Count} slots={slots} extras={profile.ExtraModifierCandidates.Count}";
        }

        foreach (var dungeonTypeId in new[] { "chest", "shoulders", "back", "legs", "core", "arms" })
        {
            var dungeonExercises = exercises.GetForDungeon(dungeonTypeId);
            var complete = dungeonExercises.Count >= 8 &&
                dungeonExercises.Count(exercise => exercise.IsRecommended) == 1 &&
                dungeonExercises.All(exercise =>
                    !string.IsNullOrWhiteSpace(exercise.Name) &&
                    !string.IsNullOrWhiteSpace(exercise.TrainingType) &&
                    !string.IsNullOrWhiteSpace(exercise.Summary) &&
                    !string.IsNullOrWhiteSpace(exercise.SafetyNote));
            yield return $"BETA_EXERCISES dungeon={dungeonTypeId} count={dungeonExercises.Count} recommended={dungeonExercises.Count(exercise => exercise.IsRecommended)} complete={complete}";
        }
    }

    private static IEnumerable<string> RunActorVisualSmoke()
    {
        var visuals = new ActorVisualCatalog();
        var allVisualIds = new[]
        {
            ActorVisualIds.SlimeBasic,
            ActorVisualIds.SkeletonBasic,
            ActorVisualIds.SkeletonArcher,
            ActorVisualIds.SkeletonArmored,
            ActorVisualIds.SkeletonGreatsword,
            ActorVisualIds.OrcBasic,
            ActorVisualIds.OrcArmored,
            ActorVisualIds.OrcElite,
            ActorVisualIds.OrcRiderBoss,
            ActorVisualIds.AxemanArmored,
            ActorVisualIds.WerewolfBoss,
            ActorVisualIds.WerebearBoss,
        };
        var resolvedVisualIds = allVisualIds
            .Select(id => visuals.Get(id).Id)
            .ToList();
        var fallback = visuals.Get("missing_visual");
        yield return $"ACTOR_VISUALS count={resolvedVisualIds.Count} unique={resolvedVisualIds.Distinct().Count()} fallback={fallback.Id}";

        var enemies = new EnemyCatalog();
        var dungeonVisuals = new[] { "chest", "shoulders", "back", "legs", "core", "arms" }
            .Select(id =>
            {
                var enemy = enemies.GetForDungeon(id);
                return $"{id}:{enemy.NormalVisualId}/{enemy.EliteVisualId}/{enemy.BossVisualId}";
            });
        var missing = enemies.GetForDungeon("missing");
        yield return $"ENEMY_VISUAL_MAP {string.Join(" ", dungeonVisuals)} missing={missing.NormalVisualId}/{missing.EliteVisualId}/{missing.BossVisualId}";

        var slimeSet = visuals.Get(ActorVisualIds.SlimeBasic).ToAnimationSet();
        yield return $"ACTOR_VISUAL_SET idle={slimeSet.IdlePath.EndsWith("idle.png")} attack={slimeSet.AttackPath.EndsWith("attack_01.png")} blockFallback={string.IsNullOrEmpty(slimeSet.BlockPath)}";

        var chestEnemy = enemies.GetForDungeon("chest");
        var fourSet = new[]
        {
            BattleEncounterView.ResolveEnemyVisualId(chestEnemy, new RoomProgress(1, 4, false, false, false)),
            BattleEncounterView.ResolveEnemyVisualId(chestEnemy, new RoomProgress(2, 4, false, false, false)),
            BattleEncounterView.ResolveEnemyVisualId(chestEnemy, new RoomProgress(3, 4, false, false, false)),
            BattleEncounterView.ResolveEnemyVisualId(chestEnemy, new RoomProgress(4, 4, true, false, false)),
        };
        var twoSet = new[]
        {
            BattleEncounterView.ResolveEnemyVisualId(chestEnemy, new RoomProgress(1, 2, false, false, false)),
            BattleEncounterView.ResolveEnemyVisualId(chestEnemy, new RoomProgress(2, 2, true, false, false)),
        };
        var oneSet = BattleEncounterView.ResolveEnemyVisualId(chestEnemy, new RoomProgress(1, 1, true, false, false));
        yield return $"ENEMY_VISUAL_RULE sets4={string.Join(",", fourSet)} sets2={string.Join(",", twoSet)} sets1={oneSet}";
    }

    private static IEnumerable<string> RunRunInterruptionSmoke()
    {
        var session = new GameSession(persistenceEnabled: false);
        session.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        var activeRun = session.StartOrGetActiveRun();
        session.LeaveActiveRoom(7);
        yield return $"ROOM_INTERRUPT active={session.ActiveRun is not null} hp={session.ActiveRun?.CurrentPlayerHp ?? -1} playerHp={session.Player.CurrentHp} stage={session.ActiveRun?.CurrentStageIndex ?? -1}";

        var stage = activeRun!.CurrentStage;
        session.RecordStageResult(new RunSummary(
            "Interrupt Clear",
            stage.RoomName,
            stage.TotalSets,
            stage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 60, null),
            CompletedResults(stage.TotalSets),
            ClearedCombatResults(stage.TotalSets),
            7));
        var beforeClaim = session.Player.Gold;
        session.ClaimDailyRewards();
        var afterFirstClaim = session.Player.Gold;
        session.ClaimDailyRewards();
        yield return $"DAILY_CLAIM_GUARD before={beforeClaim} first={afterFirstClaim} second={session.Player.Gold} claimed={session.DailyRewardsClaimed}";
    }

    private static IEnumerable<string> RunChurchSmoke()
    {
        var session = new GameSession(persistenceEnabled: false);
        var fresh = session.BuildChurchViewModel();
        yield return $"CHURCH_EMPTY cards={fresh.Cards.Count} active={fresh.ActiveQuest?.QuestId ?? "none"} candidates={fresh.Cards.Count(card => card.CanSelect)} locked={fresh.Cards.Count(card => card.Status == ChurchQuestStatus.Locked)}";

        var acceptMayor = session.AcceptLongTermQuest("mayor_missing_daughter");
        var acceptSecond = session.AcceptLongTermQuest("blacksmith_unfinished_blade");
        yield return $"CHURCH_ACCEPT first={acceptMayor} second={acceptSecond} active={session.ActiveLongTermQuest?.QuestId ?? "none"}";

        session.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
        });
        session.StartOrGetActiveRun();
        for (var index = 0; index < 3; index++)
        {
            var stage = session.ActiveRun!.CurrentStage;
            session.RecordStageResult(BuildChurchStageSummary(stage, 50, defeatedBoss: true));
        }

        yield return $"CHURCH_PROGRESS quest={session.ActiveLongTermQuest?.QuestId ?? "none"} progress={session.ActiveLongTermQuest?.Progress ?? -1} completed={session.ActiveLongTermQuest?.IsCompleted ?? false}";
        var goldBeforeClaim = session.Player.Gold;
        var claim = session.ClaimLongTermQuestReward();
        var claimAgain = session.ClaimLongTermQuestReward();
        yield return $"CHURCH_CLAIM result={claim} again={claimAgain} gold={goldBeforeClaim}->{session.Player.Gold} titles={session.UnlockedTitles.Count} active={session.ActiveLongTermQuest?.QuestId ?? "none"}";

        var bossSession = new GameSession(persistenceEnabled: false);
        bossSession.AcceptLongTermQuest("blacksmith_unfinished_blade");
        bossSession.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        bossSession.StartOrGetActiveRun();
        var bossStage = bossSession.ActiveRun!.CurrentStage;
        bossSession.RecordStageResult(BuildChurchStageSummary(bossStage, 10, defeatedBoss: false));
        var afterNoBoss = bossSession.ActiveLongTermQuest?.Progress ?? -1;
        bossSession.RecordStageResult(BuildChurchStageSummary(bossStage, 10, defeatedBoss: true));
        yield return $"CHURCH_BOSS_PROGRESS noBoss={afterNoBoss} boss={bossSession.ActiveLongTermQuest?.Progress ?? -1}";

        var goldSession = new GameSession(persistenceEnabled: false);
        goldSession.AcceptLongTermQuest("herbalist_moondew_research");
        goldSession.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("back", 4, 12, "chest_quest_01", 90),
        });
        goldSession.StartOrGetActiveRun();
        var goldStage = goldSession.ActiveRun!.CurrentStage;
        goldSession.RecordStageResult(BuildChurchStageSummary(goldStage, 300, defeatedBoss: true));
        yield return $"CHURCH_GOLD_PROGRESS progress={goldSession.ActiveLongTermQuest?.Progress ?? -1}";

        var abandon = goldSession.AbandonLongTermQuest();
        yield return $"CHURCH_ABANDON result={abandon} active={goldSession.ActiveLongTermQuest?.QuestId ?? "none"}";

        var serialized = JsonSerializer.Serialize(new SaveGameState
        {
            ActiveLongTermQuest = new ActiveLongTermQuest
            {
                QuestId = "priest_faint_faith",
                Progress = 2,
            },
            UnlockedTitles = new List<string> { "鎮民的信任" },
            ClaimedLongTermQuestIds = new List<string> { "mayor_missing_daughter" },
        });
        var restored = JsonSerializer.Deserialize<SaveGameState>(serialized)!;
        var normalized = GameSession.NormalizeSaveState(restored);
        yield return $"CHURCH_SAVE_ROUNDTRIP normalized={normalized} active={restored.ActiveLongTermQuest?.QuestId ?? "none"} progress={restored.ActiveLongTermQuest?.Progress ?? -1} titles={restored.UnlockedTitles?.Count ?? -1}";
    }

    private static RunSummary BuildChurchStageSummary(TaskTemplate stage, int gold, bool defeatedBoss)
    {
        return new RunSummary(
            "Church Smoke",
            stage.RoomName,
            stage.TotalSets,
            stage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, gold, null),
            CompletedResults(stage.TotalSets),
            ClearedCombatResults(stage.TotalSets)
                .Select(result => result with
                {
                    EnemyDefeated = !result.IsBoss || defeatedBoss,
                    RewardKind = !result.IsBoss || defeatedBoss ? BankedRewardKind.Chest : BankedRewardKind.GoldOnly,
                })
                .ToArray(),
            24);
    }

    private static IEnumerable<string> RunRecoveryAndSupplySmoke()
    {
        var recovery = new GameSession(persistenceEnabled: false);
        recovery.Player.SetCurrentHp(0);
        var moonFirst = recovery.UseMoonlightRecovery();
        var moonHp = recovery.Player.CurrentHp;
        var moonSecond = recovery.UseMoonlightRecovery();
        yield return $"RECOVERY_MOON first={moonFirst} second={moonSecond} hp={moonHp}/{recovery.Player.MaxHp}";

        var moonGuard = new GameSession(persistenceEnabled: false);
        var moonGuardSelected = moonGuard.SelectDailyBlessing(DailyBlessing.MoonGuard);
        yield return $"BLESSING_HP selected={moonGuardSelected} maxHp={moonGuard.Player.MaxHp}";

        var bladeMoon = new GameSession(persistenceEnabled: false);
        bladeMoon.SelectDailyBlessing(DailyBlessing.BladeMoon);
        var catalog = new TaskCatalog();
        var enemyCatalog = new EnemyCatalog();
        var roomService = new RoomRunService();
        var route = catalog.CreateDungeonPlanFromRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        var stage = route.Stages[0];
        var attackRoom = roomService.Start(stage, bladeMoon.Player.CombatStats, enemyCatalog.GetForDungeon(stage.DungeonTypeId), bladeMoon.Player.CurrentHp);
        var attackRep = ResolveFullSet(roomService, attackRoom, 1).First();
        yield return $"BLESSING_ATTACK attack={bladeMoon.Player.Attack} firstDamage={attackRep.DamageDealt}";

        var starlight = new GameSession(persistenceEnabled: false);
        starlight.SelectDailyBlessing(DailyBlessing.StarlightGold);
        var goldRoom = roomService.Start(stage, starlight.Player.CombatStats, enemyCatalog.GetForDungeon(stage.DungeonTypeId), starlight.Player.CurrentHp);
        ResolveFullSet(roomService, goldRoom, stage.TargetReps);
        var goldResult = roomService.ReportSet(goldRoom)!;
        yield return $"BLESSING_GOLD bonus={starlight.Player.DungeonGoldBonusPercent} gold={goldResult.Gold}";

        var lockedBlessing = new GameSession(persistenceEnabled: false);
        lockedBlessing.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        lockedBlessing.StartOrGetActiveRun();
        yield return $"BLESSING_LOCKED selected={lockedBlessing.SelectDailyBlessing(DailyBlessing.StarlightGold)}";

        var herb = new GameSession(persistenceEnabled: false);
        herb.Player.Load(300, null, currentHp: 0);
        var basic = herb.BuyBasicHeal();
        var afterBasic = $"{herb.Player.CurrentHp}/{herb.Player.MaxHp}/{herb.Player.Gold}";
        var full = herb.BuyFullHeal();
        yield return $"HERB_HEAL basic={basic} afterBasic={afterBasic} full={full} hp={herb.Player.CurrentHp}/{herb.Player.MaxHp} gold={herb.Player.Gold}";

        var supply = new GameSession(persistenceEnabled: false);
        supply.Player.Load(200, null, currentHp: -5);
        var buy1 = supply.BuyHerbShopPotion();
        var buy2 = supply.BuyHerbShopPotion();
        var buy3 = supply.BuyHerbShopPotion();
        var buy4 = supply.BuyHerbShopPotion();
        var supplyUse = supply.UseSmallPotionInRoom(-5);
        yield return $"HERB_POTION buys={buy1}/{buy2}/{buy3}/{buy4} used={supplyUse.Used} healed={supplyUse.Healed} hp={supplyUse.CurrentHp} count={supplyUse.Supply.SmallPotionCount}";

        var hpSession = new GameSession(persistenceEnabled: false);
        hpSession.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        hpSession.StartOrGetActiveRun();
        var hpStage = hpSession.ActiveRun!.CurrentStage;
        hpSession.RecordStageResult(new RunSummary(
            "HP Persist",
            hpStage.RoomName,
            1,
            hpStage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 3, null),
            new[] { CompletionResult.Partial },
            null,
            -5));
        var tavern = hpSession.BuildTavernEquipmentViewModel();
        yield return $"HP_PERSIST player={hpSession.Player.CurrentHp} tavern={tavern.Character.CurrentHp}/{tavern.Character.MaxHp}";
    }

    private static IEnumerable<string> RunQuestProgressSmoke()
    {
        var shortPartial = BuildQuestSession("chest");
        shortPartial.AcceptShortTermQuest("herbal_chest");
        RecordSyntheticStage(shortPartial, CompletionResult.Partial, 1);
        yield return $"QUEST_SHORT_PARTIAL progress={shortPartial.ActiveShortTermQuests.First().Progress}";

        var shortComplete = BuildQuestSession("chest");
        shortComplete.AcceptShortTermQuest("herbal_chest");
        RecordSyntheticStage(shortComplete, CompletionResult.Completed, 4);
        yield return $"QUEST_SHORT_COMPLETE progress={shortComplete.ActiveShortTermQuests.First().Progress}";

        var longPartial = BuildQuestSession("chest");
        longPartial.AcceptLongTermQuest("guard_gate_disturbance");
        RecordSyntheticStage(longPartial, CompletionResult.Partial, 1);
        yield return $"QUEST_LONG_PARTIAL progress={longPartial.ActiveLongTermQuest?.Progress ?? -1}";

        var longComplete = BuildQuestSession("chest");
        longComplete.AcceptLongTermQuest("guard_gate_disturbance");
        RecordSyntheticStage(longComplete, CompletionResult.Completed, 4);
        yield return $"QUEST_LONG_COMPLETE progress={longComplete.ActiveLongTermQuest?.Progress ?? -1}";
    }

    private static GameSession BuildQuestSession(string firstDungeonType)
    {
        var session = new GameSession(persistenceEnabled: false);
        session.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot(firstDungeonType, 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        session.StartOrGetActiveRun();
        return session;
    }

    private static void RecordSyntheticStage(GameSession session, CompletionResult result, int completedSets)
    {
        var stage = session.ActiveRun!.CurrentStage;
        var setResults = Enumerable.Range(1, stage.TotalSets)
            .Select(set => set <= completedSets ? result : CompletionResult.Skipped)
            .ToArray();
        var combatResults = result == CompletionResult.Completed
            ? ClearedCombatResults(stage.TotalSets)
            : Array.Empty<CombatSetResult>();
        session.RecordStageResult(new RunSummary(
            "Quest Smoke",
            stage.RoomName,
            completedSets,
            stage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, completedSets * 3, null),
            setResults,
            combatResults,
            24));
    }

    private static IEnumerable<string> RunCombatSmoke()
    {
        var catalog = new TaskCatalog();
        var enemyCatalog = new EnemyCatalog();
        var roomService = new RoomRunService();
        var route = catalog.CreateDungeonPlanFromRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
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
        yield return $"COMBAT_HP_FLOOR hp={floor.PlayerHpAfter} min={0}";

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
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
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

        var autoExpSession = new GameSession(persistenceEnabled: false);
        autoExpSession.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("back", 4, 12, "chest_quest_01", 90),
        });
        autoExpSession.StartOrGetActiveRun();
        var autoStage = autoExpSession.ActiveRun!.CurrentStage;
        autoExpSession.RecordStageResult(new RunSummary(
            "Auto EXP Smoke",
            autoStage.RoomName,
            0,
            autoStage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 0, null),
            Array.Empty<CompletionResult>(),
            Array.Empty<CombatSetResult>(),
            24));
        yield return $"TRAINING_GROWTH_AUTO gained={autoExpSession.LastRunSummary?.ExperienceGained ?? 0} exp={autoExpSession.Player.Experience}/{autoExpSession.Player.ExperienceToNextLevel}";

        var dungeonGrowthSession = new GameSession(persistenceEnabled: false);
        dungeonGrowthSession.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        var initialDungeonLevel = dungeonGrowthSession.SelectedPlan.Stages[0].DungeonLevel;
        dungeonGrowthSession.StartOrGetActiveRun();
        var growthStage = dungeonGrowthSession.ActiveRun!.CurrentStage;
        dungeonGrowthSession.RecordStageResult(new RunSummary(
            "Dungeon Growth Smoke",
            growthStage.RoomName,
            growthStage.TotalSets,
            growthStage.TotalSets,
            new RewardBundle(RewardSource.DungeonRoom, 40, null),
            CompletedResults(growthStage.TotalSets),
            ClearedCombatResults(growthStage.TotalSets),
            24));
        var chestProgress = dungeonGrowthSession.DungeonProgress.First(progress => progress.DungeonTypeId == "chest");
        dungeonGrowthSession.CompleteDailyRun();
        dungeonGrowthSession.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        yield return $"DUNGEON_GROWTH dungeon=chest initial={initialDungeonLevel} level={chestProgress.Level} exp={chestProgress.Experience}/{chestProgress.ExperienceToNextLevel} rooms={chestProgress.CompletedRooms} bosses={chestProgress.BossClears} nextStageLevel={dungeonGrowthSession.SelectedPlan.Stages[0].DungeonLevel}";

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
            new DungeonRouteSlot(dungeonTypeId, 4, reps, "chest_quest_01", 90),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });
        var stage = route.Stages[0];
        var room = roomService.Start(stage, new PlayerCombatStats(24, 3, 0), enemyCatalog.GetForDungeon(stage.DungeonTypeId), 24);
        ResolveFullSet(roomService, room, reps);
        return roomService.ReportSet(room)!;
    }
}
