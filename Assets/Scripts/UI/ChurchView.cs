using System;
using DungeonFit.Core.Models;
using DungeonFit.Gameplay;
using Godot;

namespace DungeonFit.UI;

public partial class ChurchView : Control
{
    public event Action? BackToTownRequested;
    public event Action<string>? OathAccepted;
    public event Action? OathAbandoned;
    public event Action? OathRewardClaimed;

    private ChurchViewModel _model = null!;
    private string? _selectedQuestId;
    private HubHeaderControls _header = null!;
    private GridContainer _questGrid = null!;
    private Label _detailTitle = null!;
    private Label _detailText = null!;
    private Label _requirement = null!;
    private Label _reward = null!;
    private Label _requesterPortrait = null!;
    private Label _requesterName = null!;
    private Button _primaryButton = null!;
    private Button _abandonButton = null!;
    private PanelContainer _dialogOverlay = null!;
    private Label _dialogName = null!;
    private Label _dialogPortrait = null!;
    private Label _dialogText = null!;
    private Button _dialogNextButton = null!;
    private Button _dialogSkipButton = null!;
    private Button _dialogAutoButton = null!;
    private Button _dialogAcceptButton = null!;
    private Timer _dialogAutoTimer = null!;
    private int _dialogIndex;
    private bool _dialogRead;

    public override void _Ready()
    {
        BuildLayout();
        Refresh();
    }

    public void Initialize(ChurchViewModel model)
    {
        _model = model;
        _selectedQuestId = model.SelectedQuestId;

        if (IsNodeReady())
        {
            Refresh();
        }
    }

    private void BuildLayout()
    {
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.CommonBackground);

        var safeMargin = new MarginContainer();
        safeMargin.SetAnchorsPreset(LayoutPreset.FullRect);
        safeMargin.AddThemeConstantOverride("margin_left", 34);
        safeMargin.AddThemeConstantOverride("margin_top", 36);
        safeMargin.AddThemeConstantOverride("margin_right", 34);
        safeMargin.AddThemeConstantOverride("margin_bottom", 36);
        AddChild(safeMargin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 16);
        safeMargin.AddChild(layout);

