using System;
using Godot;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;

namespace DungeonFit.UI;

public partial class RouteSlotDialogView : Control
{
    public event Action<DungeonRouteSlot>? RouteSlotConfirmed;

    private readonly MusicCatalog _musicCatalog = new();
    private readonly DungeonRouteRules _routeRules = new();

    private DungeonCategory _category = null!;
    private Label _title = null!;
    private SpinBox _setSpinBox = null!;
    private SpinBox _repSpinBox = null!;
    private OptionButton _musicSelector = null!;
    private OptionButton _restSelector = null!;

    public override void _Ready()
    {
        BuildMobileOverlay();
        Visible = false;
    }

    public void OpenForDungeon(DungeonCategory category)
    {
        _category = category;
        _title.Text = $"{category.ShortName} {Text.RouteSettings}";
        _setSpinBox.Value = DungeonRouteRules.DefaultSets;
        _repSpinBox.Value = DungeonRouteRules.DefaultReps;
        _musicSelector.Select(0);
        SelectRestSeconds(DungeonRouteRules.DefaultRestSeconds);
        Visible = true;
    }

    private void BuildMobileOverlay()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var scrim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.62f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(scrim);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var sheet = new PanelContainer
        {
            CustomMinimumSize = new Vector2(620, 620),
        };
        DungeonFitUi.ApplyPanel(sheet, UiPanelStyle.Overlay);
        center.AddChild(sheet);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 32);
        margin.AddThemeConstantOverride("margin_top", 28);
        margin.AddThemeConstantOverride("margin_right", 32);
        margin.AddThemeConstantOverride("margin_bottom", 28);
        sheet.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 18);
        margin.AddChild(layout);

        _title = CreateLabel(Text.RouteSettings, 34, HorizontalAlignment.Center);
        layout.AddChild(_title);

        _setSpinBox = CreateSpinBox(DungeonRouteRules.MinSets, DungeonRouteRules.MaxSets, DungeonRouteRules.DefaultSets);
        layout.AddChild(CreateLabeledControl(Text.SetCount, _setSpinBox));

        _repSpinBox = CreateSpinBox(DungeonRouteRules.MinReps, DungeonRouteRules.MaxReps, DungeonRouteRules.DefaultReps);
        layout.AddChild(CreateLabeledControl(Text.RepCount, _repSpinBox));

        _musicSelector = CreateMusicSelector();
        layout.AddChild(CreateLabeledControl(Text.Music, _musicSelector));

        _restSelector = CreateRestSelector();
        layout.AddChild(CreateLabeledControl(Text.RestSeconds, _restSelector));

        var hint = CreateLabel(Text.DialogHint, 22, HorizontalAlignment.Left);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        layout.AddChild(hint);

        var buttonRow = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 82),
        };
        buttonRow.AddThemeConstantOverride("separation", 16);
        layout.AddChild(buttonRow);

        var cancelButton = new Button
        {
            Text = Text.Cancel,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        cancelButton.AddThemeFontSizeOverride("font_size", 28);
        DungeonFitUi.ApplyButton(cancelButton, UiButtonStyle.Secondary);
        cancelButton.Pressed += Close;
        buttonRow.AddChild(cancelButton);

        var confirmButton = new Button
        {
            Text = Text.AddToRoute,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        confirmButton.AddThemeFontSizeOverride("font_size", 28);
        DungeonFitUi.ApplyButton(confirmButton, UiButtonStyle.Primary);
        confirmButton.Pressed += Confirm;
        buttonRow.AddChild(confirmButton);
    }

    private void Confirm()
    {
        var slot = new DungeonRouteSlot(
            _category.Id,
            (int)_setSpinBox.Value,
            (int)_repSpinBox.Value,
            GetSelectedMusicId(),
            GetSelectedRestSeconds());
        RouteSlotConfirmed?.Invoke(_routeRules.Normalize(slot));
        Close();
    }

    private void Close()
    {
        Visible = false;
    }

    private string GetSelectedMusicId()
    {
        var selected = _musicSelector.Selected;
        var tracks = _musicCatalog.GetAll();
        return selected >= 0 && selected < tracks.Count ? tracks[selected].Id : tracks[0].Id;
    }

    private int GetSelectedRestSeconds()
    {
        var selected = _restSelector.Selected;
        return selected >= 0 && selected < DungeonRouteRules.RestSecondOptions.Length
            ? DungeonRouteRules.RestSecondOptions[selected]
            : DungeonRouteRules.DefaultRestSeconds;
    }

    private void SelectRestSeconds(int restSeconds)
    {
        var index = Array.IndexOf(DungeonRouteRules.RestSecondOptions, restSeconds);
        _restSelector.Select(index < 0 ? 1 : index);
    }

    private static SpinBox CreateSpinBox(double min, double max, double value)
    {
        return new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Value = value,
            CustomMinimumSize = new Vector2(220, 64),
        };
    }

    private static Label CreateLabel(string text, int fontSize, HorizontalAlignment alignment)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static HBoxContainer CreateLabeledControl(string labelText, Control control)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 72),
        };
        var label = new Label
        {
            Text = labelText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", 28);
        row.AddChild(label);
        row.AddChild(control);
        return row;
    }

    private OptionButton CreateMusicSelector()
    {
        var selector = new OptionButton
        {
            CustomMinimumSize = new Vector2(260, 64),
        };
        foreach (var music in _musicCatalog.GetAll())
        {
            selector.AddItem(music.DisplayName);
        }

        selector.Select(0);
        return selector;
    }

    private static OptionButton CreateRestSelector()
    {
        var selector = new OptionButton
        {
            CustomMinimumSize = new Vector2(260, 64),
        };
        foreach (var seconds in DungeonRouteRules.RestSecondOptions)
        {
            selector.AddItem($"{seconds}s");
        }

        selector.Select(1);
        return selector;
    }

    private static class Text
    {
        public const string RouteSettings = "\u8a0e\u4f10\u8a2d\u5b9a";
        public const string SetCount = "\u7d44\u6578";
        public const string RepCount = "\u6b21\u6578";
        public const string Music = "\u97f3\u6a02";
        public const string RestSeconds = "\u7d44\u9593\u4f11\u606f";
        public const string DialogHint = "\u9019\u500b\u8a0e\u4f10 slot \u6703\u4f9d\u6b64\u7d44\u6578\u3001\u6b21\u6578\u3001\u97f3\u6a02\u8207\u4f11\u606f\u9032\u5165\u6311\u6230\u3002";
        public const string AddToRoute = "\u52a0\u5165\u8def\u7dda";
        public const string Cancel = "\u53d6\u6d88";
    }
}
