namespace DungeonFit.Core.Models;

public sealed record CombatSetResult(
    int SetNumber,
    bool IsBoss,
    CompletionResult Result,
    BankedRewardKind RewardKind,
    string ChestTier,
    int Gold,
    int PlayerHpBefore,
    int PlayerHpAfter,
    int EnemyMaxHp,
    int EnemyHpAfter,
    int EnemyAttack,
    int DamageDealt,
    int DamageTaken,
    bool EnemyDefeated,
    bool WasEvading);
