using System.Collections.Generic;
using System.Linq;

namespace DungeonFit.Core.Models;

public sealed record SetSummary(
	RunSummary Run,
	IReadOnlyList<BankedReward> BankedRewards,
	int CompletedStageNumber,
	int TotalStages,
	TaskTemplate? NextStage)
{
	public int BankedChestCount => BankedRewards.Count(reward => reward.IsChest);

	public bool HasNextStage => NextStage is not null;
}
