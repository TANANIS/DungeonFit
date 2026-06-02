using System;
using System.Collections.Generic;
using Godot;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;

namespace DungeonFit.UI;

public partial class RouteSlotDialogView : Control
{
    public event Action<DungeonRouteSlot>? RouteSlotConfirmed;

    private readonly ExerciseCatalog _exerciseCatalog = new();
    private readonly MusicCatalog _musicCatalog = new();
    private readonly DungeonRouteRules _routeRules = new();
    private readonly List<Button> _exerciseButtons = new();

    private DungeonCategory _category = null!;
    private string _selectedExerciseId = string.Empty;
    private Label _title = null!;
    private SpinBox _setSpinBox = null!;
    private SpinBox _repSpinBox = null!;
    private OptionButton _musicSelector = null!;
    private OptionButton _restSelector = null!;
    private Label _exerciseDetail = null!;
    private GridContainer _exerciseGrid = null!;

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

        var defaultExercise = _exerciseCatalog.GetDefaultForDungeon(category.Id);
        _selectedExerciseId = defaultExercise.Id;
        RefreshExerciseChoices();
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
            CustomMinimumSize = new Vector2(500, 820),
        };
        DungeonFitUi.ApplyPanel(sheet, UiPanelStyle.Overlay);
        center.AddChild(sheet);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        sheet.AddChild(margin);

        var layout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", 10);
        margin.AddChild(layout);

        _title = CreateLabel(Text.RouteSettings, 32, HorizontalAlignment.Center);
        layout.AddChild(_title);

        var settingsPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 228),
        };
        DungeonFitUi.ApplyPanel(settingsPanel, UiPanelStyle.Card);
        layout.AddChild(settingsPanel);

        var settingsMargin = new MarginContainer();
        settingsMargin.AddThemeConstantOverride("margin_left", 18);
        settingsMargin.AddThemeConstantOverride("margin_top", 14);
        settingsMargin.AddThemeConstantOverride("margin_right", 18);
        settingsMargin.AddThemeConstantOverride("margin_bottom", 14);
        settingsPanel.AddChild(settingsMargin);

        var settingsLayout = new VBoxContainer();
        settingsLayout.AddThemeConstantOverride("separation", 12);
        settingsMargin.AddChild(settingsLayout);

        var counterGrid = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        counterGrid.AddThemeConstantOverride("h_separation", 12);
        settingsLayout.AddChild(counterGrid);

        _setSpinBox = CreateSpinBox(DungeonRouteRules.MinSets, DungeonRouteRules.MaxSets, DungeonRouteRules.DefaultSets);
        counterGrid.AddChild(CreateStackedControl(Text.SetCount, _setSpinBox));

        _repSpinBox = CreateSpinBox(DungeonRouteRules.MinReps, DungeonRouteRules.MaxReps, DungeonRouteRules.DefaultReps);
        counterGrid.AddChild(CreateStackedControl(Text.RepCount, _repSpinBox));

        _restSelector = CreateRestSelector();
        counterGrid.AddChild(CreateStackedControl(Text.RestSeconds, _restSelector));

        _musicSelector = CreateMusicSelector();
        settingsLayout.AddChild(CreateLabeledControl(Text.Music, _musicSelector));

        var exerciseHeader = new HBoxContainer();
        layout.AddChild(exerciseHeader);

        var exerciseTitle = CreateLabel(Text.ExerciseTitle, 28, HorizontalAlignment.Left);
        exerciseTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        exerciseHeader.AddChild(exerciseTitle);

        var exerciseHint = CreateLabel(Text.ExerciseHint, 20, HorizontalAlignment.Right);
        exerciseHint.VerticalAlignment = VerticalAlignment.Center;
        exerciseHeader.AddChild(exerciseHint);

        var detailPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 86),
        };
        DungeonFitUi.ApplyPanel(detailPanel, UiPanelStyle.Card);
        layout.AddChild(detailPanel);

        var detailMargin = new MarginContainer();
        detailMargin.AddThemeConstantOverride("margin_left", 16);
        detailMargin.AddThemeConstantOverride("margin_top", 12);
        detailMargin.AddThemeConstantOverride("margin_right", 16);
        detailMargin.AddThemeConstantOverride("margin_bottom", 12);
        detailPanel.AddChild(detailMargin);

        _exerciseDetail = CreateLabel(string.Empty, 21, HorizontalAlignment.Left);
        _exerciseDetail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        detailMargin.AddChild(_exerciseDetail);

        var exerciseScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 162),
            SizeFlagsVertical = SizeFlags.Fill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        layout.AddChild(exerciseScroll);

        _exerciseGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _exerciseGrid.AddThemeConstantOverride("h_separation", 10);
        _exerciseGrid.AddThemeConstantOverride("v_separation", 10);
        exerciseScroll.AddChild(_exerciseGrid);

        var buttonRow = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 70),
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

    private void RefreshExerciseChoices()
    {
        ClearChildren(_exerciseGrid);
        _exerciseButtons.Clear();

        foreach (var exercise in _exerciseCatalog.GetForDungeon(_category.Id))
        {
            var button = new Button
            {
                Text = BuildExerciseButtonText(exercise),
                CustomMinimumSize = new Vector2(0, 76),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.AddThemeFontSizeOverride("font_size", 20);
            button.SetMeta(Meta.ExerciseId, exercise.Id);
            button.Pressed += () => SelectExercise(exercise.Id);
            _exerciseButtons.Add(button);
            _exerciseGrid.AddChild(button);
        }

        RefreshExerciseButtonStyles();
    }

    private void SelectExercise(string exerciseId)
    {
        _selectedExerciseId = exerciseId;
        RefreshExerciseButtonStyles();
    }

    private void RefreshExerciseButtonStyles()
    {
        var selectedExercise = _exerciseCatalog.GetById(_category.Id, _selectedExerciseId);
        _exerciseDetail.Text = $"{selectedExercise.Summary}\n注意：{selectedExercise.SafetyNote}";

        foreach (var button in _exerciseButtons)
        {
            var id = button.GetMeta(Meta.ExerciseId, string.Empty).AsString();
            DungeonFitUi.ApplyButton(
                button,
                id == _selectedExerciseId ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
        }
    }

    private static string BuildExerciseButtonText(ExerciseDefinition exercise)
    {
        var recommended = exercise.IsRecommended ? $" {Text.Recommended}" : string.Empty;
        return $"{exercise.Name}{recommended}\n{exercise.TrainingType}";
    }

    private void Confirm()
    {
        var slot = new DungeonRouteSlot(
            _category.Id,
            (int)_setSpinBox.Value,
            (int)_repSpinBox.Value,
            GetSelectedMusicId(),
            GetSelectedRestSeconds(),
            _selectedExerciseId);
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
            CustomMinimumSize = new Vector2(0, 58),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
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
            CustomMinimumSize = new Vector2(0, 66),
        };
        var label = new Label
        {
            Text = labelText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", 25);
        row.AddChild(label);
        row.AddChild(control);
        return row;
    }

    private static VBoxContainer CreateStackedControl(string labelText, Control control)
    {
        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        stack.AddThemeConstantOverride("separation", 4);

        var label = new Label
        {
            Text = labelText,
        };
        label.AddThemeFontSizeOverride("font_size", 22);
        stack.AddChild(label);
        stack.AddChild(control);
        return stack;
    }

    private OptionButton CreateMusicSelector()
    {
        var selector = new OptionButton
        {
            CustomMinimumSize = new Vector2(248, 58),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
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
            CustomMinimumSize = new Vector2(0, 58),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        foreach (var seconds in DungeonRouteRules.RestSecondOptions)
        {
            selector.AddItem($"{seconds}s");
        }

        selector.Select(1);
        return selector;
    }

    private static void ClearChildren(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static class Meta
    {
        public const string ExerciseId = "exercise_id";
    }

    private static class Text
    {
        public const string RouteSettings = "\u8a0e\u4f10\u8a2d\u5b9a";
        public const string SetCount = "\u7d44\u6578";
        public const string RepCount = "\u6b21\u6578";
        public const string Music = "\u97f3\u6a02";
        public const string RestSeconds = "\u4f11\u606f";
        public const string ExerciseTitle = "\u672c\u6b21\u52d5\u4f5c";
        public const string ExerciseHint = "\u9810\u8a2d\u5df2\u9078";
        public const string Recommended = "\u63a8\u85a6";
        public const string AddToRoute = "\u52a0\u5165\u8def\u7dda";
        public const string Cancel = "\u53d6\u6d88";
    }
}
