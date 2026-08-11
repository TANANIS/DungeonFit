namespace DungeonFit.Core.Models;

public sealed record ExercisePersonalRecord(
    string ExerciseId,
    double? MaxWeightKg,
    int MaxReps,
    int MaxSets);
