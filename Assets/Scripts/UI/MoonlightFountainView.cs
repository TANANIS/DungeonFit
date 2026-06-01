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
        layout.AddThemeConstantOverride("separation", 22);
        safe.AddChild(layout);

        var header = HubHeaderBuilder.Build(Text.BackShort, out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(header);

        var hero = CreatePanel(360, UiPanelStyle.Main);
        layout.AddChild(hero);
        var heroLayout = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        heroLayout.AddThemeConstantOverride("separation", 12);
        hero.AddChild(heroLayout);
        heroLayout.AddChild(CreateCenteredLabel(Text.Title, 62));
        heroLayout.AddChild(CreateCenteredLabel(Text.Subtitle, 30));
        heroLayout.AddChild(CreateCenteredLabel(Text.Description, 26));

        var recovery = CreatePanel(230, UiPanelStyle.Card);
        layout.AddChild(recovery);
        var recoveryMargin = CreateMargin(26, 22);
        recovery.AddChild(recoveryMargin);
        var recoveryLayout = new VBoxContainer();
        recoveryLayout.AddThemeConstantOverride("separation", 14);
        recoveryMargin.AddChild(recoveryLayout);
        recoveryLayout.AddChild(CreateLabel(Text.RecoveryTitle, 38));
        _hpLabel = CreateLabel(string.Empty, 30);
        recoveryLayout.AddChild(_hpLabel);
        _hpBar = new ProgressBar { ShowPercentage = false, CustomMinimumSize = new Vector2(0, 24) };
        DungeonFitUi.ApplyProgressBar(_hpBar, new Color(0.48f, 0.82f, 0.58f));
        recoveryLayout.AddChild(_hpBar);
        _recoveryButton = CreateButton(Text.UseRecovery, 0, 82, 34, UiButtonStyle.Primary);
        _recoveryButton.Pressed += () => RecoveryRequested?.Invoke();
        recoveryLayout.AddChild(_recoveryButton);

        var blessing = CreatePanel(360, UiPanelStyle.Card);
        blessing.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddChild(blessing);
        var blessingMargin = CreateMargin(26, 22);
        blessing.AddChild(blessingMargin);
        var blessingLayout = new VBoxContainer();
        blessingLayout.AddThemeConstantOverride("separation", 18);
        blessingMargin.AddChild(blessingLayout);
        blessingLayout.AddChild(CreateCenteredLabel(Text.BlessingTitle, 42));

        var blessingRow = new HBoxContainer();
        blessingRow.AddThemeConstantOverride("separation", 18);
        blessingLayout.AddChild(blessingRow);

        foreach (var blessingOption in new[]
        {
            (DailyBlessing.MoonGuard, Text.MoonGuard),
            (DailyBlessing.BladeMoon, Text.BladeMoon),
            (DailyBlessing.StarlightGold, Text.StarlightGold),
        })
        {
            var button = CreateButton(blessingOption.Item2, 0, 150, 28, UiButtonStyle.Secondary);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var id = blessingOption.Item1;
            button.Pressed += () => BlessingSelected?.Invoke(id);
            _blessingButtons[id] = button;
            blessingRow.AddChild(button);
        }

        _statusLabel = CreateCenteredLabel(string.Empty, 25);
        blessingLayout.AddChild(_statusLabel);

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
        _recoveryButton.Disabled = !_model.CanUseRecovery;
        _recoveryButton.Text = _model.RecoveryUsed ? Text.RecoveryUsed : Text.UseRecovery;

        foreach (var option in _model.Blessings)
        {
            if (!_blessingButtons.TryGetValue(option.Id, out var button))
            {
                continue;
            }

            button.Disabled = option.IsDisabled;
            button.Text = option.IsSelected
                ? $"{option.Name}\n{option.Description}\n{Text.Selected}"
                : $"{option.Name}\n{option.Description}";
        }

        _statusLabel.Text = _model.SelectedBlessingId == DailyBlessing.None
            ? _model.CanSelectBlessing ? Text.SelectOneBlessing : Text.BlessingLocked
            : Text.BlessingActive;
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
        public const string Title = "月光泉";
        public const string Subtitle = "祝福、恢復、祈禱";
        public const string Description = "在月光下恢復生命，並選擇今日地城加護。";
        public const string RecoveryTitle = "月光恢復";
        public const string HpFormat = "目前 HP  {0} / {1}";
        public const string UseRecovery = "使用月光恢復";
        public const string RecoveryUsed = "今日已恢復";
        public const string BlessingTitle = "今日祝福";
        public const string MoonGuard = "月守\n最大 HP +10%";
        public const string BladeMoon = "刃月\n攻擊 +5%";
        public const string StarlightGold = "星光金幣\nGold +10%";
        public const string Selected = "已選擇";
        public const string SelectOneBlessing = "選擇 1 個祝福，今日地城全程生效。";
        public const string BlessingLocked = "冒險中無法更換今日祝福。";
        public const string BlessingActive = "祝福已啟動，今日地城會套用此效果。";
    }
}
