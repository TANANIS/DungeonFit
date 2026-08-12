using Godot;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Diagnostics;
using DungeonFit.Gameplay;
using DungeonFit.UI;

namespace DungeonFit;

public partial class Main : Control
{
    private const string TownScenePath = "res://Assets/Scenes/Town.tscn";
    private const string DungeonPlanScenePath = "res://Assets/Scenes/DungeonPlan.tscn";
    private const string NoticeBoardScenePath = "res://Assets/Scenes/NoticeBoard.tscn";
    private const string TavernScenePath = "res://Assets/Scenes/Tavern.tscn";
    private const string BlacksmithScenePath = "res://Assets/Scenes/Blacksmith.tscn";
    private const string ChurchScenePath = "res://Assets/Scenes/Church.tscn";
    private const string MoonlightFountainScenePath = "res://Assets/Scenes/MoonlightFountain.tscn";
    private const string HerbShopScenePath = "res://Assets/Scenes/HerbShop.tscn";
    private const string SetSummaryScenePath = "res://Assets/Scenes/SetSummary.tscn";
    private const string DailySummaryScenePath = "res://Assets/Scenes/DailySummary.tscn";
    private const string RoomScenePath = "res://Assets/Scenes/RoomChallenge.tscn";

    private GameSession _session = null!;
    private readonly ShortTermQuestCatalog _shortTermQuestCatalog = new();
    private Control? _currentView;

    public async override void _Ready()
    {
        GD.Print("DungeonFit entry loaded.");
        var userArgs = OS.GetCmdlineUserArgs();

        if (userArgs.Contains("--ui-screenshot-test"))
        {
            foreach (var line in await UiScreenshotTest.Run(this))
            {
                GD.Print(line);
            }

            GetTree().Quit();
            return;
        }

        if (userArgs.Contains("--ui-screenshot-room-boss-test"))
        {
            GD.Print(await UiScreenshotTest.CaptureRoomBossVisual(this));
            GetTree().Quit();
            return;
        }

        if (userArgs.Contains("--flow-smoke-test"))
        {
            foreach (var line in FlowSmokeTest.RunDefaultPlanProgression())
            {
                GD.Print(line);
            }

            foreach (var line in FlowSmokeUiLoader.Run(this))
            {
                GD.Print(line);
            }

            GetTree().Quit();
            return;
        }

        _session = new GameSession();
        ShowTown();
    }

    private void ShowTown()
    {
        _session.RefreshNoticeBoardIfExpired();
        var town = LoadView<TownView>(TownScenePath);
        town.Initialize(
            _session.Player,
            _session.SelectedPlan,
            _session.LastRunSummary,
            _session.BuildIdleRewardViewModel(),
            _session.GetSaveStatus(),
            _session.BuildBodyProfileViewModel(),
            _session.BuildTutorialGuideViewModel());
        town.EnterDungeonRequested += ShowDungeonPlan;
        town.NoticeBoardRequested += ShowNoticeBoard;
        town.TavernRequested += () => ShowTavern();
        town.BlacksmithRequested += () => ShowBlacksmith();
        town.ChurchRequested += () => ShowChurch();
        town.MoonlightFountainRequested += ShowMoonlightFountain;
        town.HerbShopRequested += ShowHerbShop;
        town.IdleRewardClaimed += () => ClaimIdleReward(town);
        town.ManualSaveRequested += () => ManualSave(town);
        town.DeleteSaveRequested += ShowTownAfterDeleteSave;
        town.ProfileSaved += (heightCm, goalId, initialWeightKg) =>
        {
            _session.UpdatePlayerProfile(heightCm, goalId);
            if (initialWeightKg.HasValue)
            {
                _session.RecordTodayWeight(initialWeightKg.Value);
            }

            ShowTown();
        };
        town.TodayWeightSaved += weightKg =>
        {
            _session.RecordTodayWeight(weightKg);
            ShowTown();
        };
        town.TutorialPrimaryRequested += HandleTutorialPrimaryAction;
        town.TutorialSkipped += () =>
        {
            _session.SkipTutorial();
            ShowTown();
        };
    }

    private void HandleTutorialPrimaryAction(string stepId)
    {
        switch (stepId)
        {
            case TutorialStepIds.Welcome:
                _session.AdvanceTutorialFromTown();
                ShowTown();
                break;
            case TutorialStepIds.VisitTavern:
                ShowTavern();
                break;
            case TutorialStepIds.ClaimRewards when _session.ActiveRun?.IsComplete == true:
                ShowDailySummary();
                break;
            default:
                ShowDungeonPlan();
                break;
        }
    }

