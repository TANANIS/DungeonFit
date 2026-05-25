namespace DungeonFit.Core.Models;

public sealed record DungeonRouteSlot(
	string DungeonTypeId,
	int TargetSets,
	int TargetReps,
	string MusicId,
	int RestSeconds);
