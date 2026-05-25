namespace DungeonFit.Core.Models;

public sealed record RoomProgress(
	int CurrentSet,
	int TotalSets,
	bool IsBossWave,
	bool IsComplete,
	bool IsSkipped)
{
	public int CompletedSets => IsComplete || IsSkipped
		? CurrentSet
		: CurrentSet - 1;
}
