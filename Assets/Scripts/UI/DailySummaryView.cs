using System.Linq;
using Godot;
using System;
using DungeonFit.Core.Models;

namespace DungeonFit.UI;

public partial class DailySummaryView : Control
{
    public event Action? OpenAllRequested;
    public event Action? ReturnToTownRequested;

    private DailyRunSummary _summary = null!;
    private bool _isClaimed;
    private bool _isOpening;
    private Label _summaryTitle = null!;
    private Label _summaryStats = null!;
    private Label _rewardList = null!;
    private Label _summaryHint = null!;
    private Button _openAllButton = null!;
    private Button _returnTownButton = null!;

    public override void _Ready()
    {
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.SummaryBackground);
        _summaryTitle = GetNode<Label>("%SummaryTitle");
        _summaryStats = GetNode<Label>("%SummaryStats");
        _rewardList = GetNode<Label>("%RewardList");
        _summaryHint = GetNode<Label>("%SummaryHint");
        _openAllButton = GetNode<Button>("%OpenAllButton");
        _returnTownButton = GetNode<Button>("%ReturnTownButton");
        ApplyArtStyles();

        _openAllButton.Pressed += OpenChests;
        _returnTownButton.Pressed += () => ReturnToTownRequested?.Invoke();

        if (_summary is not null)
        {
            Refresh();
        }
    }

    public void Initialize(DailyRunSummary summary, bool isClaimed)
    {
        _summary = summary;
        _isClaimed = isClaimed;

        if (IsNodeReady())
        {
            Refresh();
        }
    }

    public void MarkClaimed()
    {
        _isClaimed = true;
        _isOpening = false;
        Refresh();
    }

    private void Refresh()
    {
        _summaryTitle.Text = string.Format(Text.TitleFormat, _summary.PlanName);
        _summaryStats.Text =
            string.Format(Text.StagesFormat, _summary.CompletedStages, _summary.TotalStages) + "\n" +
            string.Format(Text.SetsFormat, _summary.CompletedSets, _summary.TotalSets) + "\n" +
            string.Format(Text.TotalGoldFormat, _summary.TotalGold) + "\n" +
            string.Format(Text.ChestsFormat, _summary.ChestCount);

        if (_isOpening)
        {
            _rewardList.Text = Text.OpeningChests;
            _summaryHint.Text = Text.OpeningHint;
            _openAllButton.Disabled = true;
            _openAllButton.Text = Text.Opening;
            _returnTownButton.Disabled = true;
            return;
        }

        _rewardList.Text = _isClaimed
            ? BuildClaimedRewardList()
            : BuildSealedChestList();
        _summaryHint.Text = _isClaimed
            ? Text.ClaimedHint
            : Text.UnclaimedHint;
        _openAllButton.Disabled = _isClaimed;
        _openAllButton.Text = _isClaimed ? Text.Opened : Text.OpenAll;
        _returnTownButton.Disabled = !_isClaimed;
    }

    private void ApplyArtStyles()
    {
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/Header"), UiPanelStyle.Main);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/SummaryPanel"), UiPanelStyle.Main);
        DungeonFitUi.ApplyButton(_openAllButton, UiButtonStyle.Primary);
        DungeonFitUi.ApplyButton(_returnTownButton, UiButtonStyle.Secondary);
    }

    private async void OpenChests()
    {
        if (_isClaimed || _isOpening)
        {
            return;
        }

        _isOpening = true;
        Refresh();
        await ToSignal(GetTree().CreateTimer(0.55), SceneTreeTimer.SignalName.Timeout);
        OpenAllRequested?.Invoke();
    }

    private string BuildSealedChestList()
    {
        var bossChests = _summary.BankedRewards.Count(reward => reward.IsChest && reward.ChestTier == "Boss");
        var normalChests = _summary.BankedRewards.Count(reward => reward.IsChest && reward.ChestTier != "Boss");

        if (_summary.ChestCount == 0)
        {
            return Text.NoChests;
        }

        return string.Format(
            Text.SealedChestFormat,
            bossChests,
            normalChests);
    }

    private string BuildClaimedRewardList()
    {
        if (_summary.EquipmentRewards.Count == 0)
        {
            return Text.GoldClaimed;
        }

        return string.Join(
            "\n",
            _summary.EquipmentRewards.Select(equipment => string.Format(
                Text.EquipmentFormat,
                equipment.Rarity,
                equipment.DisplayName,
                equipment.Power)));
    }

    private static class Text
    {
        public const string TitleFormat = "{0} \u5b8c\u6210";
        public const string StagesFormat = "\u623f\u9593 {0} / {1}";
        public const string SetsFormat = "\u7d44\u6578 {0} / {1}";
        public const string TotalGoldFormat = "\u7e3d\u91d1\u5e63 +{0}";
        public const string ChestsFormat = "\u5f85\u958b\u5bf6\u7bb1 {0}";
        public const string GoldClaimed = "\u5df2\u5c07\u4eca\u65e5\u91d1\u5e63\u52a0\u5165\u89d2\u8272\u72c0\u614b\u3002";
        public const string NoChests = "\u6c92\u6709\u5f85\u958b\u5bf6\u7bb1\u3002";
        public const string SealedChestFormat = "\u5f85\u958b Boss \u5bf6\u7bb1 {0}\n\u5f85\u958b\u666e\u901a\u5bf6\u7bb1 {1}\n\u6309\u4e0b\u958b\u555f\u5bf6\u7bb1\u5f8c\u624d\u6703\u63ed\u66c9\u88dd\u5099\u3002";
        public const string EquipmentFormat = "{0} {1}  \u6230\u529b {2}";
        public const string ClaimedHint = "\u734e\u52f5\u5df2\u9818\u53d6\u3002\u6e96\u5099\u597d\u5f8c\u8fd4\u56de\u57ce\u93ae\u3002";
        public const string UnclaimedHint = "\u958b\u555f\u5bf6\u7bb1\u6703\u5957\u7528\u4eca\u65e5\u66ab\u5b58\u6536\u76ca\u3002";
        public const string OpeningChests = "\u5bf6\u7bb1\u958b\u555f\u4e2d...";
        public const string OpeningHint = "\u6b63\u5728\u7d50\u7b97\u4eca\u65e5\u66ab\u5b58\u6536\u76ca\u3002";
        public const string Opened = "\u5df2\u958b\u555f";
        public const string Opening = "\u958b\u555f\u4e2d";
        public const string OpenAll = "\u958b\u555f\u5bf6\u7bb1";
    }
}