    private void ShowMoonlightFountain()
    {
        var fountain = LoadView<MoonlightFountainView>(MoonlightFountainScenePath);
        fountain.Initialize(_session.BuildMoonlightFountainViewModel());
        fountain.BackToTownRequested += ShowTown;
        fountain.RecoveryRequested += () =>
        {
            _session.UseMoonlightRecovery();
            ShowMoonlightFountain();
        };
        fountain.BlessingSelected += blessingId =>
        {
            _session.SelectDailyBlessing(blessingId);
            ShowMoonlightFountain();
        };
    }

    private void ShowHerbShop()
    {
        var herbShop = LoadView<HerbShopView>(HerbShopScenePath);
        herbShop.Initialize(_session.BuildHerbShopViewModel());
        herbShop.BackToTownRequested += ShowTown;
        herbShop.BasicHealRequested += () =>
        {
            _session.BuyBasicHeal();
            ShowHerbShop();
        };
        herbShop.FullHealRequested += () =>
        {
            _session.BuyFullHeal();
            ShowHerbShop();
        };
        herbShop.PotionPurchaseRequested += () =>
        {
            _session.BuyHerbShopPotion();
            ShowHerbShop();
        };
    }

    private void ShowNoticeBoard()
    {
        _session.RefreshNoticeBoardIfExpired();
        var noticeBoard = LoadView<NoticeBoardView>(NoticeBoardScenePath);
        noticeBoard.Initialize(_shortTermQuestCatalog.GetDailyBoard(), _session.ActiveShortTermQuests, _session.Player);
        noticeBoard.BackToTownRequested += ShowTown;
        noticeBoard.EnterDungeonRequested += ShowDungeonPlan;
        noticeBoard.QuestAccepted += AcceptShortTermQuest;
        noticeBoard.QuestRewardClaimed += ClaimShortTermQuestReward;
    }

    private void ShowTavern(
        EquipmentInventoryFilter filter = EquipmentInventoryFilter.All,
        EquipmentInventorySort sort = EquipmentInventorySort.Rarity)
    {
        _session.MarkTutorialTavernVisited();
        var tavern = LoadView<TavernView>(TavernScenePath);
        tavern.Initialize(_session.BuildTavernEquipmentViewModel(filter, sort), _session.GetSaveStatus());
        tavern.BackToTownRequested += ShowTown;
        tavern.ManualSaveRequested += () => ManualSave(tavern);
        tavern.DeleteSaveRequested += ShowTownAfterDeleteSave;
        tavern.ViewChanged += ShowTavern;
        tavern.EquipRequested += itemId =>
        {
            _session.EquipItem(itemId);
            ShowTavern(filter, sort);
        };
        tavern.UnequipRequested += slot =>
        {
            _session.UnequipItem(slot);
            ShowTavern(filter, sort);
        };
        tavern.SellRequested += itemId =>
        {
            _session.SellEquipment(itemId);
            ShowTavern(filter, sort);
        };
        tavern.SellCommonRequested += () =>
        {
            _session.SellCommonEquipment();
            ShowTavern(filter, sort);
        };
        tavern.LockChanged += (itemId, isLocked) =>
        {
            _session.SetEquipmentLocked(itemId, isLocked);
            ShowTavern(filter, sort);
        };
        tavern.LockRareRequested += () =>
        {
            _session.LockRareEquipment();
            ShowTavern(filter, sort);
        };
    }

    private void ShowBlacksmith(string? selectedItemId = null)
    {
        var blacksmith = LoadView<BlacksmithView>(BlacksmithScenePath);
        blacksmith.Initialize(_session.BuildBlacksmithViewModel(selectedItemId));
        blacksmith.BackToTownRequested += ShowTown;
        blacksmith.EnhanceRequested += itemId =>
        {
            _session.EnhanceEquipment(itemId);
            ShowBlacksmith(itemId);
        };
        blacksmith.ExtendLevelRangeRequested += itemId =>
        {
            _session.ExtendEquipmentLevelRange(itemId);
            ShowBlacksmith(itemId);
        };
        blacksmith.DismantleRequested += itemId =>
        {
            _session.DismantleEnhancement(itemId);
            ShowBlacksmith(itemId);
        };
    }

    private void ShowChurch(string? selectedQuestId = null)
    {
        var church = LoadView<ChurchView>(ChurchScenePath);
        church.Initialize(_session.BuildChurchViewModel(selectedQuestId));
        church.BackToTownRequested += ShowTown;
        church.OathAccepted += questId =>
        {
            _session.AcceptLongTermQuest(questId);
            ShowChurch(questId);
        };
        church.OathAbandoned += () =>
        {
            _session.AbandonLongTermQuest();
            ShowChurch(selectedQuestId);
        };
        church.OathRewardClaimed += () =>
        {
            _session.ClaimLongTermQuestReward();
            ShowChurch(selectedQuestId);
        };
    }

