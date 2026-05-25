using System.Collections.Generic;
using System.Linq;

namespace DungeonFit.Core.Models;

public sealed class EquipmentItem
{
    public string Id { get; set; } = string.Empty;

    public string DefinitionId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public EquipmentSlot Slot { get; set; } = EquipmentSlot.Weapon;

    public string SourceDungeonTypeId { get; set; } = string.Empty;

    public string Rarity { get; set; } = "Common";

    public int Power { get; set; }

    public int SellPrice { get; set; }

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
        string sourceDungeonTypeId,
        string rarity,
        int power,
        int sellPrice,
        IEnumerable<EquipmentModifier> modifiers)
    {
        Id = id;
        DefinitionId = definitionId;
        DisplayName = displayName;
        Slot = slot;
        SourceDungeonTypeId = sourceDungeonTypeId;
        Rarity = rarity;
        Power = power;
        SellPrice = sellPrice;
        Modifiers = modifiers.ToList();
    }
}
