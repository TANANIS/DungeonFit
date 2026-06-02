using System;

namespace DungeonFit.Core.Models;

public sealed class BodyMetricEntry
{
    public const double MinWeightKg = 30.0;
    public const double MaxWeightKg = 250.0;

    public string DateKey { get; set; } = string.Empty;

    public double WeightKg { get; set; }

    public DateTime RecordedAtUtc { get; set; }
}
