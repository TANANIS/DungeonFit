namespace DungeonFit.Core.Models;

public sealed record DungeonCategory(
	string Id,
	string DisplayName,
	string ShortName,
	string ChallengeName,
	int DefaultBpm);
