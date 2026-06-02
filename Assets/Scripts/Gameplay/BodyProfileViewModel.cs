namespace DungeonFit.Gameplay;

public sealed record BodyProfileViewModel(
    bool HasCompletedOnboarding,
    int HeightCm,
    string GoalId,
    string GoalLabel,
    string GoalAdvice,
    double? TodayWeightKg,
    string TodayStatusText)
{
    public static BodyProfileViewModel Empty { get; } = new(
        false,
        0,
        Core.Models.FitnessGoal.GeneralHealth,
        Core.Models.FitnessGoal.GetLabel(Core.Models.FitnessGoal.GeneralHealth),
        Core.Models.FitnessGoal.GetAdvice(Core.Models.FitnessGoal.GeneralHealth),
        null,
        "今日尚未記錄體重");
}
