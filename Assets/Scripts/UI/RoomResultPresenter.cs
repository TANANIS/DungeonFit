using System;
using DungeonFit.Core.Models;
using Godot;

namespace DungeonFit.UI;

public sealed class RoomResultPresenter
{
    private readonly PanelContainer _panel;
    private readonly Label _title;
    private readonly Label _rewardSummary;
    private readonly Button _continueButton;

    private RunSummary? _summary;
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

        _continueButton.Pressed += RequestContinue;
        _panel.GuiInput += OnPanelGuiInput;
    }

    public event Action<RunSummary>? ContinueRequested;

    public bool IsShowing => _panel.Visible;

    public void Hide()
    {
        _summary = null;
        _hasRequestedContinue = false;
        _panel.Visible = false;
    }

    public void Show(RunSummary summary)
    {
        _summary = summary;
        _hasRequestedContinue = false;
        _title.Text = GetResultTitle(summary.Title);
        _continueButton.Text = Text.Continue;
        _continueButton.Disabled = false;
        _continueButton.MouseFilter = Control.MouseFilterEnum.Stop;
        _rewardSummary.Text = BuildResultSummary(summary);
        _panel.Visible = true;
        _panel.MoveToFront();
    }

    public bool HandleInput(InputEvent inputEvent)
    {
        if (!IsShowing || _hasRequestedContinue)
        {
            return false;
        }

        if (IsContinueInput(inputEvent))
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
        ContinueRequested?.Invoke(_summary);
    }

    private void OnPanelGuiInput(InputEvent inputEvent)
    {
        if (IsContinueInput(inputEvent))
        {
            RequestContinue();
        }
    }

    private static bool IsContinueInput(InputEvent inputEvent)
    {
        return inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
            || inputEvent is InputEventScreenTouch { Pressed: true }
            || inputEvent is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter or Key.Space };
    }

    private static string BuildResultSummary(RunSummary summary)
    {
        var setLine = $"組數 {summary.CompletedSets} / {summary.TotalSets}";
        var chestLine = summary.HasChest
            ? string.Format(Text.SealedChests, summary.ChestCount)
            : Text.NoEquipmentChest;

        return $"{setLine}\n金幣預覽 +{summary.Reward.Gold}\n{chestLine}\n後會將本房間收益存入今日結算。";
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
        public const string NoEquipmentChest = "寶箱已封存";
        public const string SealedChests = "\u5bf6\u7bb1\u5df2\u5c01\u5b58 {0}";
    }
}
