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
    private const double BasicHealPercent = 0.4;
    private const double SmallPotionHealPercent = 0.3;
    private const int BasicHealCost = 80;
    private const int FullHealCost = 180;
    private const int SmallPotionCost = 50;
    private const int SmallPotionDailyPurchaseLimit = 3;
    private const int SmallPotionCarryLimit = 3;
    private const int IdleRewardIntervalMinutes = 10;
    private const int IdleRewardGoldPerInterval = 1;
    private const int IdleRewardMaxUnclaimedGold = 72;

    private readonly TaskCatalog _taskCatalog = new();
    private readonly DungeonRunService _dungeonRunService = new();
    private readonly DungeonRouteRules _routeRules = new();
    private readonly ShortTermQuestCatalog _shortTermQuestCatalog = new();
    private readonly LongTermQuestCatalog _longTermQuestCatalog = new();
    private readonly SaveService _saveService = new();
    private readonly bool _persistenceEnabled;
    private string _noticeBoardRefreshKey;
    private bool _noticeBoardRefreshKeyNeedsSave;
    private string _dailyStateKey;
    private bool _moonlightRecoveryUsed;
    private int _smallPotionCount;
    private int _herbShopPotionPurchasesToday;
    private DateTime _idleLastCalculatedAtUtc;
    private int _unclaimedIdleGold;

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
            Restore(_saveService.Load());
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

    public SaveStatus GetSaveStatus()
    {
        return new SaveStatus(
            _persistenceEnabled && _saveService.HasSave(),
            Player.Gold,
            SelectedDungeonRoute.Count,
            ActiveRun?.CompletedStages ?? 0,
            ActiveRun?.BankedRewards.Count ?? 0,
            ActiveRun?.BankedRewards.Count(reward => reward.IsChest) ?? 0,
            DailyRewardsClaimed);
    }

    public void ManualSave()
    {
        RefreshNoticeBoardIfExpired();
        RefreshIdleRewards();
        Save();
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
        _noticeBoardRefreshKey = GetTodayRefreshKey();
        _dailyStateKey = GetTodayRefreshKey();
        _moonlightRecoveryUsed = false;
        _smallPotionCount = 0;
        _herbShopPotionPurchasesToday = 0;
        _idleLastCalculatedAtUtc = GetUtcNow();
        _unclaimedIdleGold = 0;
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
        SelectedPlan = _taskCatalog.CreateDungeonPlanFromRoute(SelectedDungeonRoute);
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
        LastRunSummary = summary;

        if (ActiveRun is not null)
        {
            var completedStage = ActiveRun.CurrentStage;
            LastSetSummary = _dungeonRunService.RecordStageResult(ActiveRun, summary);
            var levelsGained = Player.AddExperience(summary.ExperienceGained);
            if (levelsGained > 0)
            {
                var updatedRun = summary with { LevelsGained = levelsGained };
                LastRunSummary = updatedRun;
                LastSetSummary = LastSetSummary with { Run = updatedRun };
            }

            if (summary.RemainingPlayerHp.HasValue)
            {
                Player.SetCurrentHp(summary.RemainingPlayerHp.Value);
            }

            UpdateShortTermQuestProgress(completedStage, summary);
            UpdateLongTermQuestProgress(completedStage, summary);
            Save();
        }
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
        var idleChanged = RefreshIdleRewards(persist: false);
        var hasActiveRun = state.HasActiveRun || state.ActiveStageResults!.Count > 0;
        SelectedDungeonRoute = hasActiveRun
            ? _routeRules.NormalizeRoute(state.SelectedDungeonRoute!)
            : new List<DungeonRouteSlot>();
        SelectedPlan = SelectedDungeonRoute.Count == 0
            ? DungeonPlan.Empty
            : _taskCatalog.CreateDungeonPlanFromRoute(SelectedDungeonRoute);
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

        if (state.ExperienceToNextLevel <= 0)
        {
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

        changed = NormalizeInventory(state.Inventory) || changed;
        changed = NormalizeLoadout(state.Inventory, state.EquipmentLoadout) || changed;
        changed = NormalizeLongTermQuestState(state) || changed;

        if (!state.CurrentHp.HasValue)
        {
            var player = new PlayerState();
            player.Load(
                state.Gold,
                state.Inventory,
                state.EquipmentLoadout,
                state.Level,
                state.Experience,
                state.ExperienceToNextLevel,
                null,
                state.DailyBlessingId);
            state.CurrentHp = player.MaxHp;
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
        }

        return changed;
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
        if (summary.CompletedSets <= 0 || ActiveShortTermQuests.Count == 0)
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
            LongTermQuestObjectiveType.CompleteRooms => summary.CompletedSets > 0 ? 1 : 0,
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms =>
                summary.CompletedSets > 0 && LongTermQuestCatalog.MatchesTarget(definition, completedStage.DungeonTypeId) ? 1 : 0,
            LongTermQuestObjectiveType.DefeatBosses =>
                summary.CombatResults?.Count(result => result.IsBoss && result.EnemyDefeated) ?? 0,
            LongTermQuestObjectiveType.EarnGold => Math.Max(0, summary.Reward.Gold),
            _ => 0,
        };
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
