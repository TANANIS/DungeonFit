using System;
using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Rules;

public sealed class LootTable
{
    private static readonly EquipmentRarity Common = new("common", "\u666e\u901a", 0, 1, 0);
    private static readonly EquipmentRarity Rare = new("rare", "\u7a00\u6709", 4, 2, 1);
    private static readonly EquipmentRarity Epic = new("epic", "\u53f2\u8a69", 8, 4, 2);

    private readonly EquipmentCatalog _equipmentCatalog = new();
    private readonly DungeonLootProfileCatalog _profileCatalog = new();

    public RewardBundle RollStagePreview(RoomRun room)
    {
        if (room.IsSkipped)
        {
            return new RewardBundle(RewardSource.DungeonRoom, 10, null);
        }

        var gold = room.CombatResults.Sum(result => result.Gold);
        return new RewardBundle(RewardSource.DungeonRoom, gold, null);
    }

    public RewardBundle RollDungeonChest(DungeonChest chest)
    {
        var profile = _profileCatalog.GetByDungeonType(chest.SourceDungeonTypeId);
        var rule = SelectRule(profile, chest);
        var equipment = rule.DropsEquipment ? RollEquipment(profile, rule, chest) : null;

        return new RewardBundle(RewardSource.DungeonRoom, rule.Gold, equipment);
    }

    private EquipmentItem RollEquipment(DungeonLootProfile profile, DungeonChestLootRule rule, DungeonChest chest)
    {
        var definitionId = RollEquipmentDefinitionId(profile, chest, _equipmentCatalog);
        var definition = _equipmentCatalog.GetById(definitionId);
        var rarity = RollRarity(rule.RarityTable, chest);
        var modifiers = RollExtraModifiers(profile, rule, rarity, chest);

        return _equipmentCatalog.CreateInstance(definition, chest, rarity, modifiers);
    }

    private static DungeonChestLootRule SelectRule(DungeonLootProfile profile, DungeonChest chest)
    {
        if (chest.Tier != "Boss")
        {
            return profile.NormalChest;
        }

        return chest.Result == CompletionResult.Partial
            ? profile.PartialBossChest
            : profile.BossChest;
    }

    private static string RollEquipmentDefinitionId(DungeonLootProfile profile, DungeonChest chest, EquipmentCatalog equipmentCatalog)
    {
        var candidates = equipmentCatalog.GetForDungeon(profile.DungeonTypeId);
        if (candidates.Count == 0)
        {
            return equipmentCatalog.GetAll()[0].Id;
        }

        var slotOffset = chest.SetNumber % 3;
        var slot = slotOffset switch
        {
            1 => EquipmentSlot.Weapon,
            2 => EquipmentSlot.Armor,
            _ => EquipmentSlot.Accessory,
        };
        var slotCandidates = candidates.Where(definition => definition.Slot == slot).ToArray();
        if (slotCandidates.Length == 0)
        {
            slotCandidates = candidates.ToArray();
        }

        var index = StableRoll($"{chest.Id}:{profile.DungeonTypeId}:definition:{slot}", slotCandidates.Length);
        return slotCandidates[index].Id;
    }

    private static EquipmentRarity RollRarity(RarityDropTable table, DungeonChest chest)
    {
        var totalWeight = Math.Max(1, table.TotalWeight);
        var roll = StableRoll($"{chest.Id}:{chest.SourceDungeonTypeId}:rarity:{chest.Result}", totalWeight);

        if (roll < table.CommonWeight)
        {
            return Common;
        }

        if (roll < table.CommonWeight + table.RareWeight)
        {
            return Rare;
        }

        return Epic;
    }

    private static IEnumerable<EquipmentModifier> RollExtraModifiers(
        DungeonLootProfile profile,
        DungeonChestLootRule rule,
        EquipmentRarity rarity,
        DungeonChest chest)
    {
        var count = Math.Max(0, rarity.ExtraModifierCount - rule.ExtraModifierPenalty);
        if (count == 0 || profile.ExtraModifierCandidates.Count == 0)
        {
            return Enumerable.Empty<EquipmentModifier>();
        }

        var roll = StableRoll($"{chest.Id}:{profile.DungeonTypeId}:modifier:{chest.Result}", profile.ExtraModifierCandidates.Count);
        return profile.ExtraModifierCandidates
            .Skip(roll)
            .Concat(profile.ExtraModifierCandidates.Take(roll))
            .Take(count);
    }

    private static int StableRoll(string seed, int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            return 0;
        }

        var hash = 17;
        foreach (var character in seed)
        {
            hash = (hash * 31) + character;
        }

        return Math.Abs(hash) % maxExclusive;
    }
}
