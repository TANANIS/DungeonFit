namespace DungeonFit.Core.Models;

public sealed record EquipmentModifier(
    EquipmentStatType StatType,
    int Value,
    string Scope);
