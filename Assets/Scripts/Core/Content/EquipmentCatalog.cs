using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class EquipmentCatalog
{
    private readonly EquipmentDefinition[] _definitions =
    {
        new(
            "moon_iron_shortsword",
            "\u6708\u9435\u77ed\u528d",
            EquipmentSlot.Weapon,
            5,
            80,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 5, string.Empty),
            }),
        new(
            "training_plate",
            "\u898b\u7fd2\u8b77\u7532",
            EquipmentSlot.Armor,
            6,
            75,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 20, string.Empty),
            }),
        new(
            "violet_oath_ring",
            "\u7d2b\u8a93\u6212\u6307",
            EquipmentSlot.Accessory,
            8,
            120,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 3, string.Empty),
            }),
        new(
            "guard_captain_medal",
            "\u5b88\u5099\u968a\u52f3\u7ae0",
            EquipmentSlot.Accessory,
            7,
            110,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 4, string.Empty),
            }),
    };

    public IReadOnlyList<EquipmentDefinition> GetAll()
    {
        return _definitions;
    }

    public EquipmentDefinition GetById(string id)
    {
        return _definitions.FirstOrDefault(definition => definition.Id == id) ?? _definitions[0];
    }

    public EquipmentItem CreateInstance(
        EquipmentDefinition definition,
        DungeonChest chest,
        EquipmentRarity rarity,
        IEnumerable<EquipmentModifier> extraModifiers)
    {
        var modifiers = definition.Modifiers
            .Concat(extraModifiers)
            .ToArray();

        return new EquipmentItem(
            $"{chest.InstanceIdPrefix}_{definition.Id}",
            definition.Id,
            definition.DisplayName,
            definition.Slot,
            chest.SourceDungeonTypeId,
            rarity.DisplayName,
            definition.BasePower + rarity.PowerBonus,
            definition.BaseSellPrice * rarity.SellPriceMultiplier,
            modifiers);
    }
}