    private void ShowDungeonPlan()
    {
        _session.RefreshNoticeBoardIfExpired();
        var plan = LoadView<DungeonPlanView>(DungeonPlanScenePath);
        plan.Initialize(
            _session.SelectedPlan,
            _session.ActiveRun,
            _session.SelectedDungeonRoute,
            _session.CanEditPlan,
            _session.ActiveShortTermQuests);
        plan.StartAdventureRequested += () => StartAdventure(plan);
        plan.DailySummaryRequested += ShowDailySummary;
        plan.BackToTownRequested += ShowTown;
    }

    private void StartAdventure(DungeonPlanView plan)
    {
        _session.UpdateDungeonRoute(plan.GetSelectedDungeonRoute());
        ShowRoomChallenge();
    }

    private void ShowRoomChallenge()
    {
        var activeRun = _session.StartOrGetActiveRun();

        if (activeRun is null || activeRun.IsComplete)
        {
            ShowDungeonPlan();
            return;
        }

        _session.ClearPendingSetSummary();
        var room = LoadView<RoomChallengeView>(RoomScenePath);
        room.Initialize(
            _session.Player,
            activeRun.CurrentStage,
            activeRun.CurrentStageIndex + 1,
            activeRun.Plan.Stages.Count,
            activeRun.CurrentPlayerHp,
            _session.BuildRoomSupplyViewModel(),
            _session.GetLastExerciseHistory(activeRun.CurrentStage.ExerciseId),
            _session.GetExercisePersonalRecord(activeRun.CurrentStage.ExerciseId));
        room.RoomContinueRequested += CompleteRoomAndShowSetSummary;
        room.ReturnToTownRequested += ReturnTownFromPausedRoom;
        room.SmallPotionRequested += currentHp => _session.UseSmallPotionInRoom(currentHp);
    }

    private void ReturnTownFromPausedRoom(int currentHp)
    {
        _session.LeaveActiveRoom(currentHp);
        ShowTown();
    }

    private void CompleteRoomAndShowSetSummary(RunSummary summary, ExerciseHistoryEntry? exerciseRecord)
    {
        _session.RecordStageResult(summary, exerciseRecord);
        ShowSetSummary();
    }

    private void ShowSetSummary()
    {
        if (_session.LastSetSummary is null)
        {
            ShowDungeonPlan();
            return;
        }

        var setSummary = _session.LastSetSummary;
        var summary = LoadView<SetSummaryView>(SetSummaryScenePath);
        summary.Initialize(setSummary);
        summary.ContinueRequested += setSummary.NextStage is null ? ShowDailySummary : ShowDungeonPlan;
        summary.ReturnToTownRequested += ShowDailySummary;
    }

    private void ShowDailySummary()
    {
        var dailySummary = _session.BuildDailySummary();

        if (dailySummary is null)
        {
            ShowDungeonPlan();
            return;
        }

        var summary = LoadView<DailySummaryView>(DailySummaryScenePath);
        summary.Initialize(dailySummary, _session.DailyRewardsClaimed);
        summary.OpenAllRequested += () => ClaimDailyRewards(summary);
        summary.ReturnToTownRequested += CompleteDailyRunAndReturnTown;
    }

    private void ClaimDailyRewards(DailySummaryView summary)
    {
        _session.ClaimDailyRewards();
        summary.MarkClaimed();
    }

    private void CompleteDailyRunAndReturnTown()
    {
        _session.CompleteDailyRun();
        ShowTown();
    }

    private void ManualSave(TownView town)
    {
        _session.ManualSave();
        town.UpdateIdleReward(_session.BuildIdleRewardViewModel());
        town.UpdateSaveStatus(_session.GetSaveStatus());
    }

    private void ClaimIdleReward(TownView town)
    {
        _session.ClaimIdleRewards();
        town.Initialize(
            _session.Player,
            _session.SelectedPlan,
            _session.LastRunSummary,
            _session.BuildIdleRewardViewModel(),
            _session.GetSaveStatus(),
            _session.BuildBodyProfileViewModel());
    }

    private void ManualSave(TavernView tavern)
    {
        _session.ManualSave();
        tavern.UpdateSaveStatus(_session.GetSaveStatus());
    }

    private void ShowTownAfterDeleteSave()
    {
        _session.DeleteSaveAndReset();
        ShowTown();
    }

    private void AcceptShortTermQuest(string questId)
    {
        _session.AcceptShortTermQuest(questId);
    }

    private bool ClaimShortTermQuestReward(string questId)
    {
        return _session.ClaimShortTermQuestReward(questId);
    }

    private TView LoadView<TView>(string scenePath)
        where TView : Control
    {
        _currentView?.QueueFree();

        var scene = GD.Load<PackedScene>(scenePath);
        var view = scene.Instantiate<TView>();
        AddChild(view);
        _currentView = view;

        return view;
    }
}
