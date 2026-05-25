namespace DungeonFit.Core.Models;

public sealed record EnemyDefinition(
    string Id,
    string DisplayName,
    string BossName,
    int NormalMaxHp,
    int NormalAttack,
    int BossMaxHp,
    int BossAttack)
{
    public int GetNormalMaxHp(int level)
    {
        return ScaleHp(NormalMaxHp, level);
    }

    public int GetBossMaxHp(int level)
    {
        return ScaleHp(BossMaxHp, level);
    }

    public int GetNormalAttack(int level)
    {
        return ScaleAttack(NormalAttack, level);
    }

    public int GetBossAttack(int level)
    {
        return ScaleAttack(BossAttack, level);
    }

    private static int ScaleHp(int baseHp, int level)
    {
        var safeLevel = System.Math.Max(1, level);
        return (int)System.Math.Round(baseHp * (1 + ((safeLevel - 1) * 0.08)));
    }

    private static int ScaleAttack(int baseAttack, int level)
    {
        var safeLevel = System.Math.Max(1, level);
        return baseAttack + ((safeLevel - 1) / 3);
    }
}
