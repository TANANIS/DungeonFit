using System;
using System.Linq;
using DungeonFit.Gameplay;
using Godot;

namespace DungeonFit.UI;

public partial class BlacksmithView : Control
{
    private enum BlacksmithMode
    {
        Enhance,
        Dismantle,
    }

    public event Action? BackToTownRequested;
    public event Action<string>? EnhanceRequested;
    public event Action<string>? DismantleRequested;

    private BlacksmithViewModel _model = null!;
    private BlacksmithMode _mode = BlacksmithMode.Enhance;
    private string? _selectedItemId;
    private HubHeaderControls _header = null!;
    private GridContainer _inventoryGrid = null!;
    private Label _detailTitle = null!;
    private Label _detailMeta = null!;
    private Label _detailStats = null!;
    private Button _enhanceModeButton = null!;
    private Button _dismantleModeButton = null!;
    private Label _operationPreview = null!;
    private Label _operationHint = null!;
    private Button _actionButton = null!;

    public override void _Ready()
    {
        BuildUi();
        if (_model is not null)
        {
            Refresh();
        }
    }

    public void Initialize(BlacksmithViewModel model)
    {
        _model = model;
        _selectedItemId = model.SelectedItemId;
        if (IsNodeReady())
        {
            Refresh();
        }
    }

    private void BuildUi()
    {
        var background = new ColorRect
        {
            Color = new Color(0.035f, 0.025f, 0.02f, 1),
        };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var safe = new MarginContainer();
        safe.SetAnchorsPreset(LayoutPreset.FullRect);
        safe.AddThemeConstantOverride("margin_left", 38);
        safe.AddThemeConstantOverride("margin_top", 46);
        safe.AddThemeConstantOverride("margin_right", 38);
        safe.AddThemeConstantOverride("margin_bottom", 46);
        AddChild(safe);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 18);
        safe.AddChild(layout);

        var header = HubHeaderBuilder.Build("返回", out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(header);

        var hero = new PanelContainer { CustomMinimumSize = new Vector2(0, 250) };
        layout.AddChild(hero);
        var heroLayout = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        heroLayout.AddThemeConstantOverride("separation", 8);
        hero.AddChild(heroLayout);
        heroLayout.AddChild(CreateCenteredLabel("鐵匠鋪", 62));
        heroLayout.AddChild(CreateCenteredLabel("強化・拆除升級", 32));
        heroLayout.AddChild(CreateCenteredLabel("讓裝備在下一趟地城前更可靠。", 25));

        var middle = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        middle.AddThemeConstantOverride("separation", 18);
        layout.AddChild(middle);

        var inventoryPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 430),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        middle.AddChild(inventoryPanel);
        var inventoryMargin = CreateMargin(18, 16);
        inventoryPanel.AddChild(inventoryMargin);
        _inventoryGrid = new GridContainer { Columns = 2 };
        _inventoryGrid.AddThemeConstantOverride("h_separation", 12);
        _inventoryGrid.AddThemeConstantOverride("v_separation", 12);
        inventoryMargin.AddChild(_inventoryGrid);

        var detailPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(430, 430),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        middle.AddChild(detailPanel);
        var detailMargin = CreateMargin(22, 20);
        detailPanel.AddChild(detailMargin);
        var detailLayout = new VBoxContainer();
        detailLayout.AddThemeConstantOverride("separation", 14);
        detailMargin.AddChild(detailLayout);
        _detailTitle = CreateLabel(string.Empty, 36);
        detailLayout.AddChild(_detailTitle);
        _detailMeta = CreateLabel(string.Empty, 27);
        detailLayout.AddChild(_detailMeta);
        _detailStats = CreateLabel(string.Empty, 28);
        detailLayout.AddChild(_detailStats);

        var modeRow = new HBoxContainer();
        modeRow.AddThemeConstantOverride("separation", 16);
        layout.AddChild(modeRow);
        _enhanceModeButton = CreateButton("強化", 0, 82, 34);
        _enhanceModeButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _enhanceModeButton.Pressed += () =>
        {
            _mode = BlacksmithMode.Enhance;
            Refresh();
        };
        modeRow.AddChild(_enhanceModeButton);
        _dismantleModeButton = CreateButton("拆除升級", 0, 82, 34);
        _dismantleModeButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _dismantleModeButton.Pressed += () =>
        {
            _mode = BlacksmithMode.Dismantle;
            Refresh();
        };
        modeRow.AddChild(_dismantleModeButton);

