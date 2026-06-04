using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Gameplay;

public sealed class TavernEquipmentViewModel
{
    public TavernEquipmentViewModel(PlayerState player, EquipmentInventoryFilter filter, EquipmentInventorySort sort)
    {
        Character = new TavernCharacterSummary(
            "\u5192\u96aa\u8005",
            player.Level,
            player.CurrentHp,
            player.MaxHp,
            player.Experience,
            player.ExperienceToNextLevel,
            player.Gold,
            player.EquipmentScore,
            player.Attack);
        EquippedSlots = BuildEquippedSlots(player);
        CurrentBonusLines = BuildCurrentBonusLines(player);
        AllInventoryItems = BuildInventory(player, EquipmentInventoryFilter.All, sort);
        InventoryItems = BuildInventory(player, filter, sort);
        InventoryCount = player.Inventory.Count;
        SellableCount = InventoryItems.Count(item => item.CanSell);
        SellableValue = InventoryItems.Where(item => item.CanSell).Sum(item => item.SellPrice);
        CommonSellableCount = AllInventoryItems.Count(item => item.Rarity == "\u666e\u901a" && item.CanSell);
        CommonSellableValue = AllInventoryItems
            .Where(item => item.Rarity == "\u666e\u901a" && item.CanSell)
            .Sum(item => item.SellPrice);
        RareUnlockedCount = AllInventoryItems
            .Count(item => IsRareOrBetter(item.Rarity) && !item.IsLocked && !item.IsEquipped);
        Filter = filter;
        Sort = sort;
    }

    public TavernCharacterSummary Character { get; }

    public IReadOnlyList<TavernEquippedSlotViewModel> EquippedSlots { get; }

    public IReadOnlyList<string> CurrentBonusLines { get; }

    public IReadOnlyList<TavernInventoryItemViewModel> InventoryItems { get; }

    public IReadOnlyList<TavernInventoryItemViewModel> AllInventoryItems { get; }

    public int InventoryCount { get; }

    public int SellableCount { get; }

    public int SellableValue { get; }

    public int CommonSellableCount { get; }

    public int CommonSellableValue { get; }

    public int RareUnlockedCount { get; }

    public EquipmentInventoryFilter Filter { get; }

    public EquipmentInventorySort Sort { get; }

    private static IReadOnlyList<TavernEquippedSlotViewModel> BuildEquippedSlots(PlayerState player)
    {
        return new[]
        {
            BuildSlot(player, EquipmentSlot.Weapon, "\u6b66\u5668"),
            BuildSlot(player, EquipmentSlot.Armor, "\u8b77\u7532"),
            BuildSlot(player, EquipmentSlot.Accessory, "\u98fe\u54c1"),
        };
    }

    private static TavernEquippedSlotViewModel BuildSlot(PlayerState player, EquipmentSlot slot, string label)
    {
        var itemId = player.Loadout.GetEquippedId(slot);
        var item = itemId is null
            ? null
            : player.Inventory.FirstOrDefault(equipment => equipment.Id == itemId);

        return new TavernEquippedSlotViewModel(
            slot,
            label,
            item is null ? null : BuildItem(player, item));
    }

    private static IReadOnlyList<TavernInventoryItemViewModel> BuildInventory(
        PlayerState player,
        EquipmentInventoryFilter filter,
        EquipmentInventorySort sort)
    {
        var items = player.Inventory.AsEnumerable();

        if (filter != EquipmentInventoryFilter.All)
        {
            var slot = filter switch
            {
                EquipmentInventoryFilter.Weapon => EquipmentSlot.Weapon,
                EquipmentInventoryFilter.Armor => EquipmentSlot.Armor,
                EquipmentInventoryFilter.Accessory => EquipmentSlot.Accessory,
                _ => EquipmentSlot.Weapon,
            };
            items = items.Where(item => item.Slot == slot);
        }

        items = sort switch
        {
            EquipmentInventorySort.Power => items.OrderByDescending(item => item.GetEffectivePower(player.Level)).ThenBy(item => item.DisplayName),
            EquipmentInventorySort.SellPrice => items.OrderByDescending(item => item.SellPrice).ThenBy(item => item.DisplayName),
            EquipmentInventorySort.Type => items.OrderBy(item => item.Slot).ThenByDescending(item => item.GetEffectivePower(player.Level)),
            _ => items.OrderBy(item => GetRarityRank(item.Rarity)).ThenByDescending(item => item.GetEffectivePower(player.Level)),
        };

        return items
            .Select(item => BuildItem(player, item))
            .ToArray();
    }

