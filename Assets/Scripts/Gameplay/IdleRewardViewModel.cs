namespace DungeonFit.Gameplay;

public sealed record IdleRewardViewModel(
    int UnclaimedGold,
    int MaxUnclaimedGold,
    int RewardIntervalMinutes,
    bool CanClaim,
    string StatusText);
