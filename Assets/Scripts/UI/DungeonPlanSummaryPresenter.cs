using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
using Godot;

namespace DungeonFit.UI;

public sealed class DungeonPlanSummaryPresenter
{
    private readonly Label _planSummary;
    private readonly Button _primaryButton;
    private readonly ShortTermQuestCatalog _questCatalog = new();

    public DungeonPlanSummaryPresenter(Label planSummary, Button primaryButton)
    {
        _planSummary = planSummary;
        _primaryButton = primaryButton;
    }

    public void Refresh(
        bool canEditPlan,
        IReadOnlyList<DungeonRouteSlot> selectedDungeonRoute,
        DungeonPlan plan,
        DungeonRun? run,
        DungeonRouteRules routeRules,
        IReadOnlyList<ActiveShortTermQuest> activeQuests)
    {
        var completedStages = run?.CompletedStages ?? 0;
        var selectedSets = canEditPlan ? selectedDungeonRoute.Sum(slot => slot.TargetSets) : plan.TotalSets;
        var routeLine = canEditPlan
            ? BuildEditableRouteStatus(selectedDungeonRoute.Count, selectedSets)
            : string.Format(Text.LockedRouteStatus, completedStages, plan.Stages.Count, plan.TotalSets);

        _planSummary.Text = routeLine;
        _primaryButton.Text = GetPrimaryButtonText(run, selectedDungeonRoute.Count);
        _primaryButton.Disabled = canEditPlan && !routeRules.CanStartRoute(selectedDungeonRoute);
    }

    private string BuildActiveQuestLine(IReadOnlyList<ActiveShortTermQuest> activeQuests)
    {
        if (activeQuests.Count == 0)
        {
            return Text.ActiveQuestNone;
        }

        var validQuests = activeQuests
            .Select(activeQuest => new
            {
                ActiveQuest = activeQuest,
                Definition = _questCatalog.GetById(activeQuest.QuestId),
            })
            .Where(entry => entry.Definition is not null)
            .ToArray();

        if (validQuests.Length == 0)
        {
            return Text.ActiveQuestNone;
        }

        var completedCount = validQuests.Count(entry =>
            !entry.ActiveQuest.IsClaimed &&
            entry.ActiveQuest.Progress >= entry.Definition!.RequiredAmount);

        var activeCount = validQuests.Count(entry => !entry.ActiveQuest.IsClaimed);
        var claimedCount = validQuests.Length - activeCount;

        if (activeCount == 0)
        {
            return string.Format(Text.ActiveQuestClaimedFormat, claimedCount);
        }

        return completedCount == 0
            ? string.Format(Text.ActiveQuestFormat, activeCount)
            : string.Format(Text.ActiveQuestWithCompletedFormat, activeCount, completedCount);
    }

    private static string BuildEditableRouteStatus(int selectedCount, int selectedSets)
    {
        if (selectedCount < DungeonRouteRules.MinRouteSlots)
        {
            return string.Format(Text.RouteNeedsMoreSlots, selectedCount, DungeonRouteRules.MinRouteSlots);
        }

        if (selectedCount >= DungeonRouteRules.MaxRouteSlots)
        {
            return string.Format(Text.RouteFull, selectedCount, DungeonRouteRules.MaxRouteSlots);
        }

        return string.Format(Text.RouteReady, selectedCount, DungeonRouteRules.MinRouteSlots);
    }

    private static string GetPrimaryButtonText(DungeonRun? run, int selectedRouteCount)
    {
        if (run?.IsComplete == true)
        {
            return Text.ViewSummary;
        }

        if (run?.HasStarted == true)
        {
            return Text.ContinueAdventure;
        }

        return selectedRouteCount < DungeonRouteRules.MinRouteSlots
            ? string.Format(Text.NeedSlotsButton, DungeonRouteRules.MinRouteSlots)
            : Text.StartAdventure;
    }

    private static class Text
    {
        public const string ActiveQuestNone = "\u4efb\u52d9\u52a0\u6210\uff1a\u7121";
        public const string ActiveQuestFormat = "\u4efb\u52d9\u52a0\u6210\uff1a{0} \u500b\u9032\u884c\u4e2d";
        public const string ActiveQuestWithCompletedFormat = "\u4efb\u52d9\u52a0\u6210\uff1a{0} \u500b\u9032\u884c\u4e2d / {1} \u500b\u53ef\u56de\u5831";
        public const string ActiveQuestClaimedFormat = "\u4efb\u52d9\u52a0\u6210\uff1a{0} \u500b\u5df2\u5b8c\u6210";
        public const string LockedRouteStatus = "\u5df2\u5b8c\u6210 {0} / {1} \u623f\u9593 / \u9810\u8a08 {2} \u7d44";
        public const string RouteNeedsMoreSlots = "\u2727 \u5df2\u9078 {0} / {1} \u2727";
        public const string RouteReady = "\u2727 \u5df2\u9078 {0} / {1} \u2727";
        public const string RouteFull = "\u2727 \u5df2\u9078 {0} / {1} \u2727";
        public const string ViewSummary = "\u67e5\u770b\u7e3d\u7d50";
        public const string ContinueAdventure = "\u7e7c\u7e8c\u8a0e\u4f10";
        public const string NeedSlotsButton = "\u81f3\u5c11\u9078 {0} \u500b";
        public const string StartAdventure = "\u524d\u5f80\u8a0e\u4f10";
    }
}
