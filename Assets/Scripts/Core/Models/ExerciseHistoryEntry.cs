using System;

namespace DungeonFit.Core.Models;

public sealed class ExerciseHistoryEntry
{
    public string ExerciseId { get; set; } = string.Empty;

    public string DungeonTypeId { get; set; } = string.Empty;

    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

    public int PlannedSets { get; set; }

    public int PlannedReps { get; set; }

    public int ActualSets { get; set; }

    public int ActualReps { get; set; }

    public double? WeightKg { get; set; }
}
