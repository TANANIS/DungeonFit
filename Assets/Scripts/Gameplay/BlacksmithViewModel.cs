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
        var nextCost = SelectedItem is null ? 0 : BlacksmithRules.GetEnhancementCost(currentLevel);
        EnhancementCost = nextCost;
        DismantleRefund = SelectedItem is null ? 0 : BlacksmithRules.GetDismantleRefund(currentLevel);
        CanEnhance = SelectedItem is not null &&
            currentLevel < BlacksmithRules.MaxEnhancementLevel &&
            player.Gold >= nextCost;
        CanDismantleEnhancement = SelectedItem is not null && currentLevel > 0;
        EnhanceDisabledReason = BuildEnhanceDisabledReason(player.Gold, SelectedItem, nextCost);
        DismantleDisabledReason = SelectedItem is null
            ? "請先選擇一件裝備。"
            : currentLevel <= 0 ? "這件裝備尚未強化。" : string.Empty;
    }

    public BlacksmithCharacterSummary Character { get; }

    public IReadOnlyList<BlacksmithItemViewModel> Items { get; }

    public string? SelectedItemId { get; }

    public BlacksmithItemViewModel? SelectedItem { get; }

    public int EnhancementCost { get; }

    public int DismantleRefund { get; }

    public bool CanEnhance { get; }

    public bool CanDismantleEnhancement { get; }

    public string EnhanceDisabledReason { get; }

    public string DismantleDisabledReason { get; }

    private static IReadOnlyList<BlacksmithItemViewModel> BuildItems(PlayerState player)
    {
        return player.Inventory
            .OrderBy(item => GetRarityRank(item.Rarity))
            .ThenByDescending(item => item.Power)
            .ThenBy(item => item.DisplayName)
            .Select(item => BuildItem(player, item))
            .ToArray();
    }

    private static BlacksmithItemViewModel BuildItem(PlayerState player, EquipmentItem item)
    {
        return new BlacksmithItemViewModel(
            item.Id,
            item.DisplayName,
            GetSlotLabel(item.Slot),
            item.Rarity,
            item.Power,
            item.EnhancementLevel,
            BlacksmithRules.MaxEnhancementLevel,
            player.IsEquipped(item.Id),
            item.IsLocked,
            item.Modifiers.Select(FormatModifier).ToArray());
    }

    private static BlacksmithItemViewModel? SelectItem(
        IReadOnlyList<BlacksmithItemViewModel> items,
        string? selectedItemId)
    {
        return items.FirstOrDefault(item => item.Id == selectedItemId) ?? items.FirstOrDefault();
    }

    private static string BuildEnhanceDisabledReason(
        int gold,
        BlacksmithItemViewModel? selectedItem,
        int cost)
    {
        if (selectedItem is null)
        {
            return "請先選擇一件裝備。";
        }

        if (selectedItem.EnhancementLevel >= BlacksmithRules.MaxEnhancementLevel)
        {
            return "這件裝備已達強化上限。";
        }

        if (gold < cost)
        {
            return $"金幣不足，需要 {cost} Gold。";
        }

        return string.Empty;
    }

    private static string FormatModifier(EquipmentModifier modifier)
    {
        var scope = string.IsNullOrWhiteSpace(modifier.Scope)
            ? string.Empty
            : $"{GetDungeonLabel(modifier.Scope)} ";

        return modifier.StatType switch
        {
            EquipmentStatType.Attack => $"攻擊 +{modifier.Value}",
            EquipmentStatType.MaxHp => $"HP +{modifier.Value}",
            EquipmentStatType.DungeonGoldBonusPercent => $"{scope}Gold +{modifier.Value}%",
            EquipmentStatType.QuestRewardBonusPercent => $"支線任務獎勵 +{modifier.Value}%",
            _ => $"+{modifier.Value}",
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
    string SlotLabel,
    string Rarity,
    int Power,
    int EnhancementLevel,
    int MaxEnhancementLevel,
    bool IsEquipped,
    bool IsLocked,
    IReadOnlyList<string> ModifierLines);
