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
        var background = new ColorRect
        {
            Color = new Color(0.035f, 0.028f, 0.02f, 1),
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
        layout.AddThemeConstantOverride("separation", 20);
        safe.AddChild(layout);

        var header = HubHeaderBuilder.Build("返回", out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(header);

        var hero = new PanelContainer { CustomMinimumSize = new Vector2(0, 310) };
        layout.AddChild(hero);
        var heroLayout = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        heroLayout.AddThemeConstantOverride("separation", 12);
        hero.AddChild(heroLayout);
        heroLayout.AddChild(CreateCenteredLabel("藥草鋪", 58));
        heroLayout.AddChild(CreateCenteredLabel("藥水・恢復・補給", 30));
        heroLayout.AddChild(CreateCenteredLabel("付費恢復生命值，並準備房間內補給。", 25));

        var heal = new PanelContainer { CustomMinimumSize = new Vector2(0, 260) };
        layout.AddChild(heal);
        var healMargin = CreateMargin(26, 22);
        heal.AddChild(healMargin);
        var healLayout = new VBoxContainer();
        healLayout.AddThemeConstantOverride("separation", 14);
        healMargin.AddChild(healLayout);
        healLayout.AddChild(CreateLabel("立即治療", 38));
        _hpLabel = CreateLabel(string.Empty, 30);
        healLayout.AddChild(_hpLabel);
        _hpBar = new ProgressBar { ShowPercentage = false, CustomMinimumSize = new Vector2(0, 24) };
        healLayout.AddChild(_hpBar);
        var healButtons = new HBoxContainer();
        healButtons.AddThemeConstantOverride("separation", 18);
        healLayout.AddChild(healButtons);
        _basicHealButton = CreateButton("基礎治療\n80 Gold", 0, 100, 28);
        _basicHealButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _basicHealButton.Pressed += () => BasicHealRequested?.Invoke();
        healButtons.AddChild(_basicHealButton);
        _fullHealButton = CreateButton("完全治療\n180 Gold", 0, 100, 28);
        _fullHealButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _fullHealButton.Pressed += () => FullHealRequested?.Invoke();
        healButtons.AddChild(_fullHealButton);

        var supply = new PanelContainer { CustomMinimumSize = new Vector2(0, 310), SizeFlagsVertical = SizeFlags.ExpandFill };
        layout.AddChild(supply);
        var supplyMargin = CreateMargin(26, 22);
        supply.AddChild(supplyMargin);
        var supplyLayout = new VBoxContainer();
        supplyLayout.AddThemeConstantOverride("separation", 18);
        supplyMargin.AddChild(supplyLayout);
        supplyLayout.AddChild(CreateLabel("補給品", 38));
        supplyLayout.AddChild(CreateLabel("小型藥水：房間挑戰中恢復 30% HP。", 27));
        _potionStatus = CreateLabel(string.Empty, 27);
        supplyLayout.AddChild(_potionStatus);
        _potionButton = CreateButton("購買小型藥水\n50 Gold", 0, 92, 30);
        _potionButton.Pressed += () => PotionPurchaseRequested?.Invoke();
        supplyLayout.AddChild(_potionButton);
        supplyLayout.AddChild(CreateCenteredLabel("補給品可於房間挑戰中使用。", 24));

        var bottomButton = CreateButton("返回城鎮", 0, 112, 42);
        bottomButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(bottomButton);
    }

    private void Refresh()
    {
        HubHeaderBuilder.Refresh(_header, _model.Level, _model.Experience, _model.ExperienceToNextLevel, _model.Gold);
        _hpLabel.Text = $"目前 HP  {_model.CurrentHp} / {_model.MaxHp}";
        _hpBar.MaxValue = Math.Max(1, _model.MaxHp);
        _hpBar.Value = Math.Clamp(_model.CurrentHp, 0, Math.Max(1, _model.MaxHp));
        _basicHealButton.Disabled = !_model.CanBuyBasicHeal;
        _fullHealButton.Disabled = !_model.CanBuyFullHeal;
        _potionButton.Disabled = !_model.CanBuySmallPotion;
        _potionStatus.Text = $"持有 {_model.SmallPotionCount}  今日購買 {_model.PotionPurchasesToday} / {_model.PotionPurchaseLimit}";
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
