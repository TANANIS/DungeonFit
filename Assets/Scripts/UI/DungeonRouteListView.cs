using System;
using System.Collections.Generic;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
using Godot;

namespace DungeonFit.UI;

public sealed class DungeonRouteListView
{
    private const string RowScenePath = "res://Assets/Scenes/UI/DungeonRouteSlotRow.tscn";

    private readonly VBoxContainer _routeList;
    private readonly DungeonCategoryCatalog _categoryCatalog;
    private readonly ExerciseCatalog _exerciseCatalog = new();
    private readonly PackedScene _rowScene;

    public DungeonRouteListView(
        VBoxContainer routeList,
        DungeonCategoryCatalog categoryCatalog,
        MusicCatalog musicCatalog)
    {
        _routeList = routeList;
        _categoryCatalog = categoryCatalog;
        _rowScene = GD.Load<PackedScene>(RowScenePath);
    }

    public void Refresh(
        bool canEditPlan,
        IReadOnlyList<DungeonRouteSlot> selectedDungeonRoute,
        DungeonPlan plan,
        DungeonRun? run,
        Action<int> onRemove)
    {
        ClearChildren(_routeList);

        var visibleRows = canEditPlan
            ? Mathf.Max(selectedDungeonRoute.Count, DungeonRouteRules.MinRouteSlots)
            : plan.Stages.Count;

        for (var index = 0; index < visibleRows; index++)
        {
            var hasRouteSlot = canEditPlan
                ? index < selectedDungeonRoute.Count
                : index < plan.Stages.Count;
            var row = _rowScene.Instantiate<DungeonRouteSlotRowView>();
            row.Initialize(
                index + 1,
                hasRouteSlot
                    ? BuildRouteText(canEditPlan, selectedDungeonRoute, plan, run, index)
                    : Text.PendingSelection,
                canEditPlan && hasRouteSlot);
            if (canEditPlan && hasRouteSlot)
            {
                var routeIndex = index;
                row.RemoveRequested += () => onRemove(routeIndex);
            }

            _routeList.AddChild(row);
        }
    }

    private string BuildRouteText(
        bool canEditPlan,
        IReadOnlyList<DungeonRouteSlot> selectedDungeonRoute,
        DungeonPlan plan,
        DungeonRun? run,
        int index)
    {
        if (canEditPlan)
        {
            var slot = selectedDungeonRoute[index];
            var category = _categoryCatalog.GetById(slot.DungeonTypeId);
            var exercise = _exerciseCatalog.GetById(slot.DungeonTypeId, slot.ExerciseId);
            return $"{category.ShortName}{Text.DungeonSuffix} / {exercise.Name}";
        }

        var stage = plan.Stages[index];
        var lockedCategory = _categoryCatalog.GetById(stage.DungeonTypeId);
        return $"{GetStageMarker(run, canEditPlan, index)} {lockedCategory.ShortName}{Text.DungeonSuffix} / {stage.ActionName}";
    }

    private static string GetStageMarker(DungeonRun? run, bool canEditPlan, int index)
    {
        if (run is null || canEditPlan)
        {
            return "[ ]";
        }

        if (index < run.CompletedStages)
        {
            return "[x]";
        }

        return index == run.CurrentStageIndex && !run.IsComplete ? "[>]" : "[ ]";
    }

    private static void ClearChildren(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static class Text
    {
        public const string DungeonSuffix = "\u5730\u57ce";
        public const string PendingSelection = "\u5f85\u9078\u64c7";
    }
}
