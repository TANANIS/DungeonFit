using System.Collections.Generic;
using System.Linq;

namespace DungeonFit.Core.Models;

public sealed class DungeonPlan
{
    public static DungeonPlan Empty { get; } = new("empty_today_dungeon_route", "Today Dungeon Route", System.Array.Empty<TaskTemplate>());

    public DungeonPlan(string id, string displayName, IEnumerable<TaskTemplate> stages)
    {
        Id = id;
        DisplayName = displayName;
        Stages = stages.ToArray();
    }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<TaskTemplate> Stages { get; }

    public int TotalSets => Stages.Sum(stage => stage.TotalSets);
}
