using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;

namespace DungeonFit.Gameplay;

public sealed class GameSession
{
    private const double MoonlightRecoveryPercent = 0.5;
    private const double RoomRecoveryPercent = 0.25;
    private const double BasicHealPercent = 0.4;
    private const double SmallPotionHealPercent = 0.45;
    private const int BasicHealCost = 60;
    private const int FullHealCost = 140;
    private const int SmallPotionCost = 35;
    private const int SmallPotionDailyPurchaseLimit = 4;
    private const int SmallPotionCarryLimit = 4;
    private const int StarterSmallPotionCount = 2;
    private const int RecommendedRouteStages = 4;
    private const int FatiguedRewardPercent = 25;
    private const double FatiguedRewardMultiplier = 0.25;
    private const int IdleRewardIntervalMinutes = 10;
    private const int IdleRewardGoldPerInterval = 1;
    private const int IdleRewardMaxUnclaimedGold = 72;

    private readonly TaskCatalog _taskCatalog = new();
    private readonly DungeonRunService _dungeonRunService = new();
    private readonly DungeonRouteRules _routeRules = new();
    private readonly LootRoller _lootRoller = new();
    private readonly ShortTermQuestCatalog _shortTermQuestCatalog = new();
    private readonly LongTermQuestCatalog _longTermQuestCatalog = new();
    private readonly SaveService _saveService = new();
    private readonly bool _persistenceEnabled;
    private string? _saveWarningMessage;
    private string _noticeBoardRefreshKey;
    private bool _noticeBoardRefreshKeyNeedsSave;
    private string _dailyStateKey;
    private bool _moonlightRecoveryUsed;
    private int _smallPotionCount;
    private int _herbShopPotionPurchasesToday;
    private DateTime _idleLastCalculatedAtUtc;
    private int _unclaimedIdleGold;
    private PlayerProfile _profile = CreateIncompleteProfile();
    private TutorialProgress _tutorial = new();
    private readonly List<BodyMetricEntry> _bodyMetrics = new();
    private readonly List<DungeonProgressEntry> _dungeonProgress = new();

    public GameSession(bool persistenceEnabled = true)
    {
        _persistenceEnabled = persistenceEnabled;
        _noticeBoardRefreshKey = GetTodayRefreshKey();
        _dailyStateKey = GetTodayRefreshKey();
        _idleLastCalculatedAtUtc = GetUtcNow();
        SelectedDungeonRoute = new List<DungeonRouteSlot>();
        SelectedPlan = DungeonPlan.Empty;

        if (_persistenceEnabled)
        {
            var loadResult = _saveService.Load();
            _saveWarningMessage = loadResult.Status == SaveLoadStatus.Corrupted
                ? "存檔讀取失敗，已使用新狀態。"
                : null;
            Restore(loadResult.State);
        }
    }

    public PlayerState Player { get; } = new();

    public IReadOnlyList<DungeonRouteSlot> SelectedDungeonRoute { get; private set; }

    public DungeonPlan SelectedPlan { get; private set; }

    public DungeonRun? ActiveRun { get; private set; }

    public RunSummary? LastRunSummary { get; private set; }

    public SetSummary? LastSetSummary { get; private set; }

    public bool DailyRewardsClaimed { get; private set; }

    public IReadOnlyList<ActiveShortTermQuest> ActiveShortTermQuests { get; private set; } = new List<ActiveShortTermQuest>();

    public ActiveLongTermQuest? ActiveLongTermQuest { get; private set; }

    public IReadOnlyList<string> ClaimedLongTermQuestIds { get; private set; } = new List<string>();

    public IReadOnlyList<string> UnlockedTitles { get; private set; } = new List<string>();

    public bool CanEditPlan => ActiveRun is null;

    public PlayerProfile Profile => _profile;

    public IReadOnlyList<BodyMetricEntry> BodyMetrics => _bodyMetrics;

    public IReadOnlyList<DungeonProgressEntry> DungeonProgress => _dungeonProgress;

    public SaveStatus GetSaveStatus()
    {
        return new SaveStatus(
            _persistenceEnabled && _saveService.HasSave(),
            Player.Gold,
            SelectedDungeonRoute.Count,
            ActiveRun?.CompletedStages ?? 0,
            ActiveRun?.BankedRewards.Count ?? 0,
            ActiveRun?.BankedRewards.Count(reward => reward.IsChest) ?? 0,
            DailyRewardsClaimed,
            _saveWarningMessage);
    }

    public void ManualSave()
    {
        RefreshNoticeBoardIfExpired();
        RefreshIdleRewards();
        Save();
        _saveWarningMessage = null;
    }

    public void DeleteSaveAndReset()
    {
        if (_persistenceEnabled)
        {
            _saveService.Delete();
        }

        Player.Load(0, null);
        SelectedDungeonRoute = new List<DungeonRouteSlot>();
        SelectedPlan = DungeonPlan.Empty;
        ActiveRun = null;
        LastRunSummary = null;
        LastSetSummary = null;
        DailyRewardsClaimed = false;
        ActiveShortTermQuests = new List<ActiveShortTermQuest>();
        ActiveLongTermQuest = null;
        ClaimedLongTermQuestIds = new List<string>();
        UnlockedTitles = new List<string>();
        _profile = CreateIncompleteProfile();
        _tutorial = new TutorialProgress();
        _bodyMetrics.Clear();
        _dungeonProgress.Clear();
        _noticeBoardRefreshKey = GetTodayRefreshKey();
        _dailyStateKey = GetTodayRefreshKey();
        _moonlightRecoveryUsed = false;
        _smallPotionCount = 0;
        _herbShopPotionPurchasesToday = 0;
        _idleLastCalculatedAtUtc = GetUtcNow();
        _unclaimedIdleGold = 0;
        _saveWarningMessage = null;
    }

    public void RefreshNoticeBoardIfExpired()
    {
        var todayKey = GetTodayRefreshKey();
        RefreshDailyStateIfExpired(todayKey);

        if (_noticeBoardRefreshKeyNeedsSave && _noticeBoardRefreshKey == todayKey)
        {
            _noticeBoardRefreshKeyNeedsSave = false;
            Save();
            return;
        }

        if (_noticeBoardRefreshKey == todayKey)
        {
            return;
        }

        _noticeBoardRefreshKey = todayKey;
        ActiveShortTermQuests = new List<ActiveShortTermQuest>();
        Save();
    }

    public void AcceptShortTermQuest(string questId)
    {
        RefreshNoticeBoardIfExpired();

        if (string.IsNullOrWhiteSpace(questId))
        {
            return;
        }

        if (ActiveShortTermQuests.Any(quest => quest.QuestId == questId))
        {
            return;
        }

        var quests = ActiveShortTermQuests.ToList();
        quests.Add(new ActiveShortTermQuest
        {
            QuestId = questId,
        });
        ActiveShortTermQuests = quests;
        Save();
    }

    public bool ClaimShortTermQuestReward(string questId)
    {
        RefreshNoticeBoardIfExpired();

        if (string.IsNullOrWhiteSpace(questId))
        {
            return false;
        }

        var definition = _shortTermQuestCatalog.GetById(questId);

        if (definition is null)
        {
            return false;
        }

        var claimed = false;
        var updatedQuests = ActiveShortTermQuests
            .Select(activeQuest => ClaimShortTermQuestReward(activeQuest, definition, ref claimed))
            .ToList();

        if (!claimed)
        {
            return false;
        }

        ActiveShortTermQuests = updatedQuests;
        Save();
        return true;
    }

    public ChurchViewModel BuildChurchViewModel(string? selectedQuestId = null)
    {
        RefreshNoticeBoardIfExpired();
        return new ChurchViewModel(
            Player,
            ActiveLongTermQuest,
            ClaimedLongTermQuestIds,
            UnlockedTitles,
            selectedQuestId);
    }

    public bool AcceptLongTermQuest(string questId)
    {
        RefreshNoticeBoardIfExpired();
        if (ActiveLongTermQuest is not null ||
            string.IsNullOrWhiteSpace(questId) ||
            ClaimedLongTermQuestIds.Contains(questId))
        {
            return false;
        }

        var definition = _longTermQuestCatalog.GetById(questId);
        if (definition is null)
        {
            return false;
        }

        ActiveLongTermQuest = new ActiveLongTermQuest
        {
            QuestId = definition.Id,
            StartedAtUtc = GetUtcNow(),
        };
        Save();
        return true;
    }

