namespace DungeonFit.Core.Models;

public sealed record EquipmentRarity(
    string Id,
    string DisplayName,
    int PowerBonus,
    int SellPriceMultiplier,
    int ExtraModifierCount);
