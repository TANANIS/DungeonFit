using System;
using DungeonFit.Gameplay;
using Godot;

namespace DungeonFit.UI;

public partial class HerbShopView : Control
{
    public event Action? BackToTownRequested;
    public event Action? BasicHealRequested;
    public event Action? FullHealRequested;
    public event Action? PotionPurchaseRequested;

    private HerbShopViewModel _model = null!;
    private HubHeaderControls _header = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private Button _basicHealButton = null!;
    private Button _fullHealButton = null!;
    private Button _potionButton = null!;
    private Label _potionStatus = null!;

    public override void _Ready()
    {
        BuildUi();
        if (_model is not null)
        {
            Refresh();
        }
    }

    public void Initialize(HerbShopViewModel model)
    {
        _model = model;
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
        layout.AddThemeConstantOverride("separation", 20);
        safe.AddChild(layout);

        var header = HubHeaderBuilder.Build(Text.BackShort, out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(header);

        var hero = CreatePanel(310, UiPanelStyle.Main);
        layout.AddChild(hero);
        var heroLayout = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        heroLayout.AddThemeConstantOverride("separation", 12);
        hero.AddChild(heroLayout);
        heroLayout.AddChild(CreateCenteredLabel(Text.Title, 58));
        heroLayout.AddChild(CreateCenteredLabel(Text.Subtitle, 30));
        heroLayout.AddChild(CreateCenteredLabel(Text.Description, 25));

        var heal = CreatePanel(260, UiPanelStyle.Card);
        layout.AddChild(heal);
        var healMargin = CreateMargin(26, 22);
        heal.AddChild(healMargin);
        var healLayout = new VBoxContainer();
        healLayout.AddThemeConstantOverride("separation", 14);
        healMargin.AddChild(healLayout);
        healLayout.AddChild(CreateLabel(Text.HealTitle, 38));
        _hpLabel = CreateLabel(string.Empty, 30);
        healLayout.AddChild(_hpLabel);
        _hpBar = new ProgressBar { ShowPercentage = false, CustomMinimumSize = new Vector2(0, 24) };
        DungeonFitUi.ApplyProgressBar(_hpBar, new Color(0.48f, 0.82f, 0.58f));
        healLayout.AddChild(_hpBar);

        var healButtons = new HBoxContainer();
        healButtons.AddThemeConstantOverride("separation", 18);
        healLayout.AddChild(healButtons);
        _basicHealButton = CreateButton(Text.BasicHeal, 0, 100, 28, UiButtonStyle.Secondary);
        _basicHealButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _basicHealButton.Pressed += () => BasicHealRequested?.Invoke();
        healButtons.AddChild(_basicHealButton);
        _fullHealButton = CreateButton(Text.FullHeal, 0, 100, 28, UiButtonStyle.Primary);
        _fullHealButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _fullHealButton.Pressed += () => FullHealRequested?.Invoke();
        healButtons.AddChild(_fullHealButton);

        var supply = CreatePanel(310, UiPanelStyle.Card);
        supply.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddChild(supply);
        var supplyMargin = CreateMargin(26, 22);
        supply.AddChild(supplyMargin);
        var supplyLayout = new VBoxContainer();
        supplyLayout.AddThemeConstantOverride("separation", 18);
        supplyMargin.AddChild(supplyLayout);
        supplyLayout.AddChild(CreateLabel(Text.SupplyTitle, 38));
        supplyLayout.AddChild(CreateLabel(Text.SupplyDescription, 27));
        _potionStatus = CreateLabel(string.Empty, 27);
        supplyLayout.AddChild(_potionStatus);
        _potionButton = CreateButton(Text.BuyPotion, 0, 92, 30, UiButtonStyle.Primary);
        _potionButton.Pressed += () => PotionPurchaseRequested?.Invoke();
        supplyLayout.AddChild(_potionButton);
        supplyLayout.AddChild(CreateCenteredLabel(Text.SupplyHint, 24));

        var bottomButton = CreateButton(Text.BackTown, 0, 112, 42, UiButtonStyle.Secondary);
        bottomButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(bottomButton);
    }

    private void Refresh()
    {
        HubHeaderBuilder.Refresh(_header, _model.Level, _model.Experience, _model.ExperienceToNextLevel, _model.Gold);
        _hpLabel.Text = string.Format(Text.HpFormat, _model.CurrentHp, _model.MaxHp);
        _hpBar.MaxValue = Math.Max(1, _model.MaxHp);
        _hpBar.Value = Math.Clamp(_model.CurrentHp, 0, Math.Max(1, _model.MaxHp));
        _basicHealButton.Disabled = !_model.CanBuyBasicHeal;
        _fullHealButton.Disabled = !_model.CanBuyFullHeal;
        _potionButton.Disabled = !_model.CanBuySmallPotion;
        _potionStatus.Text = string.Format(Text.PotionStatusFormat, _model.SmallPotionCount, _model.PotionPurchasesToday, _model.PotionPurchaseLimit);
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
        public const string BackTown = "返回城鎮";
        public const string Title = "藥草鋪";
        public const string Subtitle = "恢復、補給、材料";
        public const string Description = "在進入下一段訓練前，整理生命值與小型藥水。";
        public const string HealTitle = "生命恢復";
        public const string HpFormat = "目前 HP  {0} / {1}";
        public const string BasicHeal = "基礎恢復\n80 Gold";
        public const string FullHeal = "完全恢復\n180 Gold";
        public const string SupplyTitle = "冒險補給";
        public const string SupplyDescription = "小型藥水可在房間中恢復 30% HP。";
        public const string BuyPotion = "購買小型藥水\n50 Gold";
        public const string SupplyHint = "每日最多購買 3 瓶，進入地城後可在房間中使用。";
        public const string PotionStatusFormat = "持有 {0}  今日購買 {1} / {2}";
    }
}
