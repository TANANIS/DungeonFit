namespace DungeonFit.Core.Models;

public sealed record TaskTemplate(
	string Id,
	string DungeonTypeId,
	string DungeonTypeName,
	string RoomName,
	string ChallengeName,
	string ActionName,
	int TargetReps,
	int TotalSets,
	int Bpm,
	int BeatsPerRep)
{
	public string MusicId { get; init; } = "chest_quest_01";

	public int RestSeconds { get; init; } = 90;

	public int DungeonLevel { get; init; } = 1;
}
