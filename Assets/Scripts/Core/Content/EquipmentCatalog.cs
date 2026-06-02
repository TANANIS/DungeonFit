using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class EquipmentCatalog
{
    private readonly EquipmentDefinition[] _definitions =
    {
        new(
            "chest_vanguard_blade",
            "\u80f8\u57ce\u5148\u92d2\u528d",
            EquipmentSlot.Weapon,
            1,
            5,
            5,
            80,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 5, string.Empty),
            }),
        new(
            "chest_guard_plate",
            "\u80f8\u57ce\u5b88\u5099\u7532",
            EquipmentSlot.Armor,
            1,
            5,
            6,
            75,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 20, string.Empty),
            }),
        new(
            "chest_oath_ring",
            "\u7d2b\u8a93\u6212\u6307",
            EquipmentSlot.Accessory,
            1,
            5,
            8,
            120,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 3, string.Empty),
            }),
        new(
            "shoulder_moon_halberd",
            "\u6708\u80a9\u6230\u621f",
            EquipmentSlot.Weapon,
            1,
            6,
            6,
            95,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 6, string.Empty),
            }),
        new(
            "shoulder_guard_mantle",
            "\u5b88\u671b\u62ab\u80a9",
            EquipmentSlot.Armor,
            1,
            6,
            6,
            90,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 18, string.Empty),
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 2, "shoulders"),
            }),
        new(
            "shoulder_captain_medal",
            "\u5b88\u5099\u968a\u52f3\u7ae0",
            EquipmentSlot.Accessory,
            1,
            6,
            7,
            110,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 4, string.Empty),
            }),
        new(
            "back_shadow_axe",
            "\u80cc\u5f71\u6230\u65a7",
            EquipmentSlot.Weapon,
            2,
            7,
            7,
            115,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 7, string.Empty),
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 2, "back"),
            }),
        new(
            "back_watcher_coat",
            "\u9ed1\u7ffc\u5b88\u671b\u9577\u8863",
            EquipmentSlot.Armor,
            2,
            7,
            7,
            105,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 22, string.Empty),
            }),
        new(
            "back_raven_charm",
            "\u9d09\u7fbd\u8b77\u7b26",
            EquipmentSlot.Accessory,
            2,
            7,
            8,
            130,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 4, "back"),
            }),
        new(
            "leg_miner_pick",
            "\u7926\u5de5\u6708\u9435\u93ac",
            EquipmentSlot.Weapon,
            2,
            8,
            7,
            115,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 6, string.Empty),
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 3, "legs"),
            }),
        new(
            "leg_deepstride_greaves",
            "\u6df1\u6b65\u8b77\u817f\u7532",
            EquipmentSlot.Armor,
            2,
            8,
            8,
            125,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 26, string.Empty),
            }),
        new(
            "leg_ore_badge",
            "\u6df1\u5c64\u7926\u77f3\u5fbd\u7ae0",
            EquipmentSlot.Accessory,
            2,
            8,
            8,
            125,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 4, string.Empty),
            }),
        new(
            "core_moon_staff",
            "\u6708\u767d\u7948\u6756",
            EquipmentSlot.Weapon,
            3,
            9,
            8,
            135,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 6, string.Empty),
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 3, string.Empty),
            }),
        new(
            "core_stability_vest",
            "\u6838\u5fc3\u7a69\u5b9a\u8b77\u8863",
            EquipmentSlot.Armor,
            3,
            9,
            9,
            140,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 30, string.Empty),
            }),
        new(
            "core_prayer_beads",
            "\u6708\u767d\u7948\u73e0",
            EquipmentSlot.Accessory,
            3,
            9,
            9,
            150,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 5, string.Empty),
            }),
        new(
            "arm_courier_sabre",
            "\u4fe1\u4f7f\u77ed\u5203",
            EquipmentSlot.Weapon,
            3,
            10,
            9,
            145,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 8, string.Empty),
            }),
        new(
            "arm_parcel_guard",
            "\u5305\u88f9\u5b88\u885b\u8b77\u7532",
            EquipmentSlot.Armor,
            3,
            10,
            9,
            145,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 24, string.Empty),
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 3, "arms"),
            }),
        new(
            "arm_signal_bracelet",
            "\u9060\u884c\u4fe1\u865f\u624b\u74b0",
            EquipmentSlot.Accessory,
            3,
            10,
            10,
            160,
            new[]
            {
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 5, "arms"),
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
            definition.RecommendedLevelMin,
            definition.RecommendedLevelMax,
            definition.BasePower + rarity.PowerBonus,
            definition.BaseSellPrice * rarity.SellPriceMultiplier,
            modifiers);
    }
}