    private static TavernInventoryItemViewModel BuildItem(PlayerState player, EquipmentItem item)
    {
        var isEquipped = player.IsEquipped(item.Id);
        return new TavernInventoryItemViewModel(
            item.Id,
            item.DisplayName,
            item.Slot,
            item.IconPath,
            GetSlotLabel(item.Slot),
            item.Rarity,
            item.Power,
            item.GetEffectivePower(player.Level),
            $"Lv.{item.RecommendedLevelMin}-{item.EffectiveRecommendedLevelMax}",
            item.IsWithinRecommendedLevel(player.Level),
            item.SellPrice,
            item.IsLocked,
            isEquipped,
            !isEquipped,
            isEquipped,
            !item.IsLocked && !isEquipped,
            item.SourceDungeonTypeId,
            item.Modifiers.Select(FormatModifier).ToArray());
    }

    private static IReadOnlyList<string> BuildCurrentBonusLines(PlayerState player)
    {
        var equippedItems = player.GetEquippedItems();
        if (equippedItems.Count == 0)
        {
            return new[] { "\u76ee\u524d\u6c92\u6709\u88dd\u5099\u52a0\u6210\u3002" };
        }

        var lines = equippedItems
            .SelectMany(item => item.Modifiers)
            .GroupBy(modifier => new { modifier.StatType, modifier.Scope })
            .Select(group => FormatModifier(new EquipmentModifier(
                group.Key.StatType,
                group.Sum(modifier => modifier.Value),
                group.Key.Scope)))
            .ToList();

        if (player.EquipmentScore > 0)
        {
            lines.Insert(0, $"\u6230\u529b +{player.EquipmentScore}");
        }

        return lines;
    }

    private static string FormatModifier(EquipmentModifier modifier)
    {
        var scope = string.IsNullOrWhiteSpace(modifier.Scope)
            ? string.Empty
            : $"{GetDungeonLabel(modifier.Scope)} ";

        return modifier.StatType switch
        {
            EquipmentStatType.Attack => $"\u653b\u64ca +{modifier.Value}",
            EquipmentStatType.MaxHp => $"HP +{modifier.Value}",
            EquipmentStatType.DungeonGoldBonusPercent => $"{scope}Gold +{modifier.Value}%",
            EquipmentStatType.QuestRewardBonusPercent => $"\u652f\u7dda\u4efb\u52d9\u734e\u52f5 +{modifier.Value}%",
            _ => $"+{modifier.Value}",
        };
    }

    private static string GetSlotLabel(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => "\u6b66\u5668",
            EquipmentSlot.Armor => "\u8b77\u7532",
            EquipmentSlot.Accessory => "\u98fe\u54c1",
            _ => "\u88dd\u5099",
        };
    }

    private static string GetDungeonLabel(string dungeonTypeId)
    {
        return dungeonTypeId switch
        {
            "chest" => "\u80f8\u90e8\u5730\u57ce",
            "shoulders" => "\u80a9\u90e8\u5730\u57ce",
            "back" => "\u80cc\u90e8\u5730\u57ce",
            "legs" => "\u817f\u90e8\u5730\u57ce",
            "core" => "\u6838\u5fc3\u5730\u57ce",
            "arms" => "\u624b\u81c2\u5730\u57ce",
            _ => "\u5730\u57ce",
        };
    }

    private static int GetRarityRank(string rarity)
    {
        return rarity switch
        {
            "\u53f2\u8a69" => 0,
            "\u7a00\u6709" => 1,
            "\u666e\u901a" => 2,
            _ => 3,
        };
    }

    private static bool IsRareOrBetter(string rarity)
    {
        return rarity is "\u7a00\u6709" or "\u53f2\u8a69";
    }
}

public sealed record TavernCharacterSummary(
    string Name,
    int Level,
    int CurrentHp,
    int MaxHp,
    int Experience,
    int ExperienceToNextLevel,
    int Gold,
    int EquipmentScore,
    int Attack);

public sealed record TavernEquippedSlotViewModel(
    EquipmentSlot Slot,
    string Label,
    TavernInventoryItemViewModel? Item);

public sealed record TavernInventoryItemViewModel(
    string Id,
    string DisplayName,
    EquipmentSlot Slot,
    string IconPath,
    string SlotLabel,
    string Rarity,
    int Power,
    int EffectivePower,
    string LevelRangeText,
    bool IsWithinRecommendedLevel,
    int SellPrice,
    bool IsLocked,
    bool IsEquipped,
    bool CanEquip,
    bool CanUnequip,
    bool CanSell,
    string SourceDungeonTypeId,
    IReadOnlyList<string> ModifierLines);

public enum EquipmentInventoryFilter
{
    All,
    Weapon,
    Armor,
    Accessory,
}

public enum EquipmentInventorySort
{
    Rarity,
    Power,
    Type,
    SellPrice,
}
