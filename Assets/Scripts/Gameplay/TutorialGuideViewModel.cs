namespace DungeonFit.Gameplay;

public sealed record TutorialGuideViewModel(
    bool IsVisible,
    string StepId,
    string SpeakerName,
    string Title,
    string Body,
    string GoalText,
    string PrimaryActionText,
    string SecondaryActionText)
{
    public static TutorialGuideViewModel Empty { get; } = new(
        false,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
