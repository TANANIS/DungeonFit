namespace DungeonFit.Core.Models;

public sealed record CombatRepResult(
    int SetNumber,
    int RepNumber,
    bool IsBoss,
    int PlayerHpBefore,
    int PlayerHpAfter,
    int PlayerMaxHp,
    int EnemyHpBefore,
    int EnemyHpAfter,
    int EnemyMaxHp,
    int DamageDealt,
    int DamageTaken,
    bool EnemyAttacked,
    bool EnemyDefeated,
    bool WasEvading,
    bool IsMovingAfterKill);
