using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public event Action<int, string, double?>? ProfileSaved;
    public event Action<double>? TodayWeightSaved;

    private PlayerState _player = new();
    private DungeonPlan _todayPlan = null!;
    private RunSummary? _lastRunSummary;
    private IdleRewardViewModel _idleReward = new(0, 72, 10, false, string.Empty);
    private BodyProfileViewModel _bodyProfile = BodyProfileViewModel.Empty;
    private SaveStatus? _saveStatus;
    private Label _levelLabel = null!;
    private Label _goldLabel = null!;
    private Label _todayChallenge = null!;
    private Label _bodyStatus = null!;
    private Label _idleStatus = null!;
    private Button _idleClaimButton = null!;
    private Label _lastReward = null!;
    private Label _saveStatusLabel = null!;
    private PanelContainer _settingsPanel = null!;
    private PanelContainer _bodyPanel = null!;
    private Label _bodyDialogError = null!;
    private Label _bodyDialogAdvice = null!;
    private LineEdit _heightInput = null!;
    private LineEdit _weightInput = null!;
    private string _selectedGoalId = FitnessGoal.GeneralHealth;
    private readonly List<Button> _goalButtons = new();
    private bool _hasInitialized;
    private bool _onboardingPromptShown;

    public override void _Ready()
    {
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.TownBackground);
        _levelLabel = GetNode<Label>("%LevelLabel");
        _goldLabel = GetNode<Label>("%GoldLabel");
        _todayChallenge = GetNode<Label>("%TodayChallenge");
        _idleStatus = GetNode<Label>("%IdleStatus");
        _lastReward = GetNode<Label>("%LastReward");
        _idleClaimButton = CreateIdleClaimButton();
        _bodyStatus = CreateBodyStatusLabel();
        var idleText = GetNode<VBoxContainer>("SafeMargin/Layout/IdlePanel/IdleMargin/IdleLayout/IdleText");
        idleText.AddChild(_bodyStatus);
        idleText.AddChild(_idleClaimButton);

        GetNode<Button>("%EnterDungeonButton").Pressed += RequestEnterDungeon;
        GetNode<Button>("%SettingsButton").Pressed += ShowSettings;
        var townGrid = GetNode<GridContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid");
        var noticeBoard = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/NoticeBoard");
        DecorateFacility(noticeBoard, "notice_board", Text.NoticeBoard);
        noticeBoard.MouseDefaultCursorShape = CursorShape.PointingHand;
        noticeBoard.GuiInput += OnNoticeBoardInput;
        var tavern = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/Tavern");
        DecorateFacility(tavern, "tavern", Text.Tavern);
        tavern.MouseDefaultCursorShape = CursorShape.PointingHand;
        tavern.GuiInput += OnTavernInput;
        var blacksmith = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/GeneralStore");
        DecorateFacility(blacksmith, "blacksmith", Text.Blacksmith);
        blacksmith.MouseDefaultCursorShape = CursorShape.PointingHand;
        blacksmith.GuiInput += OnBlacksmithInput;
        var herbShop = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/HerbShop");
        DecorateFacility(herbShop, "herb_shop", Text.HerbShop);
        herbShop.MouseDefaultCursorShape = CursorShape.PointingHand;
        herbShop.GuiInput += OnHerbShopInput;
        var fountain = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/Fountain");
        DecorateFacility(fountain, "fountain", Text.Fountain);
        fountain.MouseDefaultCursorShape = CursorShape.PointingHand;
        fountain.GuiInput += OnFountainInput;
        var church = GetNode<PanelContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout/TownGrid/Church");
        DecorateFacility(church, "church", Text.Church);
        church.MouseDefaultCursorShape = CursorShape.PointingHand;
        church.GuiInput += OnChurchInput;
        BuildTownMap(townGrid, herbShop, tavern, blacksmith, noticeBoard, fountain, church);
        ApplyArtStyles();
        BuildSettingsPanel();
        BuildBodyPanel();
        Refresh();
    }

    public void Initialize(
        PlayerState player,
        DungeonPlan todayPlan,
        RunSummary? lastRunSummary,
        IdleRewardViewModel idleReward,
        SaveStatus saveStatus,
        BodyProfileViewModel? bodyProfile = null)
    {
        _player = player;
        _todayPlan = todayPlan;
        _lastRunSummary = lastRunSummary;
        _idleReward = idleReward;
        _saveStatus = saveStatus;
        _bodyProfile = bodyProfile ?? BodyProfileViewModel.Empty;
        _hasInitialized = true;

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
        RefreshBodyStatus();
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
        MaybeShowOnboarding();
    }

    public bool SmokeOpenProfileOnboarding()
    {
        if (!IsNodeReady())
        {
            return false;
        }

        ShowProfileDialog(onboarding: true);
        return _bodyPanel.Visible;
    }

    public bool SmokeOpenBodyMetricsDialog()
    {
        if (!IsNodeReady())
        {
            return false;
        }

        ShowBodyMetricsDialog();
        return _bodyPanel.Visible;
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

    private void RefreshBodyStatus()
    {
        if (_bodyStatus is null)
        {
            return;
        }

        _bodyStatus.Text = string.IsNullOrWhiteSpace(_bodyProfile.TodayStatusText)
            ? Text.TodayWeightMissing
            : _bodyProfile.TodayStatusText;
    }

    private void MaybeShowOnboarding()
    {
        if (_bodyPanel is null ||
            !_hasInitialized ||
            _onboardingPromptShown ||
            _bodyProfile.HasCompletedOnboarding ||
            _settingsPanel.Visible ||
            _bodyPanel.Visible)
        {
            return;
        }

        _onboardingPromptShown = true;
        ShowProfileDialog(onboarding: true);
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

        if (!string.IsNullOrWhiteSpace(_saveStatus.WarningMessage))
        {
            _saveStatusLabel.Text = _saveStatus.WarningMessage;
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
        DungeonFitUi.ApplyPanel(_settingsPanel, UiPanelStyle.Overlay);
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

        var profileButton = CreateSettingsButton(Text.ProfileSettings);
        profileButton.Pressed += () => ShowProfileDialog(onboarding: false);
        layout.AddChild(profileButton);

        var weightButton = CreateSettingsButton(Text.TodayWeightSettings);
        weightButton.Pressed += ShowBodyMetricsDialog;
        layout.AddChild(weightButton);

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
        DungeonFitUi.ApplyButton(button, text == Text.DeleteSave ? UiButtonStyle.Danger : UiButtonStyle.Secondary);
        return button;
    }

    private void BuildBodyPanel()
    {
        _bodyPanel = new PanelContainer
        {
            Name = "BodyProfilePanel",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _bodyPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        DungeonFitUi.ApplyPanel(_bodyPanel, UiPanelStyle.Overlay);
        AddChild(_bodyPanel);
        _bodyPanel.MoveToFront();
    }

    private void ShowProfileDialog(bool onboarding)
    {
        HideSettings();
        _selectedGoalId = FitnessGoal.Normalize(_bodyProfile.GoalId);
        _goalButtons.Clear();
        ClearChildren(_bodyPanel);

        var layout = CreateBodyDialogLayout();
        _bodyPanel.AddChild(layout.Root);
        layout.Content.AddChild(CreateDialogTitle(onboarding ? Text.OnboardingTitle : Text.ProfileTitle));
        layout.Content.AddChild(CreateDialogDescription(onboarding ? Text.OnboardingDescription : Text.ProfileDescription));

        _heightInput = CreateDialogInput(Text.HeightPlaceholder, _bodyProfile.HeightCm > 0 ? _bodyProfile.HeightCm.ToString(CultureInfo.InvariantCulture) : string.Empty);
        layout.Content.AddChild(CreateField(Text.HeightLabel, _heightInput));

        if (onboarding)
        {
            _weightInput = CreateDialogInput(Text.InitialWeightPlaceholder, _bodyProfile.TodayWeightKg.HasValue ? _bodyProfile.TodayWeightKg.Value.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty);
            layout.Content.AddChild(CreateField(Text.InitialWeightLabel, _weightInput));
        }

        layout.Content.AddChild(CreateGoalSelector());
        _bodyDialogAdvice = CreateDialogDescription(FitnessGoal.GetAdvice(_selectedGoalId));
        layout.Content.AddChild(_bodyDialogAdvice);
        _bodyDialogError = CreateDialogError();
        layout.Content.AddChild(_bodyDialogError);

        var actions = CreateDialogActions();
        var cancelButton = CreateDialogButton(onboarding ? Text.Later : Text.Cancel, UiButtonStyle.Secondary);
        cancelButton.Pressed += HideBodyPanel;
        actions.AddChild(cancelButton);

        var saveButton = CreateDialogButton(onboarding ? Text.StartProfile : Text.SaveProfile, UiButtonStyle.Primary);
        saveButton.Pressed += () => SubmitProfile(onboarding);
        actions.AddChild(saveButton);
        layout.Content.AddChild(actions);

        _bodyPanel.Visible = true;
        _bodyPanel.MoveToFront();
    }

    private void ShowBodyMetricsDialog()
    {
        HideSettings();
        ClearChildren(_bodyPanel);

        var layout = CreateBodyDialogLayout();
        _bodyPanel.AddChild(layout.Root);
        layout.Content.AddChild(CreateDialogTitle(Text.TodayWeightTitle));
        layout.Content.AddChild(CreateDialogDescription(Text.TodayWeightDescription));

        _weightInput = CreateDialogInput(Text.TodayWeightPlaceholder, _bodyProfile.TodayWeightKg.HasValue ? _bodyProfile.TodayWeightKg.Value.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty);
        layout.Content.AddChild(CreateField(Text.TodayWeightInputLabel, _weightInput));
        layout.Content.AddChild(CreateDialogDescription(_bodyProfile.GoalAdvice));
        _bodyDialogError = CreateDialogError();
        layout.Content.AddChild(_bodyDialogError);

        var actions = CreateDialogActions();
        var cancelButton = CreateDialogButton(Text.Cancel, UiButtonStyle.Secondary);
        cancelButton.Pressed += HideBodyPanel;
        actions.AddChild(cancelButton);

        var saveButton = CreateDialogButton(Text.SaveTodayWeight, UiButtonStyle.Primary);
        saveButton.Pressed += SubmitTodayWeight;
        actions.AddChild(saveButton);
        layout.Content.AddChild(actions);

        _bodyPanel.Visible = true;
        _bodyPanel.MoveToFront();
    }

    private static (MarginContainer Root, VBoxContainer Content) CreateBodyDialogLayout()
    {
        var root = new MarginContainer();
        root.AddThemeConstantOverride("margin_left", 44);
        root.AddThemeConstantOverride("margin_top", 104);
        root.AddThemeConstantOverride("margin_right", 44);
        root.AddThemeConstantOverride("margin_bottom", 104);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 18);
        root.AddChild(content);
        return (root, content);
    }

    private static Label CreateDialogTitle(string text)
    {
        var title = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        title.AddThemeFontSizeOverride("font_size", 42);
        return title;
    }

    private static Label CreateDialogDescription(string text)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("font_size", 25);
        return label;
    }

    private static Label CreateDialogError()
    {
        var label = new Label
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.72f));
        return label;
    }

    private static Control CreateField(string labelText, LineEdit input)
    {
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        var label = new Label
        {
            Text = labelText,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("font_size", 25);
        layout.AddChild(label);
        layout.AddChild(input);
        return layout;
    }

    private static LineEdit CreateDialogInput(string placeholder, string text)
    {
        var input = new LineEdit
        {
            Text = text,
            PlaceholderText = placeholder,
            CustomMinimumSize = new Vector2(0, 76),
            VirtualKeyboardType = LineEdit.VirtualKeyboardTypeEnum.NumberDecimal,
            Alignment = HorizontalAlignment.Center,
        };
        input.AddThemeFontSizeOverride("font_size", 32);
        return input;
    }

    private Control CreateGoalSelector()
    {
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 10);
        var label = new Label
        {
            Text = Text.GoalLabel,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("font_size", 25);
        layout.AddChild(label);

        var grid = new GridContainer
        {
            Columns = 2,
        };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        layout.AddChild(grid);

        foreach (var goalId in FitnessGoal.AllIds)
        {
            var button = CreateDialogButton(FitnessGoal.GetLabel(goalId), UiButtonStyle.Secondary);
            button.CustomMinimumSize = new Vector2(0, 74);
            button.Pressed += () => SelectGoal(goalId);
            _goalButtons.Add(button);
            grid.AddChild(button);
        }

        RefreshGoalButtons();
        return layout;
    }

    private void SelectGoal(string goalId)
    {
        _selectedGoalId = FitnessGoal.Normalize(goalId);
        RefreshGoalButtons();

        if (_bodyDialogAdvice is not null)
        {
            _bodyDialogAdvice.Text = FitnessGoal.GetAdvice(_selectedGoalId);
        }
    }

    private void RefreshGoalButtons()
    {
        foreach (var button in _goalButtons)
        {
            var goalId = FitnessGoal.AllIds[_goalButtons.IndexOf(button)];
            button.Text = goalId == _selectedGoalId
                ? $"✓ {FitnessGoal.GetLabel(goalId)}"
                : FitnessGoal.GetLabel(goalId);
        }
    }

    private static HBoxContainer CreateDialogActions()
    {
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 14);
        return actions;
    }

    private static Button CreateDialogButton(string text, UiButtonStyle style)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 84),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        button.AddThemeFontSizeOverride("font_size", 29);
        DungeonFitUi.ApplyButton(button, style);
        return button;
    }

    private static Label CreateBodyStatusLabel()
    {
        var label = new Label
        {
            Text = Text.TodayWeightMissing,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", new Color(0.92f, 0.84f, 1f));
        return label;
    }

    private void SubmitProfile(bool onboarding)
    {
        if (!TryParseHeight(_heightInput.Text, out var heightCm))
        {
            _bodyDialogError.Text = Text.HeightError;
            return;
        }

        double? weightKg = null;
        if (onboarding && !string.IsNullOrWhiteSpace(_weightInput.Text))
        {
            if (!TryParseWeight(_weightInput.Text, out var parsedWeight))
            {
                _bodyDialogError.Text = Text.WeightError;
                return;
            }

            weightKg = parsedWeight;
        }

        HideBodyPanel();
        ProfileSaved?.Invoke(heightCm, _selectedGoalId, weightKg);
    }

    private void SubmitTodayWeight()
    {
        if (!TryParseWeight(_weightInput.Text, out var weightKg))
        {
            _bodyDialogError.Text = Text.WeightError;
            return;
        }

        HideBodyPanel();
        TodayWeightSaved?.Invoke(weightKg);
    }

    private static bool TryParseHeight(string text, out int heightCm)
    {
        heightCm = 0;
        return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out heightCm) &&
            heightCm >= PlayerProfile.MinHeightCm &&
            heightCm <= PlayerProfile.MaxHeightCm;
    }

    private static bool TryParseWeight(string text, out double weightKg)
    {
        weightKg = 0;
        return double.TryParse(text.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out weightKg) &&
            weightKg >= BodyMetricEntry.MinWeightKg &&
            weightKg <= BodyMetricEntry.MaxWeightKg;
    }

    private void HideBodyPanel()
    {
        _bodyPanel.Visible = false;
    }

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    private Button CreateIdleClaimButton()
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(0, 64),
        };
        button.AddThemeFontSizeOverride("font_size", 27);
        DungeonFitUi.ApplyButton(button, UiButtonStyle.Secondary);
        button.Pressed += () => IdleRewardClaimed?.Invoke();
        return button;
    }

    private void ApplyArtStyles()
    {
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/Header"), UiPanelStyle.Main);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/TownStage"), UiPanelStyle.Main);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/IdlePanel"), UiPanelStyle.Card);
        DungeonFitUi.DecorateExistingIconPanel(
            GetNode<PanelContainer>("SafeMargin/Layout/IdlePanel/IdleMargin/IdleLayout/IdleToken"),
            UiThemePaths.IdleToken,
            96);
        DungeonFitUi.ApplyButton(GetNode<Button>("%EnterDungeonButton"), UiButtonStyle.Primary);
        DungeonFitUi.ApplyButton(GetNode<Button>("%SettingsButton"), UiButtonStyle.Secondary);
        DungeonFitUi.ApplyProgressBar(
            GetNode<ProgressBar>("SafeMargin/Layout/Header/HeaderMargin/HeaderRow/PlayerInfo/ExpBar"),
            new Color(0.48f, 0.82f, 0.58f));
    }

    private static void DecorateFacility(PanelContainer panel, string iconId, string labelText)
    {
        if (panel.GetChildCount() > 0 && panel.GetChild(0) is Label label)
        {
            label.Text = labelText;
        }

        DungeonFitUi.DecorateExistingIconPanel(panel, UiThemePaths.TownFacilityIcon(iconId), 86);
    }

    private void BuildTownMap(
        GridContainer townGrid,
        PanelContainer herbShop,
        PanelContainer tavern,
        PanelContainer blacksmith,
        PanelContainer noticeBoard,
        PanelContainer fountain,
        PanelContainer church)
    {
        var townLayout = GetNode<VBoxContainer>("SafeMargin/Layout/TownStage/TownMargin/TownLayout");
        var gridIndex = townGrid.GetIndex();
        townLayout.RemoveChild(townGrid);

        var map = new Control
        {
            Name = "TownMap",
            CustomMinimumSize = new Vector2(0, 610),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        townLayout.AddChild(map);
        townLayout.MoveChild(map, gridIndex);

        PlaceFacility(map, herbShop, new Vector2(22, 168), new Vector2(225, 132));
        PlaceFacility(map, tavern, new Vector2(362, 72), new Vector2(255, 152));
        PlaceFacility(map, blacksmith, new Vector2(725, 170), new Vector2(225, 132));
        PlaceFacility(map, noticeBoard, new Vector2(16, 420), new Vector2(230, 124));
        PlaceFacility(map, fountain, new Vector2(370, 354), new Vector2(240, 132));
        PlaceFacility(map, church, new Vector2(724, 456), new Vector2(230, 124));

        townGrid.QueueFree();
    }

    private static void PlaceFacility(Control map, PanelContainer panel, Vector2 position, Vector2 size)
    {
        panel.GetParent()?.RemoveChild(panel);
        map.AddChild(panel);
        panel.SetAnchorsPreset(LayoutPreset.TopLeft);
        panel.Position = position;
        panel.Size = size;
        panel.CustomMinimumSize = size;
        panel.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        panel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
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
        public const string TodayWeightMissing = "\u4eca\u65e5\u5c1a\u672a\u8a18\u9304\u9ad4\u91cd";
        public const string SaveStatusUnknown = "\u5b58\u6a94\u72c0\u614b\uff1a\u672a\u77e5";
        public const string NoSaveFile = "\u5b58\u6a94\u72c0\u614b\uff1a\u76ee\u524d\u6c92\u6709\u5b58\u6a94";
        public const string SaveStatusFormat = "\u5b58\u6a94\u72c0\u614b\uff1a\u5df2\u5b58\u5728\n\u91d1\u5e63 {0} / \u8def\u7dda {1} / \u5df2\u5b8c\u6210 {2} / \u66ab\u5b58\u734e\u52f5 {3} / \u5bf6\u7bb1 {4}\n\u4eca\u65e5\u734e\u52f5\uff1a{5}";
        public const string Claimed = "\u5df2\u9818\u53d6";
        public const string Unclaimed = "\u672a\u9818\u53d6";
        public const string ManualSave = "\u624b\u52d5\u5132\u5b58";
        public const string ProfileSettings = "\u500b\u4eba\u6a94\u6848";
        public const string TodayWeightSettings = "\u4eca\u65e5\u9ad4\u91cd";
        public const string DeleteSave = "\u522a\u9664\u7576\u524d\u5b58\u6a94";
        public const string Close = "\u95dc\u9589";
        public const string OnboardingTitle = "\u5efa\u7acb\u500b\u4eba\u6a94\u6848";
        public const string OnboardingDescription = "\u586b\u5165\u8eab\u9ad8\u3001\u521d\u59cb\u9ad4\u91cd\u8207\u76ee\u6a19\uff0c\u7528\u4f86\u986f\u793a\u8a13\u7df4\u5efa\u8b70\u3002";
        public const string ProfileTitle = "\u500b\u4eba\u6a94\u6848";
        public const string ProfileDescription = "\u4fee\u6539\u8eab\u9ad8\u8207\u76ee\u6a19\uff0c\u4e0d\u6703\u6539\u8b8a\u6230\u9b25\u6216\u734e\u52f5\u6578\u503c\u3002";
        public const string TodayWeightTitle = "\u4eca\u65e5\u9ad4\u91cd";
        public const string TodayWeightDescription = "\u6bcf\u5929\u4fdd\u7559\u4e00\u7b46\u9ad4\u91cd\uff0c\u91cd\u65b0\u586b\u5beb\u6703\u8986\u84cb\u4eca\u5929\u7684\u7d00\u9304\u3002";
        public const string HeightLabel = "\u8eab\u9ad8 cm";
        public const string HeightPlaceholder = "100-230";
        public const string InitialWeightLabel = "\u521d\u59cb\u9ad4\u91cd kg\uff08\u53ef\u7565\uff09";
        public const string InitialWeightPlaceholder = "30.0-250.0";
        public const string TodayWeightInputLabel = "\u9ad4\u91cd kg";
        public const string TodayWeightPlaceholder = "30.0-250.0";
        public const string GoalLabel = "\u76ee\u6a19";
        public const string HeightError = "\u8eab\u9ad8\u9700\u8981\u5728 100-230 cm \u4e4b\u9593\u3002";
        public const string WeightError = "\u9ad4\u91cd\u9700\u8981\u5728 30.0-250.0 kg \u4e4b\u9593\u3002";
        public const string Later = "\u7a0d\u5f8c";
        public const string Cancel = "\u53d6\u6d88";
        public const string StartProfile = "\u958b\u59cb\u8a18\u9304";
        public const string SaveProfile = "\u5132\u5b58\u6a94\u6848";
        public const string SaveTodayWeight = "\u5132\u5b58\u4eca\u65e5\u9ad4\u91cd";
        public const string BossCleared = "Boss \u64ca\u7834";
        public const string RoomWithdrawn = "\u623f\u9593\u64a4\u9000";
        public const string NoticeBoard = "\u544a\u793a\u677f";
        public const string Tavern = "\u9152\u9928";
        public const string Blacksmith = "\u9435\u5320\u92ea";
        public const string HerbShop = "\u85e5\u8349\u92ea";
        public const string Fountain = "\u6708\u5149\u6cc9";
        public const string Church = "\u6559\u5802";
    }
}
