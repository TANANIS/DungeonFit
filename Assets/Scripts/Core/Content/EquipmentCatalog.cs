using System;
using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class EquipmentCatalog
{
    private static readonly GearTemplate[] Templates =
    {
        new(
            "blade",
            "刃",
            EquipmentSlot.Weapon,
            "res://Assets/Art/Items/Weapons/moon_blade.png",
            0,
            0,
            new EquipmentModifier(EquipmentStatType.Attack, 5, string.Empty)),
        new(
            "hammer",
            "鎚",
            EquipmentSlot.Weapon,
            "res://Assets/Art/Items/Weapons/war_hammer.png",
            1,
            5,
            new EquipmentModifier(EquipmentStatType.Attack, 6, string.Empty)),
        new(
            "bow",
            "弓",
            EquipmentSlot.Weapon,
            "res://Assets/Art/Items/Weapons/training_bow.png",
            0,
            10,
            new EquipmentModifier(EquipmentStatType.Attack, 4, string.Empty)),
        new(
            "dagger",
            "匕",
            EquipmentSlot.Weapon,
            "res://Assets/Art/Items/Weapons/silver_dagger.png",
            -1,
            -5,
            new EquipmentModifier(EquipmentStatType.Attack, 4, string.Empty)),
        new(
            "guard",
            "甲",
            EquipmentSlot.Armor,
            "res://Assets/Art/Items/Armor/guard_plate.png",
            1,
            -5,
            new EquipmentModifier(EquipmentStatType.MaxHp, 20, string.Empty)),
        new(
            "helm",
            "盔",
            EquipmentSlot.Armor,
            "res://Assets/Art/Items/Armor/iron_helm.png",
            0,
            -10,
            new EquipmentModifier(EquipmentStatType.MaxHp, 16, string.Empty)),
        new(
            "shield",
            "盾",
            EquipmentSlot.Armor,
            "res://Assets/Art/Items/Armor/round_shield.png",
            2,
            5,
            new EquipmentModifier(EquipmentStatType.MaxHp, 18, string.Empty)),
        new(
            "plate",
            "鎧",
            EquipmentSlot.Armor,
            "res://Assets/Art/Items/Armor/stone_plate.png",
            2,
            10,
            new EquipmentModifier(EquipmentStatType.MaxHp, 24, string.Empty)),
        new(
            "charm",
            "符",
            EquipmentSlot.Accessory,
            "res://Assets/Art/Items/Accessories/oath_charm.png",
            2,
            20,
            new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 3, string.Empty)),
        new(
            "crystal",
            "晶",
            EquipmentSlot.Accessory,
            "res://Assets/Art/Items/Accessories/focus_crystal.png",
            1,
            25,
            new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 2, string.Empty)),
        new(
            "ring",
            "戒",
            EquipmentSlot.Accessory,
            "res://Assets/Art/Items/Accessories/golden_ring.png",
            2,
            30,
            new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 2, "{dungeon}")),
        new(
            "medal",
            "章",
            EquipmentSlot.Accessory,
            "res://Assets/Art/Items/Accessories/guard_medal.png",
            1,
            15,
            new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 4, string.Empty)),
    };

    private static readonly GearAffix[] Affixes =
    {
        new("vanguard", "先鋒", new EquipmentModifier(EquipmentStatType.Attack, 2, string.Empty)),
        new("fortitude", "堅韌", new EquipmentModifier(EquipmentStatType.MaxHp, 12, string.Empty)),
        new("prospector", "尋寶", new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 3, "{dungeon}")),
        new("oath", "誓約", new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 2, string.Empty)),
    };

    private static readonly DungeonGearTheme[] Themes =
    {
        new("chest", "胸城", 1, 5, 5, 80),
        new("shoulders", "月肩", 1, 6, 6, 90),
        new("back", "背影", 2, 7, 7, 105),
        new("legs", "深步", 2, 8, 7, 115),
        new("core", "核心", 3, 9, 8, 135),
        new("arms", "遠行", 3, 10, 9, 145),
    };

    private readonly EquipmentDefinition[] _definitions = BuildDefinitions();

    public IReadOnlyList<EquipmentDefinition> GetAll()
    {
        return _definitions;
    }

    public IReadOnlyList<EquipmentDefinition> GetForDungeon(string dungeonTypeId)
    {
        var matches = _definitions
            .Where(definition => definition.Id.StartsWith($"{dungeonTypeId}_", StringComparison.Ordinal))
            .ToArray();
        return matches.Length > 0 ? matches : GetForDungeon("chest");
    }

    public IReadOnlyList<string> GetDefinitionIdsForDungeon(string dungeonTypeId)
    {
        return GetForDungeon(dungeonTypeId)
            .Select(definition => definition.Id)
            .ToArray();
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
            definition.IconPath,
            chest.SourceDungeonTypeId,
            rarity.DisplayName,
            definition.RecommendedLevelMin,
            definition.RecommendedLevelMax,
            definition.BasePower + rarity.PowerBonus,
            definition.BaseSellPrice * rarity.SellPriceMultiplier,
            modifiers);
    }

    private static EquipmentDefinition[] BuildDefinitions()
    {
        return Themes
            .SelectMany(theme => Templates.SelectMany(template => Affixes.Select(affix => BuildDefinition(theme, template, affix))))
            .ToArray();
    }

    private static EquipmentDefinition BuildDefinition(DungeonGearTheme theme, GearTemplate template, GearAffix affix)
    {
        return new EquipmentDefinition(
            $"{theme.DungeonTypeId}_{template.IdSuffix}_{affix.IdSuffix}",
            $"{theme.NamePrefix}{affix.NamePrefix}{template.NameSuffix}",
            template.Slot,
            template.IconPath,
            theme.LevelMin,
            theme.LevelMax,
            theme.BasePower + template.PowerOffset,
            theme.BaseSellPrice + template.SellPriceOffset,
            new[]
            {
                ResolveAffixModifier(theme.DungeonTypeId, template.BaseModifier),
                ResolveAffixModifier(theme.DungeonTypeId, affix.Modifier),
            });
    }

    private static EquipmentModifier ResolveAffixModifier(string dungeonTypeId, EquipmentModifier modifier)
    {
        return modifier.Scope == "{dungeon}"
            ? modifier with { Scope = dungeonTypeId }
            : modifier;
    }

    private sealed record DungeonGearTheme(
        string DungeonTypeId,
        string NamePrefix,
        int LevelMin,
        int LevelMax,
        int BasePower,
        int BaseSellPrice);

    private sealed record GearTemplate(
        string IdSuffix,
        string NameSuffix,
        EquipmentSlot Slot,
        string IconPath,
        int PowerOffset,
        int SellPriceOffset,
        EquipmentModifier BaseModifier);

    private sealed record GearAffix(
        string IdSuffix,
        string NamePrefix,
        EquipmentModifier Modifier);
}
