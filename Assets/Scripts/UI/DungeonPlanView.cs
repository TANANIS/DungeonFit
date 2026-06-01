using System.Collections.Generic;
using System;
using Godot;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;

namespace DungeonFit.UI;

public partial class DungeonPlanView : Control
{
    public event Action? StartAdventureRequested;
    public event Action? BackToTownRequested;
    public event Action? DailySummaryRequested;

    private DungeonPlan _plan = null!;
    private readonly DungeonCategoryCatalog _categoryCatalog = new();
    private readonly MusicCatalog _musicCatalog = new();
    private readonly DungeonRouteRules _routeRules = new();
    private DungeonRun? _run;
    private IReadOnlyList<ActiveShortTermQuest> _activeQuests = Array.Empty<ActiveShortTermQuest>();
    private readonly List<DungeonRouteSlot> _selectedDungeonRoute = new();
    private bool _canEditPlan;
    private Label _planTitle = null!;
    private Label _planSubtitle = null!;
    private Label _planSummary = null!;
    private GridContainer _dungeonTypeGrid = null!;
    private VBoxContainer _routeList = null!;
    private Button _startAdventureButton = null!;
    private RouteSlotDialogView _slotDialog = null!;
    private DungeonTypeGridView _dungeonTypeGridView = null!;
    private DungeonRouteListView _routeListView = null!;
    private DungeonPlanSummaryPresenter _summaryPresenter = null!;

    public override void _Ready()
    {
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.DungeonPlanBackground);
        _planTitle = GetNode<Label>("%PlanTitle");
        _planSubtitle = GetNode<Label>("%PlanSubtitle");
        _planSummary = GetNode<Label>("%PlanSummary");
        _dungeonTypeGrid = GetNode<GridContainer>("%DungeonTypeGrid");
        _routeList = GetNode<VBoxContainer>("%StageList");
        _startAdventureButton = GetNode<Button>("%StartAdventureButton");
        _dungeonTypeGridView = new DungeonTypeGridView(_dungeonTypeGrid, _categoryCatalog);
        _routeListView = new DungeonRouteListView(_routeList, _categoryCatalog, _musicCatalog);
        _summaryPresenter = new DungeonPlanSummaryPresenter(_planSummary, _startAdventureButton);
        ApplyArtStyles();

        _slotDialog = new RouteSlotDialogView();
        _slotDialog.RouteSlotConfirmed += AddRouteSlot;
        AddChild(_slotDialog);
        _slotDialog.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _startAdventureButton.Pressed += RequestPrimaryAction;
        var backButton = GetNode<Button>("%BackTownButton");
        DungeonFitUi.ApplyButton(backButton, UiButtonStyle.Secondary);
        backButton.Pressed += () => BackToTownRequested?.Invoke();

        if (_plan is not null)
        {
            Refresh();
        }
    }

    public void Initialize(
        DungeonPlan plan,
        DungeonRun? run,
        IReadOnlyList<DungeonRouteSlot> selectedDungeonRoute,
        bool canEditPlan,
        IReadOnlyList<ActiveShortTermQuest> activeQuests)
    {
        _plan = plan;
        _run = run;
        _activeQuests = activeQuests;
        _selectedDungeonRoute.Clear();
        _selectedDungeonRoute.AddRange(selectedDungeonRoute);
        _canEditPlan = canEditPlan;

        if (IsNodeReady())
        {
            Refresh();
        }
    }

    public IReadOnlyList<DungeonRouteSlot> GetSelectedDungeonRoute()
    {
        return _selectedDungeonRoute.ToArray();
    }

    private void Refresh()
    {
        _planTitle.Text = _canEditPlan ? Text.SelectDungeon : _plan.DisplayName;
        _planSubtitle.Text = _canEditPlan ? Text.SelectDungeonSubtitle : Text.RouteLocked;
        _dungeonTypeGridView.Refresh(_canEditPlan, _selectedDungeonRoute.Count, OpenRouteSlotDialog);
        _routeListView.Refresh(_canEditPlan, _selectedDungeonRoute, _plan, _run, RemoveRouteSlot);
        _summaryPresenter.Refresh(_canEditPlan, _selectedDungeonRoute, _plan, _run, _routeRules, _activeQuests);
    }

    private void OpenRouteSlotDialog(DungeonCategory category)
    {
        if (!_canEditPlan || _selectedDungeonRoute.Count >= DungeonRouteRules.MaxRouteSlots)
        {
            return;
        }

        _slotDialog.OpenForDungeon(category);
    }

    private void AddRouteSlot(DungeonRouteSlot slot)
    {
        if (!_canEditPlan || _selectedDungeonRoute.Count >= DungeonRouteRules.MaxRouteSlots)
        {
            return;
        }

        _selectedDungeonRoute.Add(slot);
        Refresh();
    }

    private void RemoveRouteSlot(int routeIndex)
    {
        if (!_canEditPlan || routeIndex < 0 || routeIndex >= _selectedDungeonRoute.Count)
        {
            return;
        }

        _selectedDungeonRoute.RemoveAt(routeIndex);
        Refresh();
    }

    private void RequestPrimaryAction()
    {
        if (_run?.IsComplete == true)
        {
            DailySummaryRequested?.Invoke();
            return;
        }

        StartAdventureRequested?.Invoke();
    }

    private void ApplyArtStyles()
    {
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/Header"), UiPanelStyle.Main);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/RoutePanel"), UiPanelStyle.Main);
        DungeonFitUi.ApplyButton(_startAdventureButton, UiButtonStyle.Primary);
    }

    private static class Text
    {
        public const string SelectDungeon = "\u9078\u64c7\u5730\u57ce";
        public const string SelectDungeonSubtitle = "\u9078\u64c7 4-6 \u500b\u8a0e\u4f10\u5340\u57df\uff0c\u76f8\u540c\u5730\u57ce\u53ef\u4ee5\u91cd\u8907\u9078\u53d6\u3002";
        public const string RouteLocked = "\u4eca\u65e5\u8a0e\u4f10\u8def\u7dda\u5df2\u9396\u5b9a\u3002";
    }
}
