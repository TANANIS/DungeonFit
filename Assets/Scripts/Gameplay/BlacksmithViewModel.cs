using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Gameplay;

public sealed class BlacksmithViewModel
{
    public BlacksmithViewModel(PlayerState player, string? selectedItemId)
    {
        Character = new BlacksmithCharacterSummary(
            "冒險者",
            player.Level,
            player.Experience,
            player.ExperienceToNextLevel,
            player.Gold,
            player.EquipmentScore);
        Items = BuildItems(player);
        SelectedItem = SelectItem(Items, selectedItemId);
        SelectedItemId = SelectedItem?.Id;
        var currentLevel = SelectedItem?.EnhancementLevel ?? 0;
        EnhancementCost = SelectedItem is null ? 0 : BlacksmithRules.GetEnhancementCost(currentLevel);
        DismantleRefund = SelectedItem is null ? 0 : BlacksmithRules.GetDismantleRefund(currentLevel);
        LevelExtensionCost = SelectedItem is null ? 0 : BlacksmithRules.GetLevelExtensionCost(SelectedItem.LevelExtension);
        CanEnhance = SelectedItem is not null &&
            currentLevel < BlacksmithRules.MaxEnhancementLevel &&
            player.Gold >= EnhancementCost;
        CanExtendLevelRange = SelectedItem is not null &&
            SelectedItem.LevelExtension < BlacksmithRules.MaxLevelExtension &&
            player.Gold >= LevelExtensionCost;
        CanDismantleEnhancement = SelectedItem is not null && currentLevel > 0;
    }

    public BlacksmithCharacterSummary Character { get; }

    public IReadOnlyList<BlacksmithItemViewModel> Items { get; }

    public string? SelectedItemId { get; }

    public BlacksmithItemViewModel? SelectedItem { get; }

    public int EnhancementCost { get; }

    public int LevelExtensionCost { get; }

    public int DismantleRefund { get; }

    public bool CanEnhance { get; }

    public bool CanExtendLevelRange { get; }

    public bool CanDismantleEnhancement { get; }

    private static IReadOnlyList<BlacksmithItemViewModel> BuildItems(PlayerState player)
    {
        return player.Inventory
            .OrderBy(item => GetRarityRank(item.Rarity))
            .ThenByDescending(item => item.GetEffectivePower(player.Level))
            .ThenBy(item => item.DisplayName)
            .Select(item => BuildItem(player, item))
            .ToArray();
    }

    private static BlacksmithItemViewModel BuildItem(PlayerState player, EquipmentItem item)
    {
        var effectivePower = item.GetEffectivePower(player.Level);
        return new BlacksmithItemViewModel(
            item.Id,
            item.DisplayName,
            item.IconPath,
            GetSlotLabel(item.Slot),
            item.Rarity,
            item.Power,
            effectivePower,
            item.EnhancementLevel,
            BlacksmithRules.MaxEnhancementLevel,
            item.RecommendedLevelMin,
            item.RecommendedLevelMax,
            item.EffectiveRecommendedLevelMax,
            item.LevelExtension,
            BlacksmithRules.MaxLevelExtension,
            item.IsWithinRecommendedLevel(player.Level),
            player.IsEquipped(item.Id),
            item.IsLocked,
            item.Modifiers.Select(modifier => FormatModifier(item, modifier, player.Level)).ToArray());
    }

    private static BlacksmithItemViewModel? SelectItem(
        IReadOnlyList<BlacksmithItemViewModel> items,
        string? selectedItemId)
    {
        return items.FirstOrDefault(item => item.Id == selectedItemId) ?? items.FirstOrDefault();
    }

    private static string FormatModifier(EquipmentItem item, EquipmentModifier modifier, int playerLevel)
    {
        var scope = string.IsNullOrWhiteSpace(modifier.Scope)
            ? string.Empty
            : $"{GetDungeonLabel(modifier.Scope)} ";
        var value = item.GetEffectiveModifierValue(modifier, playerLevel);
        var decayLabel = value == modifier.Value ? string.Empty : $"（原 +{modifier.Value}）";

        return modifier.StatType switch
        {
            EquipmentStatType.Attack => $"攻擊 +{value}{decayLabel}",
            EquipmentStatType.MaxHp => $"HP +{value}{decayLabel}",
            EquipmentStatType.DungeonGoldBonusPercent => $"{scope}Gold +{value}%{decayLabel}",
            EquipmentStatType.QuestRewardBonusPercent => $"支線任務獎勵 +{value}%{decayLabel}",
            _ => $"+{value}{decayLabel}",
        };
    }

    private static string GetSlotLabel(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => "武器",
            EquipmentSlot.Armor => "護甲",
            EquipmentSlot.Accessory => "飾品",
            _ => "裝備",
        };
    }

    private static string GetDungeonLabel(string dungeonTypeId)
    {
        return dungeonTypeId switch
        {
            "chest" => "胸部地城",
            "shoulders" => "肩部地城",
            "back" => "背部地城",
            "legs" => "腿部地城",
            "core" => "核心地城",
            "arms" => "手臂地城",
            _ => "地城",
        };
    }

    private static int GetRarityRank(string rarity)
    {
        return rarity switch
        {
            "史詩" => 0,
            "稀有" => 1,
            "普通" => 2,
            _ => 3,
        };
    }
}

public sealed record BlacksmithCharacterSummary(
    string Name,
    int Level,
    int Experience,
    int ExperienceToNextLevel,
    int Gold,
    int EquipmentScore);

public sealed record BlacksmithItemViewModel(
    string Id,
    string DisplayName,
    string IconPath,
    string SlotLabel,
    string Rarity,
    int Power,
    int EffectivePower,
    int EnhancementLevel,
    int MaxEnhancementLevel,
    int RecommendedLevelMin,
    int RecommendedLevelMax,
    int EffectiveRecommendedLevelMax,
    int LevelExtension,
    int MaxLevelExtension,
    bool IsWithinRecommendedLevel,
    bool IsEquipped,
    bool IsLocked,
    IReadOnlyList<string> ModifierLines);
