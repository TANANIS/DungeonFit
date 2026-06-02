namespace DungeonFit.Core.Models;

public sealed record WorkoutTimingProfile(
	int Bpm,
	int BeatsPerRep,
	int TargetReps,
	int TargetSets,
	int RestSeconds)
{
	public double SecondsPerRep => 60.0 / Bpm * BeatsPerRep;

	public double RepsPerMinute => 60.0 / SecondsPerRep;

	public double ActiveSetSeconds => SecondsPerRep * TargetReps;

	public double FullRoomActiveSeconds => ActiveSetSeconds * TargetSets;
}
