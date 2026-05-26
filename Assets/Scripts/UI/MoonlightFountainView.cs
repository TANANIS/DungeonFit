using System;
using System.Collections.Generic;
using DungeonFit.Core.Models;
using DungeonFit.Gameplay;
using Godot;

namespace DungeonFit.UI;

public partial class MoonlightFountainView : Control
{
    public event Action? BackToTownRequested;
    public event Action? RecoveryRequested;
    public event Action<string>? BlessingSelected;

    private MoonlightFountainViewModel _model = null!;
    private HubHeaderControls _header = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private Button _recoveryButton = null!;
    private Label _statusLabel = null!;
    private readonly Dictionary<string, Button> _blessingButtons = new();

    public override void _Ready()
    {
        BuildUi();
        if (_model is not null)
        {
            Refresh();
        }
    }

    public void Initialize(MoonlightFountainViewModel model)
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
            Color = new Color(0.025f, 0.027f, 0.09f, 1),
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
        layout.AddThemeConstantOverride("separation", 22);
        safe.AddChild(layout);

        var header = HubHeaderBuilder.Build("返回", out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(header);

        var hero = new PanelContainer { CustomMinimumSize = new Vector2(0, 360) };
        layout.AddChild(hero);
        var heroLayout = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        heroLayout.AddThemeConstantOverride("separation", 12);
        hero.AddChild(heroLayout);
        heroLayout.AddChild(CreateCenteredLabel("月光泉", 62));
        heroLayout.AddChild(CreateCenteredLabel("免費恢復・今日祝福", 30));
        heroLayout.AddChild(CreateCenteredLabel("月光仍可回應你的祈願。", 26));

        var recovery = new PanelContainer { CustomMinimumSize = new Vector2(0, 230) };
        layout.AddChild(recovery);
        var recoveryMargin = CreateMargin(26, 22);
        recovery.AddChild(recoveryMargin);
        var recoveryLayout = new VBoxContainer();
        recoveryLayout.AddThemeConstantOverride("separation", 14);
        recoveryMargin.AddChild(recoveryLayout);
        recoveryLayout.AddChild(CreateLabel("月光洗禮", 38));
        _hpLabel = CreateLabel(string.Empty, 30);
        recoveryLayout.AddChild(_hpLabel);
        _hpBar = new ProgressBar { ShowPercentage = false, CustomMinimumSize = new Vector2(0, 24) };
        recoveryLayout.AddChild(_hpBar);
        _recoveryButton = CreateButton("接受月光洗禮", 0, 82, 34);
        _recoveryButton.Pressed += () => RecoveryRequested?.Invoke();
        recoveryLayout.AddChild(_recoveryButton);

        var blessing = new PanelContainer { CustomMinimumSize = new Vector2(0, 360), SizeFlagsVertical = SizeFlags.ExpandFill };
        layout.AddChild(blessing);
        var blessingMargin = CreateMargin(26, 22);
        blessing.AddChild(blessingMargin);
        var blessingLayout = new VBoxContainer();
        blessingLayout.AddThemeConstantOverride("separation", 18);
        blessingMargin.AddChild(blessingLayout);
        blessingLayout.AddChild(CreateCenteredLabel("今日祝福", 42));

        var blessingRow = new HBoxContainer();
        blessingRow.AddThemeConstantOverride("separation", 18);
        blessingLayout.AddChild(blessingRow);

        foreach (var blessingOption in new[]
        {
            (DailyBlessing.MoonGuard, "月光庇護\n最大 HP +10%"),
            (DailyBlessing.BladeMoon, "鋒刃月影\n攻擊 +5%"),
            (DailyBlessing.StarlightGold, "拾荒星光\nGold +10%"),
        })
        {
            var button = CreateButton(blessingOption.Item2, 0, 150, 28);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var id = blessingOption.Item1;
            button.Pressed += () => BlessingSelected?.Invoke(id);
            _blessingButtons[id] = button;
            blessingRow.AddChild(button);
        }

        _statusLabel = CreateCenteredLabel(string.Empty, 25);
        blessingLayout.AddChild(_statusLabel);

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
        _recoveryButton.Disabled = !_model.CanUseRecovery;
        _recoveryButton.Text = _model.RecoveryUsed ? "今日已使用" : "接受月光洗禮";

        foreach (var option in _model.Blessings)
        {
            if (!_blessingButtons.TryGetValue(option.Id, out var button))
            {
                continue;
            }

            button.Disabled = option.IsDisabled;
            button.Text = option.IsSelected
                ? $"{option.Name}\n{option.Description}\n已選擇"
                : $"{option.Name}\n{option.Description}";
        }

        _statusLabel.Text = _model.SelectedBlessingId == DailyBlessing.None
            ? _model.CanSelectBlessing ? "選擇 1 項今日祝福。" : "冒險中無法更換今日祝福。"
            : "祝福效果僅在今日冒險中生效。";
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
