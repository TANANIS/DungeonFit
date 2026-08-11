using System;
using System.Globalization;
using DungeonFit.Core.Models;
using Godot;

namespace DungeonFit.UI;

public sealed class RoomResultPresenter
{
    private readonly PanelContainer _panel;
    private readonly Label _title;
    private readonly Label _rewardSummary;
    private readonly Button _continueButton;
    private readonly PanelContainer _recordPanel;
    private readonly Label _previousRecord;
    private readonly LineEdit _setsInput;
    private readonly LineEdit _repsInput;
    private readonly LineEdit _weightInput;

    private RunSummary? _summary;
    private TaskTemplate? _task;
    private bool _hasRequestedContinue;

    public RoomResultPresenter(
        PanelContainer panel,
        Label title,
        Label rewardSummary,
        Button continueButton)
    {
        _panel = panel;
        _title = title;
        _rewardSummary = rewardSummary;
        _continueButton = continueButton;
        _recordPanel = BuildRecordPanel(out _previousRecord, out _setsInput, out _repsInput, out _weightInput);

        if (_continueButton.GetParent() is VBoxContainer layout)
        {
            layout.AddChild(_recordPanel);
            layout.MoveChild(_recordPanel, _continueButton.GetIndex());
        }

        _continueButton.Pressed += RequestContinue;
        _panel.GuiInput += OnPanelGuiInput;
    }

    public event Action<RunSummary, ExerciseHistoryEntry?>? ContinueRequested;

    public bool IsShowing => _panel.Visible;

    public void Hide()
    {
        _summary = null;
        _task = null;
        _hasRequestedContinue = false;
        _panel.Visible = false;
    }

    public void Show(
        RunSummary summary,
        TaskTemplate task,
        ExerciseHistoryEntry? lastRecord,
        ExercisePersonalRecord? personalRecord)
    {
        _summary = summary;
        _task = task;
        _hasRequestedContinue = false;
        _title.Text = GetResultTitle(summary.Title);
        _continueButton.Text = Text.Continue;
        _continueButton.Disabled = false;
        _continueButton.MouseFilter = Control.MouseFilterEnum.Stop;
        _rewardSummary.Text = BuildResultSummary(summary);
        RefreshRecordInputs(summary, task, lastRecord, personalRecord);
        _panel.Visible = true;
        _panel.MoveToFront();
    }

    public bool HandleInput(InputEvent inputEvent)
    {
        if (!IsShowing || _hasRequestedContinue)
        {
            return false;
        }

        if (!HasRecordInputFocus() && IsContinueInput(inputEvent))
        {
            RequestContinue();
            return true;
        }

        return false;
    }

    private void RequestContinue()
    {
        if (_hasRequestedContinue || _summary is null)
        {
            return;
        }

        _hasRequestedContinue = true;
        ContinueRequested?.Invoke(_summary, BuildExerciseRecord(_summary, _task));
    }

    private void OnPanelGuiInput(InputEvent inputEvent)
    {
        if (!HasRecordInputFocus() && IsContinueInput(inputEvent))
        {
            RequestContinue();
        }
    }

