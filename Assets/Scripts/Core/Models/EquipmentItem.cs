using System.Collections.Generic;
using System.Linq;

namespace DungeonFit.Core.Models;

public sealed class EquipmentItem
{
    public string Id { get; set; } = string.Empty;

    public string DefinitionId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public EquipmentSlot Slot { get; set; } = EquipmentSlot.Weapon;

    public string IconPath { get; set; } = string.Empty;

    public string SourceDungeonTypeId { get; set; } = string.Empty;

    public string Rarity { get; set; } = "Common";

    public int RecommendedLevelMin { get; set; } = 1;

    public int RecommendedLevelMax { get; set; } = 5;

    public int LevelExtension { get; set; }

    public int Power { get; set; }

    public int SellPrice { get; set; }

    public int EnhancementLevel { get; set; }

    public bool IsLocked { get; set; }

    public List<EquipmentModifier> Modifiers { get; set; } = new();

    public EquipmentItem()
    {
    }

    public EquipmentItem(
        string id,
        string definitionId,
        string displayName,
        EquipmentSlot slot,
        string iconPath,
        string sourceDungeonTypeId,
        string rarity,
        int recommendedLevelMin,
        int recommendedLevelMax,
        int power,
        int sellPrice,
        IEnumerable<EquipmentModifier> modifiers)
    {
        Id = id;
        DefinitionId = definitionId;
        DisplayName = displayName;
        Slot = slot;
        IconPath = iconPath;
        SourceDungeonTypeId = sourceDungeonTypeId;
        Rarity = rarity;
        RecommendedLevelMin = recommendedLevelMin;
        RecommendedLevelMax = recommendedLevelMax;
        Power = power;
        SellPrice = sellPrice;
        Modifiers = modifiers.ToList();
    }

    public int EffectiveRecommendedLevelMax => RecommendedLevelMax + LevelExtension;

    public bool IsWithinRecommendedLevel(int playerLevel)
    {
        return playerLevel >= RecommendedLevelMin && playerLevel <= EffectiveRecommendedLevelMax;
    }

    public int GetEffectivePower(int playerLevel)
    {
        return IsWithinRecommendedLevel(playerLevel)
            ? Power
            : System.Math.Max(1, (int)System.Math.Ceiling(Power * 0.5));
    }

    public int GetEffectiveModifierValue(EquipmentModifier modifier, int playerLevel)
    {
        return IsWithinRecommendedLevel(playerLevel)
            ? modifier.Value
            : System.Math.Max(1, (int)System.Math.Ceiling(modifier.Value * 0.5));
    }
}
