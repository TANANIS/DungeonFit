using System.Collections.Generic;
using System.Linq;

namespace DungeonFit.Core.Models;

public sealed record RunSummary(
	string Title,
	string RoomName,
	int CompletedSets,
	int TotalSets,
	RewardBundle Reward,
	IReadOnlyList<CompletionResult>? SetResults = null,
	IReadOnlyList<CombatSetResult>? CombatResults = null,
	int? RemainingPlayerHp = null,
	int ExperienceGained = 0,
	int LevelsGained = 0,
	int LevelUpRewardCount = 0,
	int FatigueRewardPercent = 100)
{
	public string RewardText => Reward.Equipment is null
		? HasChest ? $"金幣 +{Reward.Gold}, EXP +{ExperienceGained}, 待開寶箱 {ChestCount}" : $"金幣 +{Reward.Gold}, EXP +{ExperienceGained}"
		: $"金幣 +{Reward.Gold}, EXP +{ExperienceGained}, 待開裝備寶箱";

	public CompletionResult GetSetResult(int setNumber)
	{
		if (setNumber <= 0)
		{
			return CompletionResult.Skipped;
		}

		if (SetResults is not null && SetResults.Count >= setNumber)
		{
			return SetResults[setNumber - 1];
		}

		return setNumber <= CompletedSets ? CompletionResult.Completed : CompletionResult.Skipped;
	}

	public bool HasPartialSet => EffectiveSetResults.Any(result => result == CompletionResult.Partial);

	public int ChestCount => HasCombatResults ? CompletedRoomChestCount : LegacyChestCount;

	public bool HasChest => ChestCount > 0 || Reward.Equipment is not null;

	public bool HasCombatResults => CombatResults is { Count: > 0 };

	public CombatSetResult? GetCombatSetResult(int setNumber)
	{
		if (CombatResults is null || setNumber <= 0 || CombatResults.Count < setNumber)
		{
			return null;
		}

		return CombatResults[setNumber - 1];
	}

	private int LegacyChestCount => IsCompletedRoom
		? System.Math.Min(2, 1 + ((RemainingPlayerHp ?? 0) > 0 && CompletedSets > 1 ? 1 : 0))
		: Reward.Equipment is null ? 0 : 1;

	private int CompletedRoomChestCount => IsCompletedRoom
		? FatigueRewardPercent < 100
			? 1
			: System.Math.Min(2, 1 + ((RemainingPlayerHp ?? 0) > 0 && CompletedSets > 1 ? 1 : 0))
		: 0;

	private bool IsCompletedRoom => TotalSets > 0 &&
		CompletedSets >= TotalSets &&
		Enumerable.Range(1, TotalSets)
			.All(set => GetSetResult(set) != CompletionResult.Skipped);

	private IEnumerable<CompletionResult> EffectiveSetResults =>
		SetResults ?? Enumerable.Repeat(CompletionResult.Completed, CompletedSets);
}
