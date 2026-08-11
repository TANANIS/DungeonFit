using System.Collections.Generic;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;

namespace DungeonFit.Core.Content;

public sealed class TaskCatalog
{
    private readonly DungeonCategoryCatalog _categoryCatalog = new();
    private readonly ExerciseCatalog _exerciseCatalog = new();
    private readonly DungeonRouteRules _routeRules = new();

    public DungeonPlan GetDefaultPlan()
    {
        return CreateDungeonPlanFromRoute(new[]
        {
            _routeRules.CreateDefaultSlot("chest"),
            _routeRules.CreateDefaultSlot("shoulders"),
            _routeRules.CreateDefaultSlot("chest"),
            _routeRules.CreateDefaultSlot("arms"),
        });
    }

    public DungeonPlan CreateDungeonPlanFromRoute(IEnumerable<DungeonRouteSlot> routeSlots)
    {
        var route = _routeRules.NormalizeRoute(routeSlots);
        if (route.Count == 0)
        {
            return DungeonPlan.Empty;
        }

        var stages = new List<TaskTemplate>();

        for (var index = 0; index < route.Count; index++)
        {
            stages.Add(CreateGenericStage(route[index], index));
        }

        return new DungeonPlan("today_dungeon_route", "今日討伐路線", stages);
    }

    private TaskTemplate CreateGenericStage(DungeonRouteSlot slot, int index)
    {
        var category = _categoryCatalog.GetById(slot.DungeonTypeId);
        var exercise = _exerciseCatalog.GetById(slot.DungeonTypeId, slot.ExerciseId);
        var timing = _routeRules.CreateTimingProfile(slot, category.DefaultBpm);

        return new TaskTemplate(
            $"route_slot_{index + 1}_{slot.DungeonTypeId}",
            slot.DungeonTypeId,
            category.DisplayName,
            $"{category.DisplayName} - {exercise.Name}",
            category.ChallengeName,
            exercise.Name,
            timing.TargetReps,
            timing.TargetSets,
            timing.Bpm,
            timing.BeatsPerRep)
        {
            MusicId = slot.MusicId,
            RestSeconds = timing.RestSeconds,
            ExerciseId = exercise.Id,
        };
    }
}
