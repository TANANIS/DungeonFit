using Godot;
using System;
using DungeonFit.Core.Models;
using DungeonFit.Gameplay;

namespace DungeonFit.UI;

public partial class TownView : Control
{
    public event Action? EnterDungeonRequested;
    public event Action? NoticeBoardRequested;
    public event Action? TavernRequested;
    public event Action? BlacksmithRequested;
    public event Action? MoonlightFountainRequested;
    public event Action? HerbShopRequested;
    public event Action? ChurchRequested;
    public event Action? IdleRewardClaimed;
    public event Action? ManualSaveRequested;
    public event Action? DeleteSaveRequested;

    private PlayerState _player = new();
    private DungeonPlan _todayPlan = null!;
    private RunSummary? _lastRunSummary;
    private IdleRewardViewModel _idleReward = new(0, 72, 10, false, string.Empty);
    private SaveStatus? _saveStatus;
    private Label _levelLabel = null!;
    private Label _goldLabel = null!;
    private Label _todayChallenge = null!;
    private Label _idleStatus = null!;
    private Button _idleClaimButton = null!;
    private Label _lastReward = null!;
    private Label _saveStatusLabel = null!;
    private PanelContainer _settingsPanel = null!;

    public override void _Ready()
    {
        _levelLabel = GetNode<Label>("%LevelLabel");
        _goldLabel = GetNode<Label>("%GoldLabel");
        _todayChallenge = GetNode<Label>("%TodayChallenge");
        _idleStatus = GetNode<Label>("%IdleStatus");
        _lastReward = GetNode<Label>("%LastReward");
        _idleClaimButton = CreateIdleClaimButton();
        GetNode<VBoxContainer>("SafeMargin/Layout/IdlePanel/IdleMargin/IdleLayout/IdleText").AddChild(_idleClaimButton);

        GetNode<Button>("%EnterDungeonButton").Pressed += RequestEnterDungeon;
        GetNode<Button>("%SettingsButton").Pressed += ShowSettings;
        var noticeBoard = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/NoticeBoard");
        noticeBoard.MouseDefaultCursorShape = CursorShape.PointingHand;
        noticeBoard.GuiInput += OnNoticeBoardInput;
        var tavern = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/Tavern");
        tavern.MouseDefaultCursorShape = CursorShape.PointingHand;
        tavern.GuiInput += OnTavernInput;
        var blacksmith = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/GeneralStore");
        blacksmith.MouseDefaultCursorShape = CursorShape.PointingHand;
        blacksmith.GuiInput += OnBlacksmithInput;
        var herbShop = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/HerbShop");
        herbShop.MouseDefaultCursorShape = CursorShape.PointingHand;
        herbShop.GuiInput += OnHerbShopInput;
        var fountain = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/Fountain");
        fountain.MouseDefaultCursorShape = CursorShape.PointingHand;
        fountain.GuiInput += OnFountainInput;
        var church = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/Church");
        church.MouseDefaultCursorShape = CursorShape.PointingHand;
        church.GuiInput += OnChurchInput;
        BuildSettingsPanel();
        Refresh();
    }

    public void Initialize(
        PlayerState player,
        DungeonPlan todayPlan,
        RunSummary? lastRunSummary,
        IdleRewardViewModel idleReward,
        SaveStatus saveStatus)
    {
        _player = player;
        _todayPlan = todayPlan;
        _lastRunSummary = lastRunSummary;
        _idleReward = idleReward;
        _saveStatus = saveStatus;

        if (IsNodeReady())
        {
            Refresh();
        }
    }

    public void UpdateSaveStatus(SaveStatus saveStatus)
    {
        _saveStatus = saveStatus;
        RefreshSaveStatus();
    }

    public void UpdateIdleReward(IdleRewardViewModel idleReward)
    {
        _idleReward = idleReward;
        RefreshIdleReward();
    }

