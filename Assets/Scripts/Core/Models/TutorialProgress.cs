namespace DungeonFit.Core.Models;

public static class TutorialStepIds
{
    public const string Welcome = "welcome";
    public const string PlanRoute = "plan_route";
    public const string ClearRoom = "clear_room";
    public const string ClaimRewards = "claim_rewards";
    public const string VisitTavern = "visit_tavern";
    public const string Completed = "completed";

    public static bool IsValid(string stepId)
    {
        return stepId is Welcome or PlanRoute or ClearRoom or ClaimRewards or VisitTavern or Completed;
    }
}

public sealed class TutorialProgress
{
    public string StepId { get; set; } = TutorialStepIds.Welcome;

    public bool IsSkipped { get; set; }

    public bool IsCompleted { get; set; }

    public static TutorialProgress Completed()
    {
        return new TutorialProgress
        {
            StepId = TutorialStepIds.Completed,
            IsCompleted = true,
        };
    }
}
