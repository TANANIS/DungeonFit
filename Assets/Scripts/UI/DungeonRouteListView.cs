using System;
using System.Collections.Generic;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
using Godot;

namespace DungeonFit.UI;

public sealed class DungeonRouteListView
{
    private readonly VBoxContainer _routeList;
    private readonly DungeonCategoryCatalog _categoryCatalog;
    private readonly MusicCatalog _musicCatalog;
    private readonly ExerciseCatalog _exerciseCatalog = new();

    public DungeonRouteListView(
        VBoxContainer routeList,
        DungeonCategoryCatalog categoryCatalog,
        MusicCatalog musicCatalog)
    {
        _routeList = routeList;
        _categoryCatalog = categoryCatalog;
        _musicCatalog = musicCatalog;
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
            var row = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(0, 76),
            };
            var rowPanel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(0, 76),
            };
            DungeonFitUi.ApplyPanel(rowPanel, UiPanelStyle.Card);

            var label = new Label
            {
                Text = hasRouteSlot
                    ? BuildRouteText(canEditPlan, selectedDungeonRoute, plan, run, index)
                    : $"{index + 1}. {Text.PendingSelection}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            label.AddThemeFontSizeOverride("font_size", hasRouteSlot ? 25 : 26);
            row.AddChild(label);

            if (canEditPlan && hasRouteSlot)
            {
                var routeIndex = index;
                var removeButton = new Button
                {
                    Text = "X",
                    CustomMinimumSize = new Vector2(72, 60),
                };
                removeButton.AddThemeFontSizeOverride("font_size", 28);
                DungeonFitUi.ApplyButton(removeButton, UiButtonStyle.Danger);
                removeButton.Pressed += () => onRemove(routeIndex);
                row.AddChild(removeButton);
            }

            rowPanel.AddChild(row);
            _routeList.AddChild(rowPanel);
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
            return $"{index + 1}. {category.ShortName}{Text.DungeonSuffix} / {exercise.Name}  {slot.TargetSets} x {slot.TargetReps}  {GetMusicName(slot.MusicId)}  休息 {slot.RestSeconds}s";
        }

        var stage = plan.Stages[index];
        var lockedCategory = _categoryCatalog.GetById(stage.DungeonTypeId);
        return $"{GetStageMarker(run, canEditPlan, index)} {index + 1}. {lockedCategory.ShortName}{Text.DungeonSuffix} / {stage.ActionName}  {stage.TotalSets} x {stage.TargetReps}  {GetMusicName(stage.MusicId)}  休息 {stage.RestSeconds}s";
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

    private string GetMusicName(string musicId)
    {
        return _musicCatalog.GetById(musicId).DisplayName;
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