    public bool AbandonLongTermQuest()
    {
        RefreshNoticeBoardIfExpired();
        if (ActiveLongTermQuest is null)
        {
            return false;
        }

        ActiveLongTermQuest = null;
        Save();
        return true;
    }

    public bool ClaimLongTermQuestReward()
    {
        RefreshNoticeBoardIfExpired();
        var activeQuest = ActiveLongTermQuest;
        if (activeQuest is null || activeQuest.IsClaimed)
        {
            return false;
        }

        var definition = _longTermQuestCatalog.GetById(activeQuest.QuestId);
        if (definition is null ||
            activeQuest.Progress < definition.RequiredAmount ||
            ClaimedLongTermQuestIds.Contains(definition.Id))
        {
            return false;
        }

        Player.Apply(new RewardBundle(RewardSource.ChurchOath, definition.RewardGold, null));
        ClaimedLongTermQuestIds = ClaimedLongTermQuestIds
            .Append(definition.Id)
            .Distinct()
            .ToList();
        UnlockedTitles = UnlockedTitles
            .Append(definition.RewardTitle)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Distinct()
            .ToList();
        activeQuest.IsCompleted = true;
        activeQuest.IsClaimed = true;
        ActiveLongTermQuest = null;
        Save();
        return true;
    }

    public void UpdateDungeonRoute(IEnumerable<DungeonRouteSlot> dungeonRouteSlots)
    {
        if (!CanEditPlan)
        {
            return;
        }

        var route = _routeRules.NormalizeRoute(dungeonRouteSlots);

        if (!_routeRules.CanStartRoute(route))
        {
            return;
        }

        SelectedDungeonRoute = route;
        SelectedPlan = CreateLeveledPlan(SelectedDungeonRoute);
        if (!AdvanceTutorial(TutorialStepIds.PlanRoute, TutorialStepIds.ClearRoom))
        {
            AdvanceTutorial(TutorialStepIds.Welcome, TutorialStepIds.ClearRoom);
        }

        Save();
    }

    public DungeonRun? StartOrGetActiveRun()
    {
        if (SelectedPlan.Stages.Count == 0)
        {
            return null;
        }

        if (ActiveRun is null)
        {
            RefillStarterSupplies();
            ActiveRun = _dungeonRunService.Start(SelectedPlan, Player.CurrentHp);
            DailyRewardsClaimed = false;
            Save();
        }

        return ActiveRun;
    }

    public void ClearPendingSetSummary()
    {
        LastSetSummary = null;
    }

    public void RecordStageResult(RunSummary summary)
    {
        RefreshNoticeBoardIfExpired();
        var appliedSummary = EnsureTrainingExperience(summary);

        if (ActiveRun is not null)
        {
            appliedSummary = ApplyRouteFatigue(ActiveRun, appliedSummary);
            LastRunSummary = appliedSummary;
            var completedStage = ActiveRun.CurrentStage;
            LastSetSummary = _dungeonRunService.RecordStageResult(ActiveRun, appliedSummary);
            var levelsGained = Player.AddExperience(appliedSummary.ExperienceGained);
            if (levelsGained > 0)
            {
                var levelUpRewards = GrantLevelUpRewards(completedStage, levelsGained);
                var updatedRun = appliedSummary with
                {
                    LevelsGained = levelsGained,
                    LevelUpRewardCount = levelUpRewards,
                };
                LastRunSummary = updatedRun;
                LastSetSummary = LastSetSummary with { Run = updatedRun };
                appliedSummary = updatedRun;
            }

            if (appliedSummary.RemainingPlayerHp.HasValue)
            {
                Player.SetCurrentHp(appliedSummary.RemainingPlayerHp.Value);
            }

            ApplyRoomRecovery(appliedSummary);

            UpdateShortTermQuestProgress(completedStage, appliedSummary);
            UpdateLongTermQuestProgress(completedStage, appliedSummary);
            UpdateDungeonProgress(completedStage, appliedSummary);
            if (IsRoomCompleted(appliedSummary))
            {
                AdvanceTutorial(TutorialStepIds.ClearRoom, TutorialStepIds.ClaimRewards);
            }

            Save();
        }
        else
        {
            LastRunSummary = appliedSummary;
        }
    }

    private void UpdateDungeonProgress(TaskTemplate completedStage, RunSummary summary)
    {
        var entry = GetOrCreateDungeonProgress(completedStage.DungeonTypeId);
        entry.CompletedRooms++;
        if (summary.CombatResults?.Any(result => result.IsBoss && result.EnemyDefeated) == true)
        {
            entry.BossClears++;
        }

        DungeonProgressRules.AddExperience(entry, DungeonProgressRules.CalculateExperience(summary));
        if (SelectedDungeonRoute.Count > 0 && ActiveRun is null)
        {
            SelectedPlan = CreateLeveledPlan(SelectedDungeonRoute);
        }
    }

    private static RunSummary EnsureTrainingExperience(RunSummary summary)
    {
        if (summary.ExperienceGained > 0)
        {
            return summary;
        }

        return summary with
        {
            ExperienceGained = TrainingExperienceRules.Calculate(
                summary.CompletedSets,
                summary.TotalSets,
                summary.CombatResults),
        };
    }

