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
        Extend,
        Dismantle,
    }

    public event Action? BackToTownRequested;
    public event Action<string>? EnhanceRequested;
    public event Action<string>? ExtendLevelRangeRequested;
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
    private Button _extendModeButton = null!;
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
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.CommonBackground);

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

        var header = HubHeaderBuilder.Build(Text.BackShort, out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(header);

        var hero = CreatePanel(220, UiPanelStyle.Main);
        layout.AddChild(hero);
        var heroLayout = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        heroLayout.AddThemeConstantOverride("separation", 8);
        hero.AddChild(heroLayout);
        heroLayout.AddChild(CreateCenteredLabel(Text.Title, 58));
        heroLayout.AddChild(CreateCenteredLabel(Text.Subtitle, 30));
        heroLayout.AddChild(CreateCenteredLabel(Text.Description, 24));

        var middle = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        middle.AddThemeConstantOverride("separation", 18);
        layout.AddChild(middle);

        var inventoryPanel = CreatePanel(410, UiPanelStyle.Card);
        inventoryPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        middle.AddChild(inventoryPanel);
        var inventoryMargin = CreateMargin(18, 16);
        inventoryPanel.AddChild(inventoryMargin);
        _inventoryGrid = new GridContainer { Columns = 2 };
        _inventoryGrid.AddThemeConstantOverride("h_separation", 12);
        _inventoryGrid.AddThemeConstantOverride("v_separation", 12);
        inventoryMargin.AddChild(_inventoryGrid);

        var detailPanel = CreatePanel(410, UiPanelStyle.Card);
        detailPanel.CustomMinimumSize = new Vector2(430, 410);
        detailPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        middle.AddChild(detailPanel);
        var detailMargin = CreateMargin(22, 20);
        detailPanel.AddChild(detailMargin);
        var detailLayout = new VBoxContainer();
        detailLayout.AddThemeConstantOverride("separation", 14);
        detailMargin.AddChild(detailLayout);
        _detailTitle = CreateLabel(string.Empty, 34);
        detailLayout.AddChild(_detailTitle);
        _detailMeta = CreateLabel(string.Empty, 25);
        detailLayout.AddChild(_detailMeta);
        _detailStats = CreateLabel(string.Empty, 25);
        detailLayout.AddChild(_detailStats);

        var modeRow = new HBoxContainer();
        modeRow.AddThemeConstantOverride("separation", 12);
        layout.AddChild(modeRow);
        _enhanceModeButton = CreateButton(Text.EnhanceMode, 0, 76, 30, UiButtonStyle.Primary);
        _enhanceModeButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _enhanceModeButton.Pressed += () => SetMode(BlacksmithMode.Enhance);
        modeRow.AddChild(_enhanceModeButton);
        _extendModeButton = CreateButton(Text.ExtendMode, 0, 76, 30, UiButtonStyle.Secondary);
        _extendModeButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _extendModeButton.Pressed += () => SetMode(BlacksmithMode.Extend);
        modeRow.AddChild(_extendModeButton);
        _dismantleModeButton = CreateButton(Text.DismantleMode, 0, 76, 30, UiButtonStyle.Secondary);
        _dismantleModeButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _dismantleModeButton.Pressed += () => SetMode(BlacksmithMode.Dismantle);
        modeRow.AddChild(_dismantleModeButton);

        var operationPanel = CreatePanel(220, UiPanelStyle.Card);
        layout.AddChild(operationPanel);
        var operationMargin = CreateMargin(24, 18);
        operationPanel.AddChild(operationMargin);
        var operationLayout = new VBoxContainer();
        operationLayout.AddThemeConstantOverride("separation", 12);
        operationMargin.AddChild(operationLayout);
        _operationPreview = CreateCenteredLabel(string.Empty, 28);
        operationLayout.AddChild(_operationPreview);
        _operationHint = CreateCenteredLabel(string.Empty, 23);
        operationLayout.AddChild(_operationHint);
        _actionButton = CreateButton(string.Empty, 0, 84, 34, UiButtonStyle.Primary);
        _actionButton.Pressed += RunSelectedAction;
        operationLayout.AddChild(_actionButton);
    }

    private void SetMode(BlacksmithMode mode)
    {
        _mode = mode;
        Refresh();
    }

    private void Refresh()
    {
        HubHeaderBuilder.Refresh(_header, _model.Character.Level, _model.Character.Experience, _model.Character.ExperienceToNextLevel, _model.Character.Gold);
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
            _inventoryGrid.Columns = 1;
            var empty = CreateCenteredLabel(Text.EmptyInventory, 30);
            empty.CustomMinimumSize = new Vector2(0, 340);
            empty.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            empty.SizeFlagsVertical = SizeFlags.ExpandFill;
            empty.VerticalAlignment = VerticalAlignment.Center;
            _inventoryGrid.AddChild(empty);
            return;
        }

        _inventoryGrid.Columns = 2;
        foreach (var item in _model.Items)
        {
            var button = CreateButton(BuildInventoryText(item), 0, 116, 21, UiButtonStyle.Secondary);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.Disabled = item.Id == _selectedItemId;
            var itemId = item.Id;
            button.Pressed += () =>
            {
                _selectedItemId = itemId;
                Refresh();
            };
            _inventoryGrid.AddChild(button);
        }
    }

    private void RefreshDetail()
    {
        var item = SelectedItem;
        if (item is null)
        {
            _detailTitle.Text = Text.NoItemTitle;
            _detailMeta.Text = Text.NoItemMeta;
            _detailStats.Text = string.Empty;
            return;
        }

        _detailTitle.Text = $"{item.DisplayName}  +{item.EnhancementLevel}";
        _detailMeta.Text = $"{item.Rarity} | {item.SlotLabel} | 戰力 {item.EffectivePower}/{item.Power}";
        var markers = item.IsEquipped ? Text.Equipped : Text.NotEquipped;
        if (item.IsLocked)
        {
            markers += $" / {Text.Locked}";
        }

        var levelState = item.IsWithinRecommendedLevel
            ? Text.LevelRangeOk
            : Text.LevelRangeDecayed;
        var modifiers = item.ModifierLines.Count == 0
            ? Text.NoModifiers
            : string.Join("\n", item.ModifierLines);
        _detailStats.Text = $"{markers}\n{Text.EnhancementLevel} {item.EnhancementLevel} / {item.MaxEnhancementLevel}\n{Text.LevelRange} {item.RecommendedLevelMin}-{item.EffectiveRecommendedLevelMax}  {levelState}\n{modifiers}";
    }

    private void RefreshOperation()
    {
        _enhanceModeButton.Disabled = _mode == BlacksmithMode.Enhance;
        _extendModeButton.Disabled = _mode == BlacksmithMode.Extend;
        _dismantleModeButton.Disabled = _mode == BlacksmithMode.Dismantle;
        var item = SelectedItem;

        switch (_mode)
        {
            case BlacksmithMode.Enhance:
                RefreshEnhanceOperation(item);
                return;
            case BlacksmithMode.Extend:
                RefreshExtendOperation(item);
                return;
            default:
                RefreshDismantleOperation(item);
                return;
        }
    }

    private void RefreshEnhanceOperation(BlacksmithItemViewModel? item)
    {
        var cost = item is null ? 0 : BlacksmithRules.GetEnhancementCost(item.EnhancementLevel);
        var canRun = item is not null && item.EnhancementLevel < item.MaxEnhancementLevel && _model.Character.Gold >= cost;
        _operationPreview.Text = item is null ? Text.SelectItemPrompt : string.Format(Text.EnhancePreviewFormat, item.Power, item.Power + 1);
        _operationHint.Text = item is null ? Text.EnhanceHint : canRun ? string.Format(Text.CostFormat, cost) : BuildEnhanceDisabledReason(item, cost);
        _actionButton.Text = Text.RunEnhance;
        _actionButton.Disabled = !canRun;
    }

    private void RefreshExtendOperation(BlacksmithItemViewModel? item)
    {
        var cost = item is null ? 0 : BlacksmithRules.GetLevelExtensionCost(item.LevelExtension);
        var canRun = item is not null && item.LevelExtension < item.MaxLevelExtension && _model.Character.Gold >= cost;
        _operationPreview.Text = item is null ? Text.SelectItemPrompt : string.Format(Text.ExtendPreviewFormat, item.EffectiveRecommendedLevelMax, item.EffectiveRecommendedLevelMax + 1);
        _operationHint.Text = item is null ? Text.ExtendHint : canRun ? string.Format(Text.CostFormat, cost) : BuildExtendDisabledReason(item, cost);
        _actionButton.Text = Text.RunExtend;
        _actionButton.Disabled = !canRun;
    }

    private void RefreshDismantleOperation(BlacksmithItemViewModel? item)
    {
        var canRun = item is not null && item.EnhancementLevel > 0;
        var refund = item is null ? 0 : BlacksmithRules.GetDismantleRefund(item.EnhancementLevel);
        _operationPreview.Text = item is null ? Text.SelectItemPrompt : string.Format(Text.DismantlePreviewFormat, item.EnhancementLevel);
        _operationHint.Text = item is null ? Text.DismantleHint : canRun ? string.Format(Text.RefundFormat, refund) : Text.CannotDismantleZero;
        _actionButton.Text = Text.RunDismantle;
        _actionButton.Disabled = !canRun;
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

        switch (_mode)
        {
            case BlacksmithMode.Enhance:
                EnhanceRequested?.Invoke(item.Id);
                return;
            case BlacksmithMode.Extend:
                ExtendLevelRangeRequested?.Invoke(item.Id);
                return;
            default:
                DismantleRequested?.Invoke(item.Id);
                return;
        }
    }

    private static string BuildInventoryText(BlacksmithItemViewModel item)
    {
        var marker = item.IsEquipped ? $"{Text.Equipped} " : string.Empty;
        return $"{marker}{item.DisplayName}\n{item.SlotLabel} / {item.Rarity} / +{item.EnhancementLevel}\n戰力 {item.EffectivePower}  Lv.{item.RecommendedLevelMin}-{item.EffectiveRecommendedLevelMax}";
    }

    private string BuildEnhanceDisabledReason(BlacksmithItemViewModel item, int cost)
    {
        if (item.EnhancementLevel >= item.MaxEnhancementLevel)
        {
            return Text.MaxEnhancementReached;
        }

        return _model.Character.Gold < cost ? string.Format(Text.NotEnoughGoldFormat, cost) : string.Empty;
    }

    private string BuildExtendDisabledReason(BlacksmithItemViewModel item, int cost)
    {
        if (item.LevelExtension >= item.MaxLevelExtension)
        {
            return Text.MaxExtensionReached;
        }

        return _model.Character.Gold < cost ? string.Format(Text.NotEnoughGoldFormat, cost) : string.Empty;
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

    private static Button CreateButton(string text, int width, int height, int fontSize, UiButtonStyle style)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, height),
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        DungeonFitUi.ApplyButton(button, style);
        return button;
    }

    private static class Text
    {
        public const string BackShort = "返回";
        public const string Title = "鐵匠鋪";
        public const string Subtitle = "強化、延長、拆解裝備";
        public const string Description = "消耗金幣提高戰力，或延長低階裝備的可用等級。";
        public const string EnhanceMode = "強化";
        public const string ExtendMode = "延長";
        public const string DismantleMode = "拆解";
        public const string EmptyInventory = "目前沒有可處理的裝備。";
        public const string NoItemTitle = "尚未選擇裝備";
        public const string NoItemMeta = "從左側選擇一件裝備查看詳細數值。";
        public const string Equipped = "已裝備";
        public const string NotEquipped = "未裝備";
        public const string Locked = "已鎖定";
        public const string NoModifiers = "沒有額外詞綴。";
        public const string EnhancementLevel = "強化等級";
        public const string LevelRange = "可用等級";
        public const string LevelRangeOk = "效果完整";
        public const string LevelRangeDecayed = "超出等級，效果衰減";
        public const string SelectItemPrompt = "請先選擇裝備";
        public const string EnhancePreviewFormat = "目前戰力 {0}  >>>  強化後戰力 {1}";
        public const string EnhanceHint = "選擇一件裝備後可消耗金幣強化。";
        public const string ExtendPreviewFormat = "目前可用到 Lv.{0}  >>>  延長後 Lv.{1}";
        public const string ExtendHint = "延長可用等級可解除高等級時的效果衰減。";
        public const string CostFormat = "消耗 {0} Gold";
        public const string RunEnhance = "執行強化";
        public const string RunExtend = "延長等級";
        public const string DismantlePreviewFormat = "目前 +{0}  >>>  拆解後 +0";
        public const string DismantleHint = "拆解強化等級，取回部分強化資源。";
        public const string RefundFormat = "返還 {0} Gold";
        public const string CannotDismantleZero = "這件裝備尚未強化，無法拆解。";
        public const string RunDismantle = "拆解強化";
        public const string MaxEnhancementReached = "這件裝備已達強化上限。";
        public const string MaxExtensionReached = "這件裝備已達延長上限。";
        public const string NotEnoughGoldFormat = "金幣不足，需要 {0} Gold。";
    }
}
