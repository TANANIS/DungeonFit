namespace DungeonFit.Core.Models;

public sealed record BankedReward(
    string StageId,
    string StageName,
    int SetNumber,
    CompletionResult Result,
    string ChestTier,
    RewardBundle Reward,
    BankedRewardKind Kind = BankedRewardKind.Chest)
{
    public int GoldPreview => Reward.Gold;

    public bool IsChest => Kind == BankedRewardKind.Chest;
}