    private void Refresh()
    {
        _levelLabel.Text = string.Format(Text.AdventurerLevelFormat, _player.Level);
        _goldLabel.Text = string.Format(Text.GoldFormat, _player.Gold);
        _todayChallenge.Text = _todayPlan is null
            ? Text.RoutePreparing
            : _todayPlan.Stages.Count == 0
                ? Text.RouteEmpty
                : string.Format(Text.RouteSummaryFormat, _todayPlan.DisplayName, _todayPlan.Stages.Count, _todayPlan.TotalSets);
        RefreshIdleReward();
        _lastReward.Text = _lastRunSummary is null
            ? Text.NoBankedReward
            : string.Format(
                Text.LastRewardFormat,
                GetResultTitle(_lastRunSummary.Title),
                _lastRunSummary.RewardText,
                _lastRunSummary.RoomName,
                _lastRunSummary.CompletedSets,
                _lastRunSummary.TotalSets,
                _lastRunSummary.ExperienceGained);
        RefreshSaveStatus();
    }

    private void RefreshIdleReward()
    {
        _idleStatus.Text = string.IsNullOrWhiteSpace(_idleReward.StatusText)
            ? Text.IdleStatus
            : _idleReward.StatusText;

        if (_idleClaimButton is null)
        {
            return;
        }

        _idleClaimButton.Disabled = !_idleReward.CanClaim;
        _idleClaimButton.Text = _idleReward.CanClaim
            ? string.Format(Text.ClaimIdleRewardFormat, _idleReward.UnclaimedGold)
            : string.Format(Text.IdleRewardEmptyFormat, _idleReward.RewardIntervalMinutes);
    }

    private void RefreshSaveStatus()
    {
        if (_saveStatusLabel is null)
        {
            return;
        }

        if (_saveStatus is null)
        {
            _saveStatusLabel.Text = Text.SaveStatusUnknown;
            return;
        }

        _saveStatusLabel.Text = _saveStatus.HasSaveFile
            ? string.Format(
                Text.SaveStatusFormat,
                _saveStatus.Gold,
                _saveStatus.RouteSlotCount,
                _saveStatus.CompletedStageCount,
                _saveStatus.BankedRewardCount,
                _saveStatus.BankedChestCount,
                _saveStatus.DailyRewardsClaimed ? Text.Claimed : Text.Unclaimed)
            : Text.NoSaveFile;
    }