    private static RunSummary ApplyRouteFatigue(DungeonRun run, RunSummary summary)
    {
        var completedStageNumber = run.CurrentStageIndex + 1;
        if (completedStageNumber <= RecommendedRouteStages)
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
            FatigueRewardPercent = FatiguedRewardPercent,
        };
    }

    private static int ScaleFatiguedReward(int value)
    {
        return value <= 0
            ? 0
            : Math.Max(1, (int)Math.Ceiling(value * FatiguedRewardMultiplier));
    }

    private int GrantLevelUpRewards(TaskTemplate completedStage, int levelsGained)
    {
        var rewardCount = 0;
        for (var level = 1; level <= levelsGained; level++)
        {
            var chest = new DungeonChest(
                $"{completedStage.Id}_level_up_{Player.Level}_{level}",
                "Boss",
                completedStage.Id,
                completedStage.DungeonTypeId,
                $"level_up_{Guid.NewGuid():N}",
                CompletionResult.Completed,
                Player.Level + level);
            Player.Apply(_lootRoller.RollDungeonChest(chest));
            rewardCount++;
        }

        return rewardCount;
    }

    private void ApplyRoomRecovery(RunSummary summary)
    {
        if (ActiveRun is null ||
            ActiveRun.IsComplete ||
            !IsRoomCompleted(summary))
        {
            return;
        }

        var healed = Player.HealPercent(RoomRecoveryPercent);
        if (healed > 0)
        {
            ActiveRun.RestorePlayerHp(Player.CurrentHp);
        }
    }

    private void RefillStarterSupplies()
    {
        _smallPotionCount = Math.Max(
            _smallPotionCount,
            Math.Min(SmallPotionCarryLimit, StarterSmallPotionCount));
    }

    private DungeonPlan CreateLeveledPlan(IEnumerable<DungeonRouteSlot> route)
    {
        var plan = _taskCatalog.CreateDungeonPlanFromRoute(route);
        if (plan.Stages.Count == 0)
        {
            return plan;
        }

        var stages = plan.Stages
            .Select(stage => stage with { DungeonLevel = GetDungeonLevel(stage.DungeonTypeId) })
            .ToArray();
        return new DungeonPlan(plan.Id, plan.DisplayName, stages);
    }

    private int GetDungeonLevel(string dungeonTypeId)
    {
        return _dungeonProgress.FirstOrDefault(entry => entry.DungeonTypeId == dungeonTypeId)?.Level ?? 1;
    }

    private DungeonProgressEntry GetOrCreateDungeonProgress(string dungeonTypeId)
    {
        var normalizedId = string.IsNullOrWhiteSpace(dungeonTypeId) ? "chest" : dungeonTypeId;
        var entry = _dungeonProgress.FirstOrDefault(progress => progress.DungeonTypeId == normalizedId);
        if (entry is not null)
        {
            return entry;
        }

        entry = new DungeonProgressEntry
        {
            DungeonTypeId = normalizedId,
            Level = 1,
            Experience = 0,
            ExperienceToNextLevel = DungeonProgressEntry.GetExperienceToNextLevel(1),
        };
        _dungeonProgress.Add(entry);
        return entry;
    }

    public void LeaveActiveRoom(int currentHp)
    {
        RefreshNoticeBoardIfExpired();
        var clampedHp = Math.Max(0, currentHp);
        Player.SetCurrentHp(clampedHp);
        ActiveRun?.RestorePlayerHp(clampedHp);
        LastSetSummary = null;
        Save();
    }

    public MoonlightFountainViewModel BuildMoonlightFountainViewModel()
    {
        RefreshNoticeBoardIfExpired();
        return new MoonlightFountainViewModel(
            Player.Level,
            Player.Experience,
            Player.ExperienceToNextLevel,
            Player.Gold,
            Player.CurrentHp,
            Player.MaxHp,
            _moonlightRecoveryUsed,
            !_moonlightRecoveryUsed && Player.CurrentHp < Player.MaxHp,
            Player.DailyBlessingId,
            ActiveRun is null && Player.DailyBlessingId == DailyBlessing.None,
            BuildDailyBlessings());
    }

    public HerbShopViewModel BuildHerbShopViewModel()
    {
        RefreshNoticeBoardIfExpired();
        var canHeal = Player.CurrentHp < Player.MaxHp;
        return new HerbShopViewModel(
            Player.Level,
            Player.Experience,
            Player.ExperienceToNextLevel,
            Player.Gold,
            Player.CurrentHp,
            Player.MaxHp,
            canHeal && Player.Gold >= BasicHealCost,
            canHeal && Player.Gold >= FullHealCost,
            Player.Gold >= SmallPotionCost && _herbShopPotionPurchasesToday < SmallPotionDailyPurchaseLimit,
            _smallPotionCount,
            _herbShopPotionPurchasesToday,
            SmallPotionDailyPurchaseLimit);
    }

    public RoomSupplyViewModel BuildRoomSupplyViewModel()
    {
        var usable = Math.Min(_smallPotionCount, SmallPotionCarryLimit);
        return new RoomSupplyViewModel(usable, SmallPotionCarryLimit, usable > 0);
    }

    public IdleRewardViewModel BuildIdleRewardViewModel(DateTime? nowUtc = null)
    {
        RefreshIdleRewards(nowUtc);
        return new IdleRewardViewModel(
            _unclaimedIdleGold,
            IdleRewardMaxUnclaimedGold,
            IdleRewardIntervalMinutes,
            _unclaimedIdleGold > 0,
            BuildIdleRewardStatusText());
    }

    public BodyProfileViewModel BuildBodyProfileViewModel(DateTime? localNow = null)
    {
        var goalId = FitnessGoal.Normalize(_profile.GoalId);
        var todayKey = GetBodyMetricDateKey(localNow);
        var todayWeight = _bodyMetrics.FirstOrDefault(metric => metric.DateKey == todayKey)?.WeightKg;
        var status = todayWeight.HasValue
            ? string.Format(
                CultureInfo.InvariantCulture,
                "今日體重 {0:0.0} kg / 目標 {1}",
                todayWeight.Value,
                FitnessGoal.GetLabel(goalId))
            : "今日尚未記錄體重";

        return new BodyProfileViewModel(
            _profile.HasCompletedOnboarding,
            _profile.HeightCm,
            goalId,
            FitnessGoal.GetLabel(goalId),
            FitnessGoal.GetAdvice(goalId),
            todayWeight,
            status);
    }

    public bool UpdatePlayerProfile(int heightCm, string goalId)
    {
        if (heightCm < PlayerProfile.MinHeightCm || heightCm > PlayerProfile.MaxHeightCm)
        {
            return false;
        }

        var now = GetUtcNow();
        if (_profile.CreatedAtUtc == default)
        {
            _profile.CreatedAtUtc = now;
        }

        _profile.HeightCm = heightCm;
        _profile.GoalId = FitnessGoal.Normalize(goalId);
        _profile.UpdatedAtUtc = now;
        _profile.HasCompletedOnboarding = true;
        Save();
        return true;
    }

    public bool RecordTodayWeight(double weightKg, DateTime? localNow = null)
    {
        if (weightKg < BodyMetricEntry.MinWeightKg || weightKg > BodyMetricEntry.MaxWeightKg)
        {
            return false;
        }

        var roundedWeight = Math.Round(weightKg, 1, MidpointRounding.AwayFromZero);
        var todayKey = GetBodyMetricDateKey(localNow);
        var now = GetUtcNow();
        _bodyMetrics.RemoveAll(metric => metric.DateKey == todayKey);
        _bodyMetrics.Add(new BodyMetricEntry
        {
            DateKey = todayKey,
            WeightKg = roundedWeight,
            RecordedAtUtc = now,
        });
        Save();
        return true;
    }

    public bool RefreshIdleRewards(DateTime? nowUtc = null, bool persist = true)
    {
        var now = NormalizeUtc(nowUtc ?? GetUtcNow());
        if (_idleLastCalculatedAtUtc == default)
        {
            _idleLastCalculatedAtUtc = now;
            if (persist)
            {
                Save();
            }

            return true;
        }

        if (now <= _idleLastCalculatedAtUtc)
        {
            return false;
        }

        if (_unclaimedIdleGold >= IdleRewardMaxUnclaimedGold)
        {
            _idleLastCalculatedAtUtc = now;
            if (persist)
            {
                Save();
            }

            return true;
        }

        var elapsedIntervals = (int)((now - _idleLastCalculatedAtUtc).TotalMinutes / IdleRewardIntervalMinutes);
        if (elapsedIntervals <= 0)
        {
            return false;
        }

        var claimableSpace = IdleRewardMaxUnclaimedGold - _unclaimedIdleGold;
        var earnedGold = Math.Min(elapsedIntervals * IdleRewardGoldPerInterval, claimableSpace);
        _unclaimedIdleGold += earnedGold;
        _idleLastCalculatedAtUtc = _unclaimedIdleGold >= IdleRewardMaxUnclaimedGold
            ? now
            : _idleLastCalculatedAtUtc.AddMinutes(elapsedIntervals * IdleRewardIntervalMinutes);
        if (persist)
        {
            Save();
        }

        return true;
    }

    public bool ClaimIdleRewards(DateTime? nowUtc = null)
    {
        RefreshIdleRewards(nowUtc);
        if (_unclaimedIdleGold <= 0)
        {
            return false;
        }

        Player.Apply(new RewardBundle(RewardSource.IdleReward, _unclaimedIdleGold, null));
        _unclaimedIdleGold = 0;
        _idleLastCalculatedAtUtc = NormalizeUtc(nowUtc ?? GetUtcNow());
        Save();
        return true;
    }

    public bool UseMoonlightRecovery()
    {
        RefreshNoticeBoardIfExpired();
        if (_moonlightRecoveryUsed || Player.CurrentHp >= Player.MaxHp)
        {
            return false;
        }

        var healed = Player.HealPercent(MoonlightRecoveryPercent);
        if (healed <= 0)
        {
            return false;
        }

        _moonlightRecoveryUsed = true;
        Save();
        return true;
    }

    public bool SelectDailyBlessing(string blessingId)
    {
        RefreshNoticeBoardIfExpired();
        if (ActiveRun is not null || Player.DailyBlessingId != DailyBlessing.None)
        {
            return false;
        }

        var changed = Player.SetDailyBlessing(blessingId);
        if (changed)
        {
            Save();
        }

        return changed;
    }

    public bool BuyBasicHeal()
    {
        RefreshNoticeBoardIfExpired();
        if (Player.CurrentHp >= Player.MaxHp || !Player.SpendGold(BasicHealCost))
        {
            return false;
        }

        Player.HealPercent(BasicHealPercent);
        Save();
        return true;
    }

    public bool BuyFullHeal()
    {
        RefreshNoticeBoardIfExpired();
        if (Player.CurrentHp >= Player.MaxHp || !Player.SpendGold(FullHealCost))
        {
            return false;
        }

        Player.HealToFull();
        Save();
        return true;
    }

    public bool BuyHerbShopPotion()
    {
        RefreshNoticeBoardIfExpired();
        if (_herbShopPotionPurchasesToday >= SmallPotionDailyPurchaseLimit || !Player.SpendGold(SmallPotionCost))
        {
            return false;
        }

        _smallPotionCount++;
        _herbShopPotionPurchasesToday++;
        Save();
        return true;
    }

    public SupplyUseResult UseSmallPotionInRoom(int currentRoomHp)
    {
        RefreshNoticeBoardIfExpired();
        if (_smallPotionCount <= 0)
        {
            return new SupplyUseResult(false, 0, currentRoomHp, Player.MaxHp, BuildRoomSupplyViewModel());
        }

        Player.SetCurrentHp(currentRoomHp);
        var healed = Player.HealPercent(SmallPotionHealPercent);
        if (healed <= 0)
        {
            return new SupplyUseResult(false, 0, Player.CurrentHp, Player.MaxHp, BuildRoomSupplyViewModel());
        }

        _smallPotionCount--;
        Save();
        return new SupplyUseResult(true, healed, Player.CurrentHp, Player.MaxHp, BuildRoomSupplyViewModel());
    }

    public DailyRunSummary? BuildDailySummary()
    {
        return ActiveRun is null ? null : new DailyRunSummary(ActiveRun);
    }

    public TavernEquipmentViewModel BuildTavernEquipmentViewModel(
        EquipmentInventoryFilter filter = EquipmentInventoryFilter.All,
        EquipmentInventorySort sort = EquipmentInventorySort.Rarity)
    {
        return new TavernEquipmentViewModel(Player, filter, sort);
    }

    public TutorialGuideViewModel BuildTutorialGuideViewModel()
    {
        if (_tutorial.IsCompleted || _tutorial.IsSkipped)
        {
            return TutorialGuideViewModel.Empty;
        }

        var readableGuide = BuildReadableTutorialGuide(_tutorial.StepId);
        if (readableGuide.IsVisible)
        {
            return readableGuide;
        }

        return _tutorial.StepId switch
        {
            TutorialStepIds.Welcome => new TutorialGuideViewModel(
                true,
                TutorialStepIds.Welcome,
                "村長 羅文",
                "月鎮的新委託",
                "你醒得正好。月鎮外的地城又開始發光了，我需要一位能把訓練變成冒險的人。先不用逞強，今天的目標很單純：完成第一條訓練路線，帶回寶箱。",
                "目標：完成一次地城路線，打開每日獎勵，回酒館整理裝備。",
                "接受委託",
                "跳過引導"),
            TutorialStepIds.PlanRoute => new TutorialGuideViewModel(
                true,
                TutorialStepIds.PlanRoute,
                "村長 羅文",
                "先規劃今天的路線",
                "先從熟悉的胸、肩、核心或手臂地城開始。四個房間是今日推薦長度；想多練也可以，但第五房以後會進入疲勞收益。",
                "目標：進入地城規劃，排出一條可以開始的路線。",
                "前往地城規劃",
                "跳過引導"),
            TutorialStepIds.ClearRoom => new TutorialGuideViewModel(
                true,
                TutorialStepIds.ClearRoom,
                "村長 羅文",
                "完成第一個房間",
                "每一組動作都會推進戰鬥。HP 到 0 不會讓今天白費；只要沒有中途放棄，完成房間就有保底寶箱。",
                "目標：完成至少一個房間，看看 Set Summary 的 EXP、金幣與寶箱。",
                "繼續訓練",
                "跳過引導"),
            TutorialStepIds.ClaimRewards => new TutorialGuideViewModel(
                true,
                TutorialStepIds.ClaimRewards,
                "村長 羅文",
                "把獎勵帶回來",
                "寶箱和金幣會先存在每日總結裡。記得按下 Open All，裝備和金幣才會真正進到角色身上。",
                "目標：前往每日總結，打開今天的獎勵。",
                "查看總結算",
                "跳過引導"),
            TutorialStepIds.VisitTavern => new TutorialGuideViewModel(
                true,
                TutorialStepIds.VisitTavern,
                "村長 羅文",
                "去酒館整理裝備",
                "冒險者不只靠肌肉，也靠整理背包。去酒館看看新裝備，普通裝可以賣掉，稀有裝可以先鎖起來。",
                "目標：進入酒館，確認裝備、出售普通裝或鎖定稀有裝。",
                "前往酒館",
                "結束引導"),
            _ => TutorialGuideViewModel.Empty,
        };
    }

    private static TutorialGuideViewModel BuildReadableTutorialGuide(string stepId)
    {
        return stepId switch
        {
            TutorialStepIds.Welcome => new TutorialGuideViewModel(
                true,
                TutorialStepIds.Welcome,
                "村長 露文",
                "歡迎來到 DungeonFit",
                "這座城鎮會把每天的訓練變成地城路線。選擇部位、完成房間、帶回金幣與裝備，角色就會慢慢成長。",
                "目標：先建立一條今日路線，開始第一個房間。",
                "開始規劃",
                "稍後再說"),
            TutorialStepIds.PlanRoute => new TutorialGuideViewModel(
                true,
                TutorialStepIds.PlanRoute,
                "村長 露文",
                "規劃今日路線",
                "每個地城代表一種訓練部位。加入至少四個房間後，就能開始今天的路線。路線越長，收益越高，但疲勞也會累積。",
                "目標：選滿路線並進入第一個房間。",
                "前往房間",
                "稍後再說"),
            TutorialStepIds.ClearRoom => new TutorialGuideViewModel(
                true,
                TutorialStepIds.ClearRoom,
                "村長 露文",
                "完成一個房間",
                "每組訓練都會推進戰鬥。HP 歸零前完成房間，就能把金幣、EXP 和可能出現的寶箱暫存到今日結算。",
                "目標：完成目前房間，查看 Set Summary。",
                "挑戰房間",
                "稍後再說"),
            TutorialStepIds.ClaimRewards => new TutorialGuideViewModel(
                true,
                TutorialStepIds.ClaimRewards,
                "村長 露文",
                "領取今日收益",
                "房間收益會先暫存在今日結算。按下 Open All 後，金幣與裝備才會正式加入角色狀態。",
                "目標：領取今日結算，再回城鎮整理裝備。",
                "領取獎勵",
                "稍後再說"),
            TutorialStepIds.VisitTavern => new TutorialGuideViewModel(
                true,
                TutorialStepIds.VisitTavern,
                "村長 露文",
                "整理裝備",
                "酒館可以查看裝備、切換穿戴、鎖定稀有裝備，或賣掉不需要的普通裝備。先整理背包，下一趟會更穩。",
                "目標：進入酒館查看你剛得到的裝備。",
                "前往酒館",
                "完成教學"),
            _ => TutorialGuideViewModel.Empty,
        };
    }

    public void AdvanceTutorialFromTown()
    {
        AdvanceTutorial(TutorialStepIds.Welcome, TutorialStepIds.PlanRoute);
        Save();
    }

    public void SkipTutorial()
    {
        _tutorial = TutorialProgress.Completed();
        _tutorial.IsSkipped = true;
        Save();
    }

    public void MarkTutorialTavernVisited()
    {
        if (AdvanceTutorial(TutorialStepIds.VisitTavern, TutorialStepIds.Completed))
        {
            _tutorial.IsCompleted = true;
            Save();
        }
    }

    private bool AdvanceTutorial(string expectedStepId, string nextStepId)
    {
        if (_tutorial.IsSkipped ||
            _tutorial.IsCompleted ||
            _tutorial.StepId != expectedStepId)
        {
            return false;
        }

        _tutorial.StepId = nextStepId;
        if (nextStepId == TutorialStepIds.Completed)
        {
            _tutorial.IsCompleted = true;
        }

        return true;
    }

    public BlacksmithViewModel BuildBlacksmithViewModel(string? selectedItemId = null)
    {
        return new BlacksmithViewModel(Player, selectedItemId);
    }

    public void ClaimDailyRewards()
    {
        if (ActiveRun is null || DailyRewardsClaimed)
        {
            return;
        }

        foreach (var bankedReward in ActiveRun.BankedRewards)
        {
            Player.Apply(bankedReward.Reward);
        }

        DailyRewardsClaimed = true;
        AdvanceTutorial(TutorialStepIds.ClaimRewards, TutorialStepIds.VisitTavern);
        Save();
    }

    public bool EquipItem(string itemId)
    {
        var changed = Player.Equip(itemId);

        if (changed)
        {
            Save();
        }

        return changed;
    }

    public bool UnequipItem(EquipmentSlot slot)
    {
        var changed = Player.Unequip(slot);

        if (changed)
        {
            Save();
        }

        return changed;
    }

    public bool SetEquipmentLocked(string itemId, bool isLocked)
    {
        var changed = Player.SetEquipmentLocked(itemId, isLocked);

        if (changed)
        {
            Save();
        }

        return changed;
    }

    public int LockRareEquipment()
    {
        var changedCount = 0;
        foreach (var item in Player.Inventory)
        {
            if (!IsRareOrBetter(item.Rarity) || item.IsLocked || Player.IsEquipped(item.Id))
            {
                continue;
            }

            if (Player.SetEquipmentLocked(item.Id, true))
            {
                changedCount++;
            }
        }

        if (changedCount > 0)
        {
            Save();
        }

        return changedCount;
    }

    public bool EnhanceEquipment(string itemId)
    {
        var item = Player.Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null)
        {
            return false;
        }

        var cost = BlacksmithRules.GetEnhancementCost(item.EnhancementLevel);
        var changed = Player.EnhanceEquipment(itemId, cost, BlacksmithRules.MaxEnhancementLevel);
        if (changed)
        {
            Save();
        }

        return changed;
    }

    public bool DismantleEnhancement(string itemId)
    {
        var item = Player.Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null)
        {
            return false;
        }

        var refund = BlacksmithRules.GetDismantleRefund(item.EnhancementLevel);
        var changed = Player.DismantleEnhancement(itemId, refund);
        if (changed)
        {
            Save();
        }

        return changed;
    }

    public bool ExtendEquipmentLevelRange(string itemId)
    {
        var item = Player.Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null)
        {
            return false;
        }

        var cost = BlacksmithRules.GetLevelExtensionCost(item.LevelExtension);
        var changed = Player.ExtendEquipmentLevelRange(itemId, cost, BlacksmithRules.MaxLevelExtension);
        if (changed)
        {
            Save();
        }

        return changed;
    }

    public bool SellEquipment(string itemId)
    {
        var changed = Player.SellEquipment(itemId);

        if (changed)
        {
            Save();
        }

        return changed;
    }

    public int SellUnlockedEquipment(IEnumerable<string> itemIds)
    {
        var soldCount = Player.SellUnlockedEquipment(itemIds);

        if (soldCount > 0)
        {
            Save();
        }

        return soldCount;
    }

    public int SellCommonEquipment()
    {
        var itemIds = Player.Inventory
            .Where(item => item.Rarity == "\u666e\u901a" && !item.IsLocked && !Player.IsEquipped(item.Id))
            .Select(item => item.Id)
            .ToArray();
        return SellUnlockedEquipment(itemIds);
    }

    public void CompleteDailyRun()
    {
        ActiveRun = null;
        LastSetSummary = null;
        DailyRewardsClaimed = false;
        SelectedDungeonRoute = new List<DungeonRouteSlot>();
        SelectedPlan = DungeonPlan.Empty;
        Save();
    }

    private void Restore(SaveGameState? state)
    {
        if (state is null)
        {
            return;
        }

        var saveAfterNormalize = NormalizeSaveState(state);
        Player.Load(
            state.Gold,
            state.Inventory!,
            state.EquipmentLoadout,
            state.Level,
            state.Experience,
            state.ExperienceToNextLevel,
            state.CurrentHp,
            state.DailyBlessingId);
        _dailyStateKey = string.IsNullOrWhiteSpace(state.DailyStateKey)
            ? GetTodayRefreshKey()
            : state.DailyStateKey;
        _moonlightRecoveryUsed = state.MoonlightRecoveryUsed;
        _smallPotionCount = Math.Max(0, state.SmallPotionCount);
        _herbShopPotionPurchasesToday = Math.Max(0, state.HerbShopPotionPurchasesToday);
        _idleLastCalculatedAtUtc = NormalizeUtc(state.IdleLastCalculatedAtUtc ?? GetUtcNow());
        _unclaimedIdleGold = Math.Clamp(state.UnclaimedIdleGold, 0, IdleRewardMaxUnclaimedGold);
        _profile = state.Profile!;
        _tutorial = state.Tutorial!;
        _bodyMetrics.Clear();
        _bodyMetrics.AddRange(state.BodyMetrics!);
        _dungeonProgress.Clear();
        _dungeonProgress.AddRange(state.DungeonProgress!);
        var idleChanged = RefreshIdleRewards(persist: false);
        var hasActiveRun = state.HasActiveRun || state.ActiveStageResults!.Count > 0;
        SelectedDungeonRoute = hasActiveRun
            ? _routeRules.NormalizeRoute(state.SelectedDungeonRoute!)
            : new List<DungeonRouteSlot>();
        SelectedPlan = SelectedDungeonRoute.Count == 0
            ? DungeonPlan.Empty
            : CreateLeveledPlan(SelectedDungeonRoute);
        LastRunSummary = state.LastRunSummary;
        DailyRewardsClaimed = state.DailyRewardsClaimed;
        ActiveShortTermQuests = RestoreShortTermQuests(state);
        ClaimedLongTermQuestIds = state.ClaimedLongTermQuestIds!
            .Where(id => _longTermQuestCatalog.GetById(id) is not null)
            .Distinct()
            .ToList();
        UnlockedTitles = state.UnlockedTitles!
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Distinct()
            .ToList();
        ActiveLongTermQuest = RestoreLongTermQuest(state.ActiveLongTermQuest, ClaimedLongTermQuestIds);
        var shouldSaveAfterRestore = RefreshNoticeBoardFromSave(state);

        if (!hasActiveRun || SelectedPlan.Stages.Count == 0)
        {
            if (state.SelectedDungeonRoute!.Count > 0 || shouldSaveAfterRestore || saveAfterNormalize || idleChanged)
            {
                Save();
            }

            return;
        }

        ActiveRun = _dungeonRunService.Start(SelectedPlan, Player.MaxHp);
        foreach (var savedStage in state.ActiveStageResults!)
        {
            if (savedStage.Summary is null)
            {
                continue;
            }

            ActiveRun.RestoreStageResult(savedStage.Summary, savedStage.BankedRewards ?? Enumerable.Empty<BankedReward>());
        }

        ActiveRun.RestorePlayerHp(state.ActiveRunCurrentHp ?? Player.MaxHp);

        if (shouldSaveAfterRestore || saveAfterNormalize || idleChanged)
        {
            Save();
        }
    }

    public static bool NormalizeSaveState(SaveGameState state)
    {
        var changed = false;
        var originalVersion = state.Version;

        if (state.Version != SaveGameState.CurrentVersion)
        {
            state.Version = SaveGameState.CurrentVersion;
            changed = true;
        }

        if (state.Level <= 0)
        {
            state.Level = 1;
            changed = true;
        }

        if (state.Experience < 0)
        {
            state.Experience = 0;
            changed = true;
        }

        if (state.Gold < 0)
        {
            state.Gold = 0;
            changed = true;
        }

        var expectedExperienceToNext = PlayerState.GetExperienceToNextLevel(state.Level);
        if (state.ExperienceToNextLevel != expectedExperienceToNext)
        {
            state.ExperienceToNextLevel = expectedExperienceToNext;
            changed = true;
        }

        while (state.Experience >= state.ExperienceToNextLevel)
        {
            state.Experience -= state.ExperienceToNextLevel;
            state.Level++;
            state.ExperienceToNextLevel = PlayerState.GetExperienceToNextLevel(state.Level);
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(state.DailyStateKey))
        {
            state.DailyStateKey = GetTodayRefreshKey();
            changed = true;
        }

        if (!DailyBlessing.IsValid(state.DailyBlessingId) && !string.IsNullOrEmpty(state.DailyBlessingId))
        {
            state.DailyBlessingId = DailyBlessing.None;
            changed = true;
        }

        if (state.SmallPotionCount < 0)
        {
            state.SmallPotionCount = 0;
            changed = true;
        }

        if (state.HerbShopPotionPurchasesToday < 0)
        {
            state.HerbShopPotionPurchasesToday = 0;
            changed = true;
        }

        if (state.UnclaimedIdleGold < 0)
        {
            state.UnclaimedIdleGold = 0;
            changed = true;
        }

        if (state.UnclaimedIdleGold > IdleRewardMaxUnclaimedGold)
        {
            state.UnclaimedIdleGold = IdleRewardMaxUnclaimedGold;
            changed = true;
        }

        if (!state.IdleLastCalculatedAtUtc.HasValue)
        {
            state.IdleLastCalculatedAtUtc = GetUtcNow();
            changed = true;
        }

        if (state.Inventory is null)
        {
            state.Inventory = new List<EquipmentItem>();
            changed = true;
        }

        if (state.EquipmentLoadout is null)
        {
            state.EquipmentLoadout = new EquipmentLoadout();
            changed = true;
        }

        if (state.SelectedDungeonRoute is null)
        {
            state.SelectedDungeonRoute = new List<DungeonRouteSlot>();
            changed = true;
        }

        if (state.ActiveStageResults is null)
        {
            state.ActiveStageResults = new List<SavedStageResult>();
            changed = true;
        }

        if (state.ActiveShortTermQuests is null)
        {
            state.ActiveShortTermQuests = new List<ActiveShortTermQuest>();
            changed = true;
        }

        if (state.ClaimedLongTermQuestIds is null)
        {
            state.ClaimedLongTermQuestIds = new List<string>();
            changed = true;
        }

        if (state.UnlockedTitles is null)
        {
            state.UnlockedTitles = new List<string>();
            changed = true;
        }

        changed = NormalizeTutorialState(state, originalVersion) || changed;
        changed = NormalizeBodyProfileState(state) || changed;
        changed = NormalizeDungeonProgressState(state) || changed;
        changed = NormalizeInventory(state.Inventory) || changed;
        changed = NormalizeLoadout(state.Inventory, state.EquipmentLoadout) || changed;
        changed = NormalizeLongTermQuestState(state) || changed;

        var normalizedPlayer = new PlayerState();
        normalizedPlayer.Load(
            state.Gold,
            state.Inventory,
            state.EquipmentLoadout,
            state.Level,
            state.Experience,
            state.ExperienceToNextLevel,
            null,
            state.DailyBlessingId);
        var normalizedCurrentHp = state.CurrentHp.HasValue
            ? Math.Clamp(state.CurrentHp.Value, 0, normalizedPlayer.MaxHp)
            : normalizedPlayer.MaxHp;
        if (state.CurrentHp != normalizedCurrentHp)
        {
            state.CurrentHp = normalizedCurrentHp;
            changed = true;
        }

        if (state.ActiveRunCurrentHp.HasValue)
        {
            var normalizedRunHp = Math.Clamp(state.ActiveRunCurrentHp.Value, 0, normalizedPlayer.MaxHp);
            if (state.ActiveRunCurrentHp != normalizedRunHp)
            {
                state.ActiveRunCurrentHp = normalizedRunHp;
                changed = true;
            }
        }

        if (!state.HasActiveRun && state.ActiveStageResults.Count == 0 && state.ActiveRunCurrentHp.HasValue)
        {
            state.ActiveRunCurrentHp = null;
            changed = true;
        }

        foreach (var stage in state.ActiveStageResults)
        {
            if (stage.BankedRewards is null)
            {
                stage.BankedRewards = new List<BankedReward>();
                changed = true;
            }
        }

        return changed;
    }

    private static bool NormalizeInventory(List<EquipmentItem> inventory)
    {
        var changed = false;
        var seenIds = new HashSet<string>();
        var equipmentCatalog = new EquipmentCatalog();

        for (var index = inventory.Count - 1; index >= 0; index--)
        {
            var item = inventory[index];
            if (item is null)
            {
                inventory.RemoveAt(index);
                changed = true;
            }
        }

        for (var index = 0; index < inventory.Count; index++)
        {
            var item = inventory[index];
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                item.Id = CreateMigratedEquipmentId(item, index);
                changed = true;
            }

            if (!seenIds.Add(item.Id))
            {
                item.Id = CreateMigratedEquipmentId(item, index);
                changed = true;
                seenIds.Add(item.Id);
            }

            var normalizedEnhancementLevel = BlacksmithRules.ClampEnhancementLevel(item.EnhancementLevel);
            if (item.EnhancementLevel != normalizedEnhancementLevel)
            {
                item.EnhancementLevel = normalizedEnhancementLevel;
                changed = true;
            }

            var normalizedLevelExtension = BlacksmithRules.ClampLevelExtension(item.LevelExtension);
            if (item.LevelExtension != normalizedLevelExtension)
            {
                item.LevelExtension = normalizedLevelExtension;
                changed = true;
            }

            if (item.RecommendedLevelMin <= 0)
            {
                item.RecommendedLevelMin = 1;
                changed = true;
            }

            if (item.RecommendedLevelMax < item.RecommendedLevelMin)
            {
                item.RecommendedLevelMax = item.RecommendedLevelMin + 4;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(item.IconPath))
            {
                item.IconPath = ResolveMigratedIconPath(equipmentCatalog, item);
                changed = true;
            }
        }

        return changed;
    }

    private static string ResolveMigratedIconPath(EquipmentCatalog equipmentCatalog, EquipmentItem item)
    {
        var definition = equipmentCatalog.GetById(item.DefinitionId);
        if (definition.Id == item.DefinitionId)
        {
            return definition.IconPath;
        }

        return item.Slot switch
        {
            EquipmentSlot.Armor => "res://Assets/Art/Items/Armor/guard_plate.png",
            EquipmentSlot.Accessory => "res://Assets/Art/Items/Accessories/oath_charm.png",
            _ => "res://Assets/Art/Items/Weapons/moon_blade.png",
        };
    }

    private static bool NormalizeTutorialState(SaveGameState state, int originalVersion)
    {
        if (state.Tutorial is null)
        {
            state.Tutorial = ShouldCompleteTutorialForMigratedSave(state, originalVersion)
                ? TutorialProgress.Completed()
                : new TutorialProgress();
            return true;
        }

        var changed = false;
        if (!TutorialStepIds.IsValid(state.Tutorial.StepId))
        {
            state.Tutorial.StepId = TutorialStepIds.Welcome;
            changed = true;
        }

        if (state.Tutorial.IsCompleted || state.Tutorial.IsSkipped)
        {
            if (state.Tutorial.StepId != TutorialStepIds.Completed)
            {
                state.Tutorial.StepId = TutorialStepIds.Completed;
                changed = true;
            }

            state.Tutorial.IsCompleted = true;
            return changed;
        }

        return changed;
    }

    private static bool ShouldCompleteTutorialForMigratedSave(SaveGameState state, int originalVersion)
    {
        return originalVersion < SaveGameState.CurrentVersion &&
            (state.Level > 1 ||
                state.Inventory?.Count > 0 ||
                state.ActiveStageResults?.Count > 0 ||
                state.LastRunSummary is not null);
    }

    private static bool NormalizeBodyProfileState(SaveGameState state)
    {
        var changed = false;

        if (state.Profile is null)
        {
            state.Profile = CreateIncompleteProfile();
            changed = true;
        }

        var profile = state.Profile;
        var normalizedGoal = FitnessGoal.Normalize(profile.GoalId);
        if (profile.GoalId != normalizedGoal)
        {
            profile.GoalId = normalizedGoal;
            changed = true;
        }

        if (profile.HasCompletedOnboarding &&
            (profile.HeightCm < PlayerProfile.MinHeightCm || profile.HeightCm > PlayerProfile.MaxHeightCm))
        {
            profile.HeightCm = 0;
            profile.HasCompletedOnboarding = false;
            changed = true;
        }

        if (!profile.HasCompletedOnboarding && profile.HeightCm != 0)
        {
            profile.HeightCm = 0;
            changed = true;
        }

        if (profile.CreatedAtUtc != default)
        {
            var normalizedCreated = NormalizeUtc(profile.CreatedAtUtc);
            if (normalizedCreated != profile.CreatedAtUtc)
            {
                profile.CreatedAtUtc = normalizedCreated;
                changed = true;
            }
        }

        if (profile.UpdatedAtUtc != default)
        {
            var normalizedUpdated = NormalizeUtc(profile.UpdatedAtUtc);
            if (normalizedUpdated != profile.UpdatedAtUtc)
            {
                profile.UpdatedAtUtc = normalizedUpdated;
                changed = true;
            }
        }

        if (state.BodyMetrics is null)
        {
            state.BodyMetrics = new List<BodyMetricEntry>();
            return true;
        }

        var normalizedMetrics = state.BodyMetrics
            .Where(IsValidBodyMetric)
            .Select(metric => new BodyMetricEntry
            {
                DateKey = metric.DateKey,
                WeightKg = Math.Round(metric.WeightKg, 1, MidpointRounding.AwayFromZero),
                RecordedAtUtc = NormalizeUtc(metric.RecordedAtUtc == default ? GetUtcNow() : metric.RecordedAtUtc),
            })
            .GroupBy(metric => metric.DateKey)
            .Select(group => group.OrderByDescending(metric => metric.RecordedAtUtc).First())
            .OrderBy(metric => metric.DateKey, StringComparer.Ordinal)
            .ToList();

        if (normalizedMetrics.Count != state.BodyMetrics.Count ||
            normalizedMetrics.Zip(state.BodyMetrics).Any(pair =>
                pair.First.DateKey != pair.Second.DateKey ||
                Math.Abs(pair.First.WeightKg - pair.Second.WeightKg) > 0.001 ||
                pair.First.RecordedAtUtc != pair.Second.RecordedAtUtc))
        {
            state.BodyMetrics = normalizedMetrics;
            changed = true;
        }

        return changed;
    }

    private static bool IsValidBodyMetric(BodyMetricEntry? metric)
    {
        return metric is not null &&
            !string.IsNullOrWhiteSpace(metric.DateKey) &&
            metric.DateKey.Length == 10 &&
            metric.WeightKg >= BodyMetricEntry.MinWeightKg &&
            metric.WeightKg <= BodyMetricEntry.MaxWeightKg;
    }

    private static bool IsRareOrBetter(string rarity)
    {
        return rarity is "\u7a00\u6709" or "\u53f2\u8a69";
    }

    private static bool NormalizeDungeonProgressState(SaveGameState state)
    {
        if (state.DungeonProgress is null)
        {
            state.DungeonProgress = new List<DungeonProgressEntry>();
            return true;
        }

        var normalized = state.DungeonProgress
            .Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.DungeonTypeId))
            .GroupBy(entry => entry.DungeonTypeId)
            .Select(group =>
            {
                var source = group.OrderByDescending(entry => entry.Level).First();
                var level = Math.Max(1, source.Level);
                var experienceToNext = source.ExperienceToNextLevel <= 0
                    ? DungeonProgressEntry.GetExperienceToNextLevel(level)
                    : source.ExperienceToNextLevel;
                return new DungeonProgressEntry
                {
                    DungeonTypeId = source.DungeonTypeId,
                    Level = level,
                    Experience = Math.Max(0, source.Experience),
                    ExperienceToNextLevel = experienceToNext,
                    CompletedRooms = Math.Max(0, source.CompletedRooms),
                    BossClears = Math.Max(0, source.BossClears),
                };
            })
            .OrderBy(entry => entry.DungeonTypeId, StringComparer.Ordinal)
            .ToList();

        if (normalized.Count != state.DungeonProgress.Count ||
            normalized.Zip(state.DungeonProgress).Any(pair =>
                pair.First.DungeonTypeId != pair.Second.DungeonTypeId ||
                pair.First.Level != pair.Second.Level ||
                pair.First.Experience != pair.Second.Experience ||
                pair.First.ExperienceToNextLevel != pair.Second.ExperienceToNextLevel ||
                pair.First.CompletedRooms != pair.Second.CompletedRooms ||
                pair.First.BossClears != pair.Second.BossClears))
        {
            state.DungeonProgress = normalized;
            return true;
        }

        return false;
    }

    private static bool NormalizeLoadout(List<EquipmentItem> inventory, EquipmentLoadout loadout)
    {
        var changed = false;
        var ids = inventory.Select(item => item.Id).ToHashSet();

        if (loadout.WeaponId is not null && !ids.Contains(loadout.WeaponId))
        {
            loadout.WeaponId = null;
            changed = true;
        }

        if (loadout.ArmorId is not null && !ids.Contains(loadout.ArmorId))
        {
            loadout.ArmorId = null;
            changed = true;
        }

        if (loadout.AccessoryId is not null && !ids.Contains(loadout.AccessoryId))
        {
            loadout.AccessoryId = null;
            changed = true;
        }

        return changed;
    }

    private static string CreateMigratedEquipmentId(EquipmentItem item, int index)
    {
        var baseId = string.IsNullOrWhiteSpace(item.DefinitionId)
            ? "equipment"
            : item.DefinitionId;
        return $"migrated_{baseId}_{index}_{Guid.NewGuid():N}";
    }

    private void Save()
    {
        if (!_persistenceEnabled)
        {
            return;
        }

        var state = new SaveGameState
        {
            Level = Player.Level,
            Experience = Player.Experience,
            ExperienceToNextLevel = Player.ExperienceToNextLevel,
            Gold = Player.Gold,
            CurrentHp = Player.CurrentHp,
            Profile = CloneProfile(_profile),
            BodyMetrics = _bodyMetrics
                .Select(CloneBodyMetric)
                .ToList(),
            DungeonProgress = _dungeonProgress
                .Select(CloneDungeonProgress)
                .ToList(),
            Tutorial = CloneTutorial(_tutorial),
            DailyStateKey = _dailyStateKey,
            MoonlightRecoveryUsed = _moonlightRecoveryUsed,
            DailyBlessingId = Player.DailyBlessingId,
            SmallPotionCount = _smallPotionCount,
            HerbShopPotionPurchasesToday = _herbShopPotionPurchasesToday,
            IdleLastCalculatedAtUtc = _idleLastCalculatedAtUtc,
            UnclaimedIdleGold = _unclaimedIdleGold,
            Inventory = new List<EquipmentItem>(Player.Inventory),
            EquipmentLoadout = new EquipmentLoadout
            {
                WeaponId = Player.Loadout.WeaponId,
                ArmorId = Player.Loadout.ArmorId,
                AccessoryId = Player.Loadout.AccessoryId,
            },
            SelectedDungeonRoute = new List<DungeonRouteSlot>(SelectedDungeonRoute),
            HasActiveRun = ActiveRun is not null,
            ActiveRunCurrentHp = ActiveRun?.CurrentPlayerHp ?? 0,
            DailyRewardsClaimed = DailyRewardsClaimed,
            LastRunSummary = LastRunSummary,
            NoticeBoardRefreshKey = _noticeBoardRefreshKey,
            ActiveShortTermQuests = ActiveShortTermQuests.ToList(),
            ActiveLongTermQuest = ActiveLongTermQuest,
            ClaimedLongTermQuestIds = ClaimedLongTermQuestIds.ToList(),
            UnlockedTitles = UnlockedTitles.ToList(),
        };

        if (ActiveRun is not null)
        {
            var bankedIndex = 0;
            foreach (var summary in ActiveRun.StageSummaries)
            {
                var bankedRewards = new List<BankedReward>();
                for (var count = 0; count < summary.CompletedSets && bankedIndex < ActiveRun.BankedRewards.Count; count++)
                {
                    bankedRewards.Add(ActiveRun.BankedRewards[bankedIndex]);
                    bankedIndex++;
                }

                state.ActiveStageResults!.Add(new SavedStageResult
                {
                    Summary = summary,
                    BankedRewards = bankedRewards,
                });
            }
        }

        _saveService.Save(state);
    }

    private static IReadOnlyList<ActiveShortTermQuest> RestoreShortTermQuests(SaveGameState state)
    {
        var quests = state.ActiveShortTermQuests!
            .Where(quest => !string.IsNullOrWhiteSpace(quest.QuestId))
            .GroupBy(quest => quest.QuestId)
            .Select(group => group.First())
            .ToList();

        if (state.ActiveShortTermQuest is not null &&
            !string.IsNullOrWhiteSpace(state.ActiveShortTermQuest.QuestId) &&
            quests.All(quest => quest.QuestId != state.ActiveShortTermQuest.QuestId))
        {
            quests.Add(state.ActiveShortTermQuest);
        }

        return quests;
    }

    private ActiveLongTermQuest? RestoreLongTermQuest(
        ActiveLongTermQuest? activeQuest,
        IReadOnlyList<string> claimedQuestIds)
    {
        if (activeQuest is null ||
            string.IsNullOrWhiteSpace(activeQuest.QuestId) ||
            claimedQuestIds.Contains(activeQuest.QuestId))
        {
            return null;
        }

        var definition = _longTermQuestCatalog.GetById(activeQuest.QuestId);
        if (definition is null)
        {
            return null;
        }

        activeQuest.Progress = Math.Clamp(activeQuest.Progress, 0, definition.RequiredAmount);
        activeQuest.IsCompleted = activeQuest.Progress >= definition.RequiredAmount;
        return activeQuest;
    }

    private static bool NormalizeLongTermQuestState(SaveGameState state)
    {
        var changed = false;
        var catalog = new LongTermQuestCatalog();
        var claimed = state.ClaimedLongTermQuestIds!
            .Where(id => catalog.GetById(id) is not null)
            .Distinct()
            .ToList();
        if (claimed.Count != state.ClaimedLongTermQuestIds!.Count)
        {
            state.ClaimedLongTermQuestIds = claimed;
            changed = true;
        }

        var titles = state.UnlockedTitles!
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Distinct()
            .ToList();
        if (titles.Count != state.UnlockedTitles!.Count)
        {
            state.UnlockedTitles = titles;
            changed = true;
        }

        var activeQuest = state.ActiveLongTermQuest;
        if (activeQuest is null)
        {
            return changed;
        }

        var definition = catalog.GetById(activeQuest.QuestId);
        if (definition is null || claimed.Contains(activeQuest.QuestId))
        {
            state.ActiveLongTermQuest = null;
            return true;
        }

        var normalizedProgress = Math.Clamp(activeQuest.Progress, 0, definition.RequiredAmount);
        if (normalizedProgress != activeQuest.Progress)
        {
            activeQuest.Progress = normalizedProgress;
            changed = true;
        }

        var isCompleted = activeQuest.Progress >= definition.RequiredAmount;
        if (activeQuest.IsCompleted != isCompleted)
        {
            activeQuest.IsCompleted = isCompleted;
            changed = true;
        }

        if (activeQuest.IsClaimed)
        {
            activeQuest.IsClaimed = false;
            changed = true;
        }

        if (activeQuest.StartedAtUtc == default)
        {
            activeQuest.StartedAtUtc = GetUtcNow();
            changed = true;
        }

        return changed;
    }

    private bool RefreshNoticeBoardFromSave(SaveGameState state)
    {
        var todayKey = GetTodayRefreshKey();

        if (string.IsNullOrWhiteSpace(state.NoticeBoardRefreshKey))
        {
            _noticeBoardRefreshKey = todayKey;
            _noticeBoardRefreshKeyNeedsSave = true;
            return true;
        }

        _noticeBoardRefreshKey = state.NoticeBoardRefreshKey;

        if (_noticeBoardRefreshKey == todayKey)
        {
            return false;
        }

        _noticeBoardRefreshKey = todayKey;
        ActiveShortTermQuests = new List<ActiveShortTermQuest>();
        return true;
    }

    private void RefreshDailyStateIfExpired(string todayKey)
    {
        if (_dailyStateKey == todayKey)
        {
            return;
        }

        _dailyStateKey = todayKey;
        _moonlightRecoveryUsed = false;
        _herbShopPotionPurchasesToday = 0;
        Player.ClearDailyBlessing();
        Save();
    }

    private IReadOnlyList<DailyBlessingOptionViewModel> BuildDailyBlessings()
    {
        return new[]
        {
            BuildDailyBlessingOption(DailyBlessing.MoonGuard, "月光庇護", "今日最大 HP +10%"),
            BuildDailyBlessingOption(DailyBlessing.BladeMoon, "鋒刃月影", "今日攻擊 +5%"),
            BuildDailyBlessingOption(DailyBlessing.StarlightGold, "拾荒星光", "今日地城 Gold +10%"),
        };
    }

    private DailyBlessingOptionViewModel BuildDailyBlessingOption(string id, string name, string description)
    {
        var selected = Player.DailyBlessingId == id;
        return new DailyBlessingOptionViewModel(
            id,
            name,
            description,
            selected,
            !selected && (ActiveRun is not null || Player.DailyBlessingId != DailyBlessing.None));
    }

    private static string GetTodayRefreshKey()
    {
        return DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string GetBodyMetricDateKey(DateTime? localNow = null)
    {
        return (localNow ?? DateTime.Now).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static PlayerProfile CreateIncompleteProfile()
    {
        return new PlayerProfile
        {
            GoalId = FitnessGoal.GeneralHealth,
            HasCompletedOnboarding = false,
        };
    }

    private static PlayerProfile CloneProfile(PlayerProfile profile)
    {
        return new PlayerProfile
        {
            HeightCm = profile.HeightCm,
            GoalId = FitnessGoal.Normalize(profile.GoalId),
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,
            HasCompletedOnboarding = profile.HasCompletedOnboarding,
        };
    }

    private static BodyMetricEntry CloneBodyMetric(BodyMetricEntry metric)
    {
        return new BodyMetricEntry
        {
            DateKey = metric.DateKey,
            WeightKg = metric.WeightKg,
            RecordedAtUtc = metric.RecordedAtUtc,
        };
    }

    private static TutorialProgress CloneTutorial(TutorialProgress tutorial)
    {
        return new TutorialProgress
        {
            StepId = tutorial.StepId,
            IsSkipped = tutorial.IsSkipped,
            IsCompleted = tutorial.IsCompleted,
        };
    }

    private static DungeonProgressEntry CloneDungeonProgress(DungeonProgressEntry entry)
    {
        return new DungeonProgressEntry
        {
            DungeonTypeId = entry.DungeonTypeId,
            Level = entry.Level,
            Experience = entry.Experience,
            ExperienceToNextLevel = entry.ExperienceToNextLevel,
            CompletedRooms = entry.CompletedRooms,
            BossClears = entry.BossClears,
        };
    }

    private string BuildIdleRewardStatusText()
    {
        return _unclaimedIdleGold <= 0
            ? $"戶外探索中。每 {IdleRewardIntervalMinutes} 分鐘累積 1 金幣。"
            : $"可領取 {_unclaimedIdleGold} / {IdleRewardMaxUnclaimedGold} 金幣。";
    }

    private static DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    private void UpdateShortTermQuestProgress(TaskTemplate completedStage, RunSummary summary)
    {
        if (!IsStageFullyCompleted(summary) || ActiveShortTermQuests.Count == 0)
        {
            return;
        }

        var updatedQuests = ActiveShortTermQuests
            .Select(activeQuest => UpdateShortTermQuestProgress(activeQuest, completedStage))
            .ToList();
        ActiveShortTermQuests = updatedQuests;
    }

    private ActiveShortTermQuest UpdateShortTermQuestProgress(
        ActiveShortTermQuest activeQuest,
        TaskTemplate completedStage)
    {
        var definition = _shortTermQuestCatalog.GetById(activeQuest.QuestId);

        if (definition is null ||
            definition.TargetDungeonTypeId != completedStage.DungeonTypeId ||
            activeQuest.Progress >= definition.RequiredAmount)
        {
            return activeQuest;
        }

        return new ActiveShortTermQuest
        {
            QuestId = activeQuest.QuestId,
            Progress = System.Math.Min(activeQuest.Progress + 1, definition.RequiredAmount),
            IsClaimed = activeQuest.IsClaimed,
        };
    }

    private void UpdateLongTermQuestProgress(TaskTemplate completedStage, RunSummary summary)
    {
        var activeQuest = ActiveLongTermQuest;
        if (activeQuest is null || activeQuest.IsClaimed)
        {
            return;
        }

        var definition = _longTermQuestCatalog.GetById(activeQuest.QuestId);
        if (definition is null || activeQuest.Progress >= definition.RequiredAmount)
        {
            return;
        }

        var progressGain = CalculateLongTermQuestProgressGain(definition, completedStage, summary);
        if (progressGain <= 0)
        {
            return;
        }

        activeQuest.Progress = Math.Min(definition.RequiredAmount, activeQuest.Progress + progressGain);
        activeQuest.IsCompleted = activeQuest.Progress >= definition.RequiredAmount;
    }

    private static int CalculateLongTermQuestProgressGain(
        LongTermQuestDefinition definition,
        TaskTemplate completedStage,
        RunSummary summary)
    {
        return definition.ObjectiveType switch
        {
            LongTermQuestObjectiveType.CompleteRooms => IsStageFullyCompleted(summary) ? 1 : 0,
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms =>
                IsStageFullyCompleted(summary) && LongTermQuestCatalog.MatchesTarget(definition, completedStage.DungeonTypeId) ? 1 : 0,
            LongTermQuestObjectiveType.DefeatBosses =>
                summary.CombatResults?.Count(result => result.IsBoss && result.EnemyDefeated) ?? 0,
            LongTermQuestObjectiveType.EarnGold => Math.Max(0, summary.Reward.Gold),
            _ => 0,
        };
    }

    private static bool IsStageFullyCompleted(RunSummary summary)
    {
        return summary.TotalSets > 0 &&
            summary.CompletedSets >= summary.TotalSets &&
            Enumerable.Range(1, summary.TotalSets)
                .All(setNumber => summary.GetSetResult(setNumber) == CompletionResult.Completed);
    }

    private static bool IsRoomCompleted(RunSummary summary)
    {
        return summary.TotalSets > 0 &&
            summary.CompletedSets >= summary.TotalSets &&
            Enumerable.Range(1, summary.TotalSets)
                .All(setNumber => summary.GetSetResult(setNumber) != CompletionResult.Skipped);
    }

    private ActiveShortTermQuest ClaimShortTermQuestReward(
        ActiveShortTermQuest activeQuest,
        ShortTermQuestDefinition definition,
        ref bool claimed)
    {
        if (activeQuest.QuestId != definition.Id ||
            activeQuest.IsClaimed ||
            activeQuest.Progress < definition.RequiredAmount)
        {
            return activeQuest;
        }

        Player.Apply(new RewardBundle(RewardSource.NoticeBoardQuest, definition.RewardGold, null));
        claimed = true;

        return new ActiveShortTermQuest
        {
            QuestId = activeQuest.QuestId,
            Progress = activeQuest.Progress,
            IsClaimed = true,
        };
    }
}