        layout.AddChild(BuildHeader());
        layout.AddChild(BuildHero());
        layout.AddChild(BuildQuestGrid());
        layout.AddChild(BuildDetailPanel());
        layout.AddChild(BuildReturnButton());
        BuildDialogOverlay();
    }

    private Control BuildHeader()
    {
        var header = HubHeaderBuilder.Build(Text.BackShort, out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        return header;
    }

    private static Control BuildHero()
    {
        var panel = CreatePanel(180, UiPanelStyle.Main);
        var layout = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        layout.AddThemeConstantOverride("separation", 10);
        panel.AddChild(layout);

        layout.AddChild(CreateLabel(Text.Title, 58, HorizontalAlignment.Center));
        layout.AddChild(CreateLabel(Text.Subtitle, 30, HorizontalAlignment.Center));
        return panel;
    }

    private Control BuildQuestGrid()
    {
        _questGrid = new GridContainer
        {
            Columns = 3,
            CustomMinimumSize = new Vector2(0, 540),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _questGrid.AddThemeConstantOverride("h_separation", 14);
        _questGrid.AddThemeConstantOverride("v_separation", 14);
        return _questGrid;
    }

    private Control BuildDetailPanel()
    {
        var panel = CreatePanel(470, UiPanelStyle.Card);

        var margin = CreateMargin(26, 22);
        panel.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 22);
        margin.AddChild(row);

        var textLayout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        textLayout.AddThemeConstantOverride("separation", 10);
        row.AddChild(textLayout);

        _detailTitle = CreateLabel(string.Empty, 36);
        textLayout.AddChild(_detailTitle);
        _detailText = CreateLabel(string.Empty, 24);
        _detailText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textLayout.AddChild(_detailText);
        _requirement = CreateLabel(string.Empty, 25);
        _requirement.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textLayout.AddChild(_requirement);
        _reward = CreateLabel(string.Empty, 25);
        _reward.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textLayout.AddChild(_reward);

        var requesterLayout = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(260, 0),
        };
        requesterLayout.AddThemeConstantOverride("separation", 12);
        row.AddChild(requesterLayout);

        _requesterPortrait = CreateLabel(string.Empty, 28, HorizontalAlignment.Center);
        _requesterPortrait.CustomMinimumSize = new Vector2(250, 190);
        _requesterPortrait.VerticalAlignment = VerticalAlignment.Center;
        requesterLayout.AddChild(_requesterPortrait);
        _requesterName = CreateLabel(string.Empty, 28, HorizontalAlignment.Center);
        requesterLayout.AddChild(_requesterName);

        _primaryButton = CreateActionButton(string.Empty, 32, UiButtonStyle.Primary);
        _primaryButton.Pressed += HandlePrimaryAction;
        requesterLayout.AddChild(_primaryButton);
        _abandonButton = CreateActionButton(Text.Abandon, 26, UiButtonStyle.Danger);
        _abandonButton.Pressed += () => OathAbandoned?.Invoke();
        requesterLayout.AddChild(_abandonButton);
        return panel;
    }

    private Control BuildReturnButton()
    {
        var button = CreateActionButton(Text.BackTown, 40, UiButtonStyle.Secondary);
        button.CustomMinimumSize = new Vector2(0, 96);
        button.Pressed += () => BackToTownRequested?.Invoke();
        return button;
    }

    private void BuildDialogOverlay()
    {
        _dialogOverlay = new PanelContainer
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _dialogOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
        DungeonFitUi.ApplyPanel(_dialogOverlay, UiPanelStyle.Overlay);
        AddChild(_dialogOverlay);
        _dialogOverlay.MoveToFront();

        var outer = new MarginContainer();
        outer.AddThemeConstantOverride("margin_left", 34);
        outer.AddThemeConstantOverride("margin_top", 150);
        outer.AddThemeConstantOverride("margin_right", 34);
        outer.AddThemeConstantOverride("margin_bottom", 80);
        _dialogOverlay.AddChild(outer);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 16);
        outer.AddChild(layout);

        _dialogPortrait = CreateLabel(string.Empty, 34, HorizontalAlignment.Center);
        _dialogPortrait.CustomMinimumSize = new Vector2(0, 280);
        _dialogPortrait.VerticalAlignment = VerticalAlignment.Center;
        layout.AddChild(_dialogPortrait);
        _dialogName = CreateLabel(string.Empty, 32, HorizontalAlignment.Center);
        layout.AddChild(_dialogName);
        _dialogText = CreateLabel(string.Empty, 31);
        _dialogText.CustomMinimumSize = new Vector2(0, 230);
        _dialogText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        layout.AddChild(_dialogText);

        var smallButtons = new HBoxContainer();
        smallButtons.AddThemeConstantOverride("separation", 14);
        layout.AddChild(smallButtons);
        _dialogSkipButton = CreateActionButton(Text.Skip, 25, UiButtonStyle.Secondary);
        _dialogSkipButton.Pressed += SkipDialog;
        smallButtons.AddChild(_dialogSkipButton);
        _dialogAutoButton = CreateActionButton(Text.Auto, 25, UiButtonStyle.Secondary);
        _dialogAutoButton.Pressed += ToggleAutoDialog;
        smallButtons.AddChild(_dialogAutoButton);
        _dialogNextButton = CreateActionButton(Text.Next, 25, UiButtonStyle.Primary);
        _dialogNextButton.Pressed += AdvanceDialog;
        smallButtons.AddChild(_dialogNextButton);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 14);
        layout.AddChild(actionRow);
        _dialogAcceptButton = CreateActionButton(Text.AcceptOath, 33, UiButtonStyle.Primary);
        _dialogAcceptButton.Pressed += AcceptSelectedFromDialog;
        actionRow.AddChild(_dialogAcceptButton);
        var laterButton = CreateActionButton(Text.Later, 33, UiButtonStyle.Secondary);
        laterButton.Pressed += HideDialog;
        actionRow.AddChild(laterButton);

        _dialogAutoTimer = new Timer
        {
            WaitTime = 1.2,
            OneShot = false,
        };
        _dialogAutoTimer.Timeout += AdvanceDialog;
        AddChild(_dialogAutoTimer);
    }

    private void Refresh()
    {
        if (_model is null || _questGrid is null)
        {
            return;
        }

        HubHeaderBuilder.Refresh(
            _header,
            _model.Player.Level,
            _model.Player.Experience,
            _model.Player.ExperienceToNextLevel,
            _model.Player.Gold);
        RefreshCardsOnly();
        RefreshDetail();
    }

    private Button CreateQuestCard(ChurchQuestCardViewModel card)
    {
        var selected = card.Id == (_selectedQuestId ?? _model.SelectedQuestId);
        var button = new Button
        {
            Text = $"{(selected ? Text.SelectedMarker : string.Empty)}{card.Requester}\n{card.Title}\n\n{GetIconText(card.IconType)}\n{card.StatusLabel}\n{card.Progress}/{card.RequiredAmount}",
            CustomMinimumSize = new Vector2(0, 250),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Disabled = false,
        };
        button.AddThemeFontSizeOverride("font_size", 24);
        DungeonFitUi.ApplyButton(button, selected ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
        button.Pressed += () =>
        {
            _selectedQuestId = card.Id;
            RefreshDetail();
            RefreshCardsOnly();
        };
        return button;
    }

    private void RefreshCardsOnly()
    {
        ClearChildren(_questGrid);
        foreach (var card in _model.Cards)
        {
            _questGrid.AddChild(CreateQuestCard(card));
        }
    }

    private void RefreshDetail()
    {
        var detail = FindSelectedDetail();
        if (detail is null)
        {
            return;
        }

        _detailTitle.Text = detail.Title;
        _detailText.Text = $"{detail.Description}\n\n委託人：{detail.Requester}";
        _requirement.Text = $"{Text.Requirement}\n{detail.RequirementText}  {detail.Progress}/{detail.RequiredAmount}";
        _reward.Text = $"{Text.Reward}\n{detail.RewardGold} Gold\n稱號：{detail.RewardTitle}";
        _requesterPortrait.Text = $"{GetIconText(FindSelectedCard()?.IconType ?? string.Empty)}\n{detail.Requester}";
        _requesterName.Text = detail.Requester;
        _primaryButton.Text = detail.CanClaim
            ? Text.ClaimReward
            : detail.CanAccept
                ? Text.AcceptOath
                : string.IsNullOrWhiteSpace(detail.DisabledReason)
                    ? Text.Unavailable
                    : detail.DisabledReason;
        _primaryButton.Disabled = !detail.CanAccept && !detail.CanClaim;
        _abandonButton.Visible = detail.CanAbandon;
        _abandonButton.Disabled = !detail.CanAbandon;
    }

    private ChurchQuestDetailViewModel? FindSelectedDetail()
    {
        var selectedId = _selectedQuestId ?? _model.SelectedQuestId;
        if (selectedId == _model.Detail?.Id)
        {
            return _model.Detail;
        }

        var selectedCard = FindSelectedCard();
        if (selectedCard is null)
        {
            return _model.Detail;
        }

        var tempModel = new ChurchViewModel(
            new PlayerState(),
            _model.ActiveQuest,
            _model.ClaimedQuestIds,
            _model.UnlockedTitles,
            selectedCard.Id);
        return tempModel.Detail;
    }

    private ChurchQuestCardViewModel? FindSelectedCard()
    {
        var selectedId = _selectedQuestId ?? _model.SelectedQuestId;
        foreach (var card in _model.Cards)
        {
            if (card.Id == selectedId)
            {
                return card;
            }
        }

        return _model.SelectedQuest;
    }

    private void HandlePrimaryAction()
    {
        var detail = FindSelectedDetail();
        if (detail is null)
        {
            return;
        }

        if (detail.CanClaim)
        {
            OathRewardClaimed?.Invoke();
            return;
        }

        if (detail.CanAccept)
        {
            ShowDialog(detail);
        }
    }

    private void ShowDialog(ChurchQuestDetailViewModel detail)
    {
        _dialogIndex = 0;
        _dialogRead = detail.DialogueLines.Count == 0;
        _dialogName.Text = detail.Requester;
        _dialogPortrait.Text = $"{detail.Requester}\n{GetIconText(FindSelectedCard()?.IconType ?? string.Empty)}";
        _dialogOverlay.Visible = true;
        _dialogOverlay.MoveToFront();
        RefreshDialogLine();
    }

    private void AdvanceDialog()
    {
        var detail = FindSelectedDetail();
        if (detail is null)
        {
            return;
        }

        if (_dialogIndex < detail.DialogueLines.Count - 1)
        {
            _dialogIndex++;
        }
        else
        {
            _dialogRead = true;
            _dialogAutoTimer.Stop();
        }

        RefreshDialogLine();
    }

    private void SkipDialog()
    {
        var detail = FindSelectedDetail();
        _dialogIndex = Math.Max(0, (detail?.DialogueLines.Count ?? 1) - 1);
        _dialogRead = true;
        _dialogAutoTimer.Stop();
        RefreshDialogLine();
    }

    private void ToggleAutoDialog()
    {
        if (_dialogAutoTimer.IsStopped())
        {
            _dialogAutoTimer.Start();
            _dialogAutoButton.Text = Text.StopAuto;
            return;
        }

        _dialogAutoTimer.Stop();
        _dialogAutoButton.Text = Text.Auto;
    }

    private void AcceptSelectedFromDialog()
    {
        if (!_dialogRead)
        {
            return;
        }

        var detail = FindSelectedDetail();
        if (detail is null)
        {
            return;
        }

        HideDialog();
        OathAccepted?.Invoke(detail.Id);
    }

    private void HideDialog()
    {
        _dialogAutoTimer.Stop();
        _dialogAutoButton.Text = Text.Auto;
        _dialogOverlay.Visible = false;
    }

    private void RefreshDialogLine()
    {
        var detail = FindSelectedDetail();
        if (detail is null)
        {
            return;
        }

        _dialogText.Text = detail.DialogueLines.Count == 0
            ? Text.NoDialogue
            : detail.DialogueLines[Math.Clamp(_dialogIndex, 0, detail.DialogueLines.Count - 1)];
        _dialogAcceptButton.Disabled = !_dialogRead;
        _dialogNextButton.Disabled = _dialogRead;
    }

    private static Button CreateActionButton(string text, int fontSize, UiButtonStyle style)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 74),
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        DungeonFitUi.ApplyButton(button, style);
        return button;
    }

    private static Label CreateLabel(
        string text,
        int fontSize,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static PanelContainer CreatePanel(int height, UiPanelStyle style)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, height) };
        DungeonFitUi.ApplyPanel(panel, style);
        return panel;
    }

    private static MarginContainer CreateMargin(int horizontal, int vertical)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", horizontal);
        margin.AddThemeConstantOverride("margin_top", vertical);
        margin.AddThemeConstantOverride("margin_right", horizontal);
        margin.AddThemeConstantOverride("margin_bottom", vertical);
        return margin;
    }

    private static string GetIconText(string iconType)
    {
        return iconType switch
        {
            "person" => "鎮民",
            "sword" => "月刃",
            "moon" => "星燈",
            "herb" => "藥草",
            "shield" => "守衛",
            "letter" => "信件",
            _ => "誓約",
        };
    }

    private static void ClearChildren(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static class Text
    {
        public const string BackShort = "返回";
        public const string BackTown = "返回城鎮";
        public const string Title = "教堂";
        public const string Subtitle = "長期誓約、祝禱與稱號";
        public const string Abandon = "放棄誓約";
        public const string Requirement = "誓約目標";
        public const string Reward = "完成獎勵";
        public const string ClaimReward = "領取獎勵";
        public const string AcceptOath = "接受誓約";
        public const string Unavailable = "尚不可用";
        public const string Skip = "跳過";
        public const string Auto = "自動";
        public const string StopAuto = "停止";
        public const string Next = "下一句";
        public const string Later = "稍後再說";
        public const string SelectedMarker = "[選取]\n";
        public const string NoDialogue = "對方只是安靜地點了點頭。";
    }
}