    private void BuildSettingsPanel()
    {
        _settingsPanel = new PanelContainer
        {
            Name = "SettingsPanel",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _settingsPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_settingsPanel);
        _settingsPanel.MoveToFront();

        var outerMargin = new MarginContainer();
        outerMargin.AddThemeConstantOverride("margin_left", 44);
        outerMargin.AddThemeConstantOverride("margin_top", 120);
        outerMargin.AddThemeConstantOverride("margin_right", 44);
        outerMargin.AddThemeConstantOverride("margin_bottom", 120);
        _settingsPanel.AddChild(outerMargin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 20);
        outerMargin.AddChild(layout);

        var title = new Label
        {
            Text = Text.SettingsTitle,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 42);
        layout.AddChild(title);

        var description = new Label
        {
            Text = Text.SettingsDescription,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        description.AddThemeFontSizeOverride("font_size", 26);
        layout.AddChild(description);

        _saveStatusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _saveStatusLabel.AddThemeFontSizeOverride("font_size", 28);
        layout.AddChild(_saveStatusLabel);

        var saveButton = CreateSettingsButton(Text.ManualSave);
        saveButton.Pressed += () => ManualSaveRequested?.Invoke();
        layout.AddChild(saveButton);

        var deleteButton = CreateSettingsButton(Text.DeleteSave);
        deleteButton.Pressed += () => DeleteSaveRequested?.Invoke();
        layout.AddChild(deleteButton);

        var closeButton = CreateSettingsButton(Text.Close);
        closeButton.Pressed += HideSettings;
        layout.AddChild(closeButton);
    }

    private static Button CreateSettingsButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 86),
        };
        button.AddThemeFontSizeOverride("font_size", 30);
        return button;
    }

    private Button CreateIdleClaimButton()
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(0, 64),
        };
        button.AddThemeFontSizeOverride("font_size", 27);
        button.Pressed += () => IdleRewardClaimed?.Invoke();
        return button;
    }

    private void ShowSettings()
    {
        _settingsPanel.Visible = true;
        _settingsPanel.MoveToFront();
        RefreshSaveStatus();
    }

    private void HideSettings()
    {
        _settingsPanel.Visible = false;
    }

    private void RequestEnterDungeon()
    {
        EnterDungeonRequested?.Invoke();
    }

    private void OnNoticeBoardInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            NoticeBoardRequested?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnTavernInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            TavernRequested?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnBlacksmithInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            BlacksmithRequested?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnHerbShopInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            HerbShopRequested?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnFountainInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            MoonlightFountainRequested?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnChurchInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            ChurchRequested?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    private static string GetResultTitle(string title)
    {
        return title switch
        {
            "Boss Cleared" => Text.BossCleared,
            "Room Withdrawn" => Text.RoomWithdrawn,
            _ => title,
        };
    }

    private static class Text
    {
        public const string AdventurerLevelFormat = "\u5192\u96aa\u8005  Lv.{0}";
        public const string GoldFormat = "\u91d1\u5e63  {0}";
        public const string RoutePreparing = "\u4eca\u65e5\u8def\u7dda\uff1a\u6e96\u5099\u4e2d...";
        public const string RouteEmpty = "\u4eca\u65e5\u8def\u7dda\uff1a\u5c1a\u672a\u898f\u5283";
        public const string RouteSummaryFormat = "\u4eca\u65e5\u8def\u7dda\uff1a{0}\n{1} \u623f\u9593  /  {2} \u7d44";
        public const string IdleStatus = "\u6236\u5916\u63a2\u7d22\u4e2d\u3002";
        public const string ClaimIdleRewardFormat = "\u9818\u53d6\u63a2\u7d22\u6536\u76ca +{0} \u91d1\u5e63";
        public const string IdleRewardEmptyFormat = "\u63a2\u7d22\u4e2d\uff1a\u6bcf {0} \u5206\u9418 +1 \u91d1\u5e63";
        public const string NoBankedReward = "\u5c1a\u672a\u66ab\u5b58\u623f\u9593\u6536\u76ca\u3002";
        public const string LastRewardFormat = "{0}\uff1a{1}\n{2}  \u7d44\u6578 {3} / {4}  EXP +{5}";
        public const string SettingsTitle = "\u8a2d\u5b9a";
        public const string SettingsDescription = "\u904a\u6232\u6703\u5728\u8def\u7dda\u3001\u623f\u9593\u7d50\u679c\u8207\u9818\u53d6\u734e\u52f5\u6642\u81ea\u52d5\u5132\u5b58\u3002";
        public const string SaveStatusUnknown = "\u5b58\u6a94\u72c0\u614b\uff1a\u672a\u77e5";
        public const string NoSaveFile = "\u5b58\u6a94\u72c0\u614b\uff1a\u76ee\u524d\u6c92\u6709\u5b58\u6a94";
        public const string SaveStatusFormat = "\u5b58\u6a94\u72c0\u614b\uff1a\u5df2\u5b58\u5728\n\u91d1\u5e63 {0} / \u8def\u7dda {1} / \u5df2\u5b8c\u6210 {2} / \u66ab\u5b58\u734e\u52f5 {3} / \u5bf6\u7bb1 {4}\n\u4eca\u65e5\u734e\u52f5\uff1a{5}";
        public const string Claimed = "\u5df2\u9818\u53d6";
        public const string Unclaimed = "\u672a\u9818\u53d6";
        public const string ManualSave = "\u624b\u52d5\u5132\u5b58";
        public const string DeleteSave = "\u522a\u9664\u7576\u524d\u5b58\u6a94";
        public const string Close = "\u95dc\u9589";
        public const string BossCleared = "Boss \u64ca\u7834";
        public const string RoomWithdrawn = "\u623f\u9593\u64a4\u9000";
    }
}
