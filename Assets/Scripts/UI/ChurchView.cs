using System;
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
        var background = new ColorRect
        {
            Color = new Color(0.018f, 0.014f, 0.05f, 1),
        };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

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
        var header = HubHeaderBuilder.Build("返回", out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        return header;
    }

    private static Control BuildHero()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 180),
        };
        var layout = new VBoxContainer();
        layout.Alignment = BoxContainer.AlignmentMode.Center;
        layout.AddThemeConstantOverride("separation", 10);
        panel.AddChild(layout);

        var title = CreateLabel("教堂", 58, HorizontalAlignment.Center);
        layout.AddChild(title);
        var subtitle = CreateLabel("人物委託・長期故事", 30, HorizontalAlignment.Center);
        layout.AddChild(subtitle);
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
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 470),
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 26);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_right", 26);
        margin.AddThemeConstantOverride("margin_bottom", 22);
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

        _primaryButton = CreateActionButton(string.Empty, 32);
        _primaryButton.Pressed += HandlePrimaryAction;
        requesterLayout.AddChild(_primaryButton);
        _abandonButton = CreateActionButton("放棄誓約", 26);
        _abandonButton.Pressed += () => OathAbandoned?.Invoke();
        requesterLayout.AddChild(_abandonButton);
        return panel;
    }

    private Control BuildReturnButton()
    {
        var button = CreateActionButton("返回城鎮", 40);
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
        _dialogSkipButton = CreateActionButton("跳過", 25);
        _dialogSkipButton.Pressed += SkipDialog;
        smallButtons.AddChild(_dialogSkipButton);
        _dialogAutoButton = CreateActionButton("自動", 25);
        _dialogAutoButton.Pressed += ToggleAutoDialog;
        smallButtons.AddChild(_dialogAutoButton);
        _dialogNextButton = CreateActionButton("下一句", 25);
        _dialogNextButton.Pressed += AdvanceDialog;
        smallButtons.AddChild(_dialogNextButton);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 14);
        layout.AddChild(actionRow);
        _dialogAcceptButton = CreateActionButton("接受委託", 33);
        _dialogAcceptButton.Pressed += AcceptSelectedFromDialog;
        actionRow.AddChild(_dialogAcceptButton);
        var laterButton = CreateActionButton("稍後再說", 33);
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
        ClearChildren(_questGrid);
        foreach (var card in _model.Cards)
        {
            _questGrid.AddChild(CreateQuestCard(card));
        }

        RefreshDetail();
    }

    private Button CreateQuestCard(ChurchQuestCardViewModel card)
    {
        var selected = card.Id == (_selectedQuestId ?? _model.SelectedQuestId);
        var button = new Button
        {
            Text = $"{(selected ? "✓ " : string.Empty)}{card.Requester}\n{card.Title}\n\n{GetIconText(card.IconType)}\n{card.StatusLabel}\n{card.Progress}/{card.RequiredAmount}",
            CustomMinimumSize = new Vector2(0, 250),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Disabled = false,
        };
        button.AddThemeFontSizeOverride("font_size", 24);
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

        _detailTitle.Text = $"✦ {detail.Title} ✦";
        _detailText.Text = $"{detail.Description}\n\n委託人：{detail.Requester}";
        _requirement.Text = $"任務需求\n{detail.RequirementText}  {detail.Progress}/{detail.RequiredAmount}";
        _reward.Text = $"獎勵\n{detail.RewardGold} Gold\n稱號：{detail.RewardTitle}";
        _requesterPortrait.Text = $"{GetIconText(FindSelectedCard()?.IconType ?? string.Empty)}\n{detail.Requester}";
        _requesterName.Text = detail.Requester;
        _primaryButton.Text = detail.CanClaim
            ? "領取獎勵"
            : detail.CanAccept
                ? "接取委託"
                : string.IsNullOrWhiteSpace(detail.DisabledReason)
                    ? "進行中"
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
            new Core.Models.PlayerState(),
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
            _dialogAutoButton.Text = "停止";
            return;
        }

        _dialogAutoTimer.Stop();
        _dialogAutoButton.Text = "自動";
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
        _dialogAutoButton.Text = "自動";
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
            ? "若你願意，請接下這份委託。"
            : detail.DialogueLines[Math.Clamp(_dialogIndex, 0, detail.DialogueLines.Count - 1)];
        _dialogAcceptButton.Disabled = !_dialogRead;
        _dialogNextButton.Disabled = _dialogRead;
    }

    private static Button CreateActionButton(string text, int fontSize)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 74),
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
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

    private static string GetIconText(string iconType)
    {
        return iconType switch
        {
            "person" => "人物",
            "sword" => "長劍",
            "moon" => "月光",
            "herb" => "藥草",
            "shield" => "守衛",
            "letter" => "書信",
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
}
