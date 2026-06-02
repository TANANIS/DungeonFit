using System;

namespace DungeonFit.Core.Models;

public sealed class PlayerProfile
{
    public const int MinHeightCm = 100;
    public const int MaxHeightCm = 230;

    public int HeightCm { get; set; }

    public string GoalId { get; set; } = FitnessGoal.GeneralHealth;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool HasCompletedOnboarding { get; set; }
}
