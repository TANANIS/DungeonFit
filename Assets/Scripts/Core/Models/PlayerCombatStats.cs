namespace DungeonFit.Core.Models;

public sealed record PlayerCombatStats(
    int MaxHp,
    int Attack,
    int EquipmentScore,
    int DungeonGoldBonusPercent = 0);