        var operationPanel = new PanelContainer { CustomMinimumSize = new Vector2(0, 230) };
        layout.AddChild(operationPanel);
        var operationMargin = CreateMargin(24, 18);
        operationPanel.AddChild(operationMargin);
        var operationLayout = new VBoxContainer();
        operationLayout.AddThemeConstantOverride("separation", 12);
        operationMargin.AddChild(operationLayout);
        _operationPreview = CreateCenteredLabel(string.Empty, 31);
        operationLayout.AddChild(_operationPreview);
        _operationHint = CreateCenteredLabel(string.Empty, 24);
        operationLayout.AddChild(_operationHint);
        _actionButton = CreateButton(string.Empty, 0, 92, 38);
        _actionButton.Pressed += RunSelectedAction;
        operationLayout.AddChild(_actionButton);
    }

    private void Refresh()
    {
        HubHeaderBuilder.Refresh(
            _header,
            _model.Character.Level,
            _model.Character.Experience,
            _model.Character.ExperienceToNextLevel,
            _model.Character.Gold);
        BuildInventoryGrid();
        RefreshDetail();
        RefreshOperation();
    }

    private void BuildInventoryGrid()
    {
        foreach (var child in _inventoryGrid.GetChildren())
        {
            child.QueueFree();
        }

        if (_model.Items.Count == 0)
        {
            _inventoryGrid.AddChild(CreateLabel("目前沒有裝備。", 30));
            return;
        }

        foreach (var item in _model.Items)
        {
            var button = CreateButton(BuildInventoryText(item), 0, 112, 22);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.Disabled = item.Id == _selectedItemId;
            var itemId = item.Id;
            button.Pressed += () =>
            {
                _selectedItemId = itemId;
                SelectItemLocally();
            };
            _inventoryGrid.AddChild(button);
        }
    }

    private void SelectItemLocally()
    {
        var selected = _model.Items.FirstOrDefault(item => item.Id == _selectedItemId);
        if (selected is null)
        {
            return;
        }

        Refresh();
    }

    private void RefreshDetail()
    {
        var item = SelectedItem;
        if (item is null)
        {
            _detailTitle.Text = "選擇裝備";
            _detailMeta.Text = "背包目前沒有可強化的裝備。";
            _detailStats.Text = string.Empty;
            return;
        }

        _detailTitle.Text = $"{item.DisplayName}  +{item.EnhancementLevel}";
        _detailMeta.Text = $"{item.Rarity} | {item.SlotLabel} | Power {item.Power}";
        var markers = item.IsEquipped ? "已裝備" : "背包中";
        if (item.IsLocked)
        {
            markers += " / 已鎖定";
        }

        var modifiers = item.ModifierLines.Count == 0
            ? "沒有額外詞綴。"
            : string.Join("\n", item.ModifierLines);
        _detailStats.Text = $"{markers}\n強化等級 {item.EnhancementLevel} / {item.MaxEnhancementLevel}\n{modifiers}";
    }

    private void RefreshOperation()
    {
        _enhanceModeButton.Disabled = _mode == BlacksmithMode.Enhance;
        _dismantleModeButton.Disabled = _mode == BlacksmithMode.Dismantle;
        var item = SelectedItem;

        if (_mode == BlacksmithMode.Enhance)
        {
            _operationPreview.Text = item is null
                ? "請先選擇裝備"
                : $"目前 Power {item.Power}  >>>  強化後 Power {item.Power + 1}";
            var enhancementCost = item is null ? 0 : BlacksmithRules.GetEnhancementCost(item.EnhancementLevel);
            var canEnhance = CanEnhanceSelectedItem(item, enhancementCost);
            _operationHint.Text = item is null
                ? "選擇一件裝備後即可查看強化費用。"
                : canEnhance
                    ? $"消耗：{enhancementCost} Gold"
                    : BuildEnhanceDisabledReason(item, enhancementCost);
            _actionButton.Text = "進行強化";
            _actionButton.Disabled = !canEnhance;
            return;
        }

        var canDismantle = item is not null && item.EnhancementLevel > 0;
        var refund = item is null ? 0 : BlacksmithRules.GetDismantleRefund(item.EnhancementLevel);
        _operationPreview.Text = item is null
            ? "請先選擇裝備"
            : $"目前 +{item.EnhancementLevel}  >>>  拆除後 +0";
        _operationHint.Text = item is null
            ? "選擇一件裝備後即可拆除升級。"
            : canDismantle
                ? $"返還：{refund} Gold"
                : "這件裝備尚未強化。";
        _actionButton.Text = "拆除升級";
        _actionButton.Disabled = !canDismantle;
    }

    private BlacksmithItemViewModel? SelectedItem =>
        _model.Items.FirstOrDefault(item => item.Id == _selectedItemId) ?? _model.SelectedItem;

    private void RunSelectedAction()
    {
        var item = SelectedItem;
        if (item is null)
        {
            return;
        }

        if (_mode == BlacksmithMode.Enhance)
        {
            EnhanceRequested?.Invoke(item.Id);
            return;
        }

        DismantleRequested?.Invoke(item.Id);
    }

    private static string BuildInventoryText(BlacksmithItemViewModel item)
    {
        var marker = item.IsEquipped ? "已裝備 " : string.Empty;
        return $"{marker}{item.DisplayName}\n{item.SlotLabel} / {item.Rarity} / +{item.EnhancementLevel}\nPower {item.Power}";
    }

    private bool CanEnhanceSelectedItem(BlacksmithItemViewModel? item, int cost)
    {
        return item is not null &&
            item.EnhancementLevel < BlacksmithRules.MaxEnhancementLevel &&
            _model.Character.Gold >= cost;
    }

    private string BuildEnhanceDisabledReason(BlacksmithItemViewModel item, int cost)
    {
        if (item.EnhancementLevel >= BlacksmithRules.MaxEnhancementLevel)
        {
            return "這件裝備已達強化上限。";
        }

        if (_model.Character.Gold < cost)
        {
            return $"金幣不足，需要 {cost} Gold。";
        }

        return string.Empty;
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

    private static Label CreateLabel(string text, int fontSize)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static Label CreateCenteredLabel(string text, int fontSize)
    {
        var label = CreateLabel(text, fontSize);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        return label;
    }

    private static Button CreateButton(string text, int width, int height, int fontSize)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, height),
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        return button;
    }

}
