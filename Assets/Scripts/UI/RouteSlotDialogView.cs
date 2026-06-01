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
    private VBoxContainer _exerciseList = null!;

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
            CustomMinimumSize = new Vector2(500, 780),
        };
        DungeonFitUi.ApplyPanel(sheet, UiPanelStyle.Overlay);
        center.AddChild(sheet);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        sheet.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 16);
        margin.AddChild(layout);

        _title = CreateLabel(Text.RouteSettings, 34, HorizontalAlignment.Center);
        layout.AddChild(_title);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 560),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        layout.AddChild(scroll);

        var content = new VBoxContainer();
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        content.AddThemeConstantOverride("separation", 16);
        scroll.AddChild(content);

        _setSpinBox = CreateSpinBox(DungeonRouteRules.MinSets, DungeonRouteRules.MaxSets, DungeonRouteRules.DefaultSets);
        content.AddChild(CreateLabeledControl(Text.SetCount, _setSpinBox));

        _repSpinBox = CreateSpinBox(DungeonRouteRules.MinReps, DungeonRouteRules.MaxReps, DungeonRouteRules.DefaultReps);
        content.AddChild(CreateLabeledControl(Text.RepCount, _repSpinBox));

        _musicSelector = CreateMusicSelector();
        content.AddChild(CreateLabeledControl(Text.Music, _musicSelector));

        _restSelector = CreateRestSelector();
        content.AddChild(CreateLabeledControl(Text.RestSeconds, _restSelector));

        var exerciseTitle = CreateLabel(Text.ExerciseTitle, 28, HorizontalAlignment.Left);
        content.AddChild(exerciseTitle);

        _exerciseDetail = CreateLabel(string.Empty, 21, HorizontalAlignment.Left);
        _exerciseDetail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_exerciseDetail);

        _exerciseList = new VBoxContainer();
        _exerciseList.AddThemeConstantOverride("separation", 10);
        content.AddChild(_exerciseList);

        var hint = CreateLabel(Text.DialogHint, 21, HorizontalAlignment.Left);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(hint);

        var buttonRow = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 76),
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
        ClearChildren(_exerciseList);
        _exerciseButtons.Clear();

        foreach (var exercise in _exerciseCatalog.GetForDungeon(_category.Id))
        {
            var button = new Button
            {
                Text = BuildExerciseButtonText(exercise),
                CustomMinimumSize = new Vector2(0, 76),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.AddThemeFontSizeOverride("font_size", 23);
            button.SetMeta(Meta.ExerciseId, exercise.Id);
            button.Pressed += () => SelectExercise(exercise.Id);
            _exerciseButtons.Add(button);
            _exerciseList.AddChild(button);
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
            CustomMinimumSize = new Vector2(132, 62),
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
            CustomMinimumSize = new Vector2(0, 70),
        };
        var label = new Label
        {
            Text = labelText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", 27);
        row.AddChild(label);
        row.AddChild(control);
        return row;
    }

    private OptionButton CreateMusicSelector()
    {
        var selector = new OptionButton
        {
            CustomMinimumSize = new Vector2(210, 62),
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
            CustomMinimumSize = new Vector2(132, 62),
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
        public const string RestSeconds = "\u7d44\u9593\u4f11\u606f";
        public const string ExerciseTitle = "\u672c\u6b21\u52d5\u4f5c";
        public const string Recommended = "\u63a8\u85a6";
        public const string DialogHint = "\u9810\u8a2d\u5df2\u9078\u597d\u63a8\u85a6\u52d5\u4f5c\uff0c\u53ef\u76f4\u63a5\u52a0\u5165\u8def\u7dda\uff1b\u5982\u6709\u4e0d\u9069\u8acb\u6539\u9078\u8f03\u719f\u6089\u7684\u52d5\u4f5c\u3002";
        public const string AddToRoute = "\u52a0\u5165\u8def\u7dda";
        public const string Cancel = "\u53d6\u6d88";
    }
}
