namespace DungeonFit.Core.Models;

public sealed record ExerciseDefinition(
    string Id,
    string DungeonTypeId,
    string Name,
    string TrainingType,
    string Summary,
    string SafetyNote,
    bool IsRecommended = false);
