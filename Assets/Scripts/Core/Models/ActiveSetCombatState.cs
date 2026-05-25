namespace DungeonFit.Core.Models;

public sealed record ActiveSetCombatState(
    int SetNumber,
    bool IsBoss,
    int PlayerHp,
    int PlayerMaxHp,
    int EnemyHp,
    int EnemyMaxHp,
    int RepsResolved,
    int DamageDealt,
    int DamageTaken,
    bool EnemyDefeated,
    bool IsEvading)
{
    public static ActiveSetCombatState Empty(int playerHp, int playerMaxHp)
    {
        return new ActiveSetCombatState(0, false, playerHp, playerMaxHp, 0, 0, 0, 0, 0, false, playerHp <= 0);
    }
}
