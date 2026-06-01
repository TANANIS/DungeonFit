using Godot;
using System;
using DungeonFit.Core.Models;

namespace DungeonFit.UI;

public partial class SetSummaryView : Control
{
    public event Action? ContinueRequested;
    public event Action? ReturnToTownRequested;

    private SetSummary _summary = null!;
    private Label _summaryTitle = null!;
    private Label _summaryStats = null!;
    private Label _summaryHint = null!;
    private Button _continueButton = null!;
    private Button _endTrainingButton = null!;

    public override void _Ready()
    {
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.SummaryBackground);
        _summaryTitle = GetNode<Label>("%SummaryTitle");
        _summaryStats = GetNode<Label>("%SummaryStats");
        _summaryHint = GetNode<Label>("%SummaryHint");
        _continueButton = GetNode<Button>("%ContinueButton");
        _endTrainingButton = GetNode<Button>("%ReturnTownButton");
        ApplyArtStyles();

        _continueButton.Pressed += () => ContinueRequested?.Invoke();
        _endTrainingButton.Pressed += () => ReturnToTownRequested?.Invoke();

        if (_summary is not null)
        {
            Refresh();
        }
    }

    public void Initialize(SetSummary summary)
    {
        _summary = summary;

        if (IsNodeReady())
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        _summaryTitle.Text = string.Format(Text.TitleFormat, _summary.CompletedStageNumber, _summary.TotalStages);
        _summaryStats.Text =
            string.Format(Text.CompletedRoomFormat, _summary.Run.RoomName) + "\n" +
            string.Format(Text.ResultFormat, GetResultTitle(_summary.Run.Title)) + "\n" +
            string.Format(Text.SetsFormat, _summary.Run.CompletedSets, _summary.Run.TotalSets) + "\n" +
            string.Format(Text.GoldPreviewFormat, _summary.Run.Reward.Gold) + "\n" +
            string.Format(Text.ExperienceFormat, _summary.Run.ExperienceGained) + "\n" +
            string.Format(Text.BankedChestFormat, _summary.BankedChestCount);

        var nextLine = _summary.NextStage is null
            ? Text.RouteCompleteHint
            : string.Format(
                Text.NextRoomFormat,
                GetDungeonName(_summary.NextStage),
                _summary.NextStage.TotalSets,
                _summary.NextStage.TargetReps,
                _summary.NextStage.RestSeconds);

        _summaryHint.Text = _summary.BankedChestCount > 0
            ? string.Format(Text.BankedHintFormat, nextLine)
            : string.Format(Text.NoChestHintFormat, nextLine);
        _continueButton.Text = _summary.NextStage is null ? Text.ViewDailySummary : Text.Continue;
        _endTrainingButton.Text = Text.EndTraining;
    }

    private void ApplyArtStyles()
    {
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/Header"), UiPanelStyle.Main);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/SummaryPanel"), UiPanelStyle.Main);
        DungeonFitUi.ApplyButton(_continueButton, UiButtonStyle.Primary);
        DungeonFitUi.ApplyButton(_endTrainingButton, UiButtonStyle.Secondary);
    }

    private static string GetDungeonName(TaskTemplate task)
    {
        return task.DungeonTypeId switch
        {
            "chest" => Text.ChestDungeon,
            "shoulders" => Text.ShoulderDungeon,
            "back" => Text.BackDungeon,
            "legs" => Text.LegDungeon,
            "core" => Text.CoreDungeon,
            "arms" => Text.ArmDungeon,
            _ => task.DungeonTypeName,
        };
    }

    private static string GetResultTitle(string title)
    {
        return title switch
        {
            "Boss Cleared" => Text.BossCleared,
            "Room Withdrawn" => Text.RoomWithdrawn,
            _ => title,
        };
    }

    private static class Text
    {
        public const string TitleFormat = "\u623f\u9593 {0} / {1} \u7d50\u7b97";
        public const string CompletedRoomFormat = "\u5b8c\u6210\u623f\u9593\uff1a{0}";
        public const string ResultFormat = "\u7d50\u679c\uff1a{0}";
        public const string SetsFormat = "\u7d44\u6578 {0} / {1}";
        public const string GoldPreviewFormat = "\u91d1\u5e63\u9810\u89bd +{0}";
        public const string ExperienceFormat = "EXP +{0}";
        public const string BankedChestFormat = "\u5b58\u5165\u5bf6\u7bb1 +{0}";
        public const string RouteCompleteHint = "\u4eca\u65e5\u8def\u7dda\u5df2\u5b8c\u6210\uff0c\u53ef\u524d\u5f80\u7e3d\u7d50\u7b97\u3002";
        public const string NextRoomFormat = "\u4e0b\u4e00\u623f\u9593\uff1a{0}  {1} x {2}  \u4f11\u606f {3}s";
        public const string BankedHintFormat = "\u6536\u76ca\u5df2\u5b58\u5165\u4eca\u65e5\u7d50\u7b97\u3002\n{0}";
        public const string NoChestHintFormat = "\u672c\u623f\u9593\u6c92\u6709\u5b58\u5165\u5bf6\u7bb1\u3002\n{0}";
        public const string Continue = "\u7e7c\u7e8c";
        public const string EndTraining = "\u7d50\u675f\u8a13\u7df4";
        public const string ViewDailySummary = "\u67e5\u770b\u7e3d\u7d50\u7b97";
        public const string ChestDungeon = "\u80f8\u5730\u57ce";
        public const string ShoulderDungeon = "\u80a9\u5730\u57ce";
        public const string BackDungeon = "\u80cc\u5730\u57ce";
        public const string LegDungeon = "\u817f\u5730\u57ce";
        public const string CoreDungeon = "\u6838\u5fc3\u5730\u57ce";
        public const string ArmDungeon = "\u624b\u81c2\u5730\u57ce";
        public const string BossCleared = "Boss \u64ca\u7834";
        public const string RoomWithdrawn = "\u623f\u9593\u64a4\u9000";
    }
}