    private static bool IsContinueInput(InputEvent inputEvent)
    {
        return inputEvent is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter or Key.Space };
    }

    private bool HasRecordInputFocus()
    {
        return _setsInput.HasFocus() || _repsInput.HasFocus() || _weightInput.HasFocus();
    }

    private static PanelContainer BuildRecordPanel(
        out Label previousRecord,
        out LineEdit setsInput,
        out LineEdit repsInput,
        out LineEdit weightInput)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 180),
        };
        DungeonFitUi.ApplyPanel(panel, UiPanelStyle.Card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        layout.AddChild(CreateLabel(Text.RecordTitle, 24, HorizontalAlignment.Left));
        previousRecord = CreateLabel(string.Empty, 20, HorizontalAlignment.Left);
        previousRecord.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        layout.AddChild(previousRecord);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        layout.AddChild(row);

        setsInput = CreateInput(Text.SetsPlaceholder);
        repsInput = CreateInput(Text.RepsPlaceholder);
        weightInput = CreateInput(Text.WeightPlaceholder);
        row.AddChild(WrapField(Text.SetsLabel, setsInput));
        row.AddChild(WrapField(Text.RepsLabel, repsInput));
        row.AddChild(WrapField(Text.WeightLabel, weightInput));

        return panel;
    }

    private void RefreshRecordInputs(
        RunSummary summary,
        TaskTemplate task,
        ExerciseHistoryEntry? lastRecord,
        ExercisePersonalRecord? personalRecord)
    {
        _setsInput.Text = Math.Max(0, summary.CompletedSets).ToString(CultureInfo.InvariantCulture);
        var targetReps = summary.TargetReps > 0 ? summary.TargetReps : task.TargetReps;
        _repsInput.Text = Math.Max(0, targetReps).ToString(CultureInfo.InvariantCulture);
        _weightInput.Text = lastRecord?.WeightKg is { } weight
            ? weight.ToString("0.#", CultureInfo.InvariantCulture)
            : string.Empty;
        var lastLine = lastRecord is null
            ? Text.NoPreviousRecord
            : string.Format(
                Text.PreviousRecordFormat,
                lastRecord.ActualSets,
                lastRecord.ActualReps,
                lastRecord.WeightKg.HasValue ? $"{lastRecord.WeightKg.Value:0.#} kg" : Text.BodyweightRecord);
        var prLine = personalRecord is null
            ? Text.NoPersonalRecord
            : string.Format(
                Text.PersonalRecordFormat,
                personalRecord.MaxWeightKg.HasValue ? $"{personalRecord.MaxWeightKg.Value:0.#} kg" : Text.BodyweightRecord,
                personalRecord.MaxReps,
                personalRecord.MaxSets);
        _previousRecord.Text = $"{lastLine}\n{prLine}";
    }

    private ExerciseHistoryEntry? BuildExerciseRecord(RunSummary summary, TaskTemplate? task)
    {
        var exerciseId = !string.IsNullOrWhiteSpace(summary.ExerciseId)
            ? summary.ExerciseId
            : task?.ExerciseId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(exerciseId))
        {
            return null;
        }

        var plannedReps = summary.TargetReps > 0 ? summary.TargetReps : task?.TargetReps ?? 0;
        return new ExerciseHistoryEntry
        {
            ExerciseId = exerciseId,
            DungeonTypeId = !string.IsNullOrWhiteSpace(summary.DungeonTypeId)
                ? summary.DungeonTypeId
                : task?.DungeonTypeId ?? string.Empty,
            CompletedAtUtc = DateTime.UtcNow,
            PlannedSets = Math.Max(0, summary.TotalSets),
            PlannedReps = Math.Max(0, plannedReps),
            ActualSets = ParseInt(_setsInput.Text, summary.CompletedSets),
            ActualReps = ParseInt(_repsInput.Text, plannedReps),
            WeightKg = ParseNullableWeight(_weightInput.Text),
        };
    }

    private static int ParseInt(string text, int fallback)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? Math.Clamp(value, 0, 999)
            : Math.Max(0, fallback);
    }

    private static double? ParseNullableWeight(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? Math.Round(Math.Clamp(value, 0, 999), 1, MidpointRounding.AwayFromZero)
            : null;
    }

    private static Control WrapField(string labelText, LineEdit input)
    {
        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stack.AddThemeConstantOverride("separation", 4);
        stack.AddChild(CreateLabel(labelText, 18, HorizontalAlignment.Left));
        stack.AddChild(input);
        return stack;
    }

    private static LineEdit CreateInput(string placeholder)
    {
        var input = new LineEdit
        {
            PlaceholderText = placeholder,
            CustomMinimumSize = new Vector2(0, 48),
            VirtualKeyboardType = LineEdit.VirtualKeyboardTypeEnum.NumberDecimal,
        };
        input.AddThemeFontSizeOverride("font_size", 20);
        return input;
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

    private static string BuildResultSummary(RunSummary summary)
    {
        var setLine = $"組數 {summary.CompletedSets} / {summary.TotalSets}";
        var chestLine = summary.HasChest
            ? string.Format(Text.SealedChests, summary.ChestCount)
            : Text.NoEquipmentChest;

        return $"{setLine}\n金幣預覽 +{summary.Reward.Gold}\n{chestLine}\n房間收益將存入今日總結算。";
    }

    private static string GetResultTitle(string title)
    {
        return title switch
        {
            Text.BossClearedRaw => Text.BossCleared,
            Text.RoomWithdrawnRaw => Text.RoomWithdrawn,
            _ => title,
        };
    }

    private static class Text
    {
        public const string BossClearedRaw = "Boss Cleared";
        public const string RoomWithdrawnRaw = "Room Withdrawn";
        public const string BossCleared = "Boss 擊破";
        public const string RoomWithdrawn = "房間撤退";
        public const string Continue = "繼續";
        public const string NoEquipmentChest = "沒有封存寶箱";
        public const string SealedChests = "寶箱已封存 {0}";
        public const string RecordTitle = "記錄本關訓練";
        public const string NoPreviousRecord = "這個動作尚無上次記錄";
        public const string PreviousRecordFormat = "上次：{0} 組 x {1} 次 / {2}";
        public const string NoPersonalRecord = "PR：尚無";
        public const string PersonalRecordFormat = "PR：重量 {0} / 次數 {1} / 組數 {2}";
        public const string BodyweightRecord = "徒手";
        public const string SetsLabel = "實際組數";
        public const string RepsLabel = "每組次數";
        public const string WeightLabel = "重量 kg";
        public const string SetsPlaceholder = "sets";
        public const string RepsPlaceholder = "reps";
        public const string WeightPlaceholder = "可留空";
    }
}
