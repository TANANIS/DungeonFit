using System.Collections.Generic;
using System.Linq;

namespace DungeonFit.Core.Models;

public sealed class PlayerState
{
    private const int DefaultLevel = 1;
    private const int DefaultExperience = 120;
    private const int DefaultExperienceToNextLevel = 300;
    private const int DefaultBaseAttack = 3;
    private const int DefaultBaseMaxHp = 24;

    public int Level { get; private set; } = DefaultLevel;

    public int Experience { get; private set; } = DefaultExperience;

    public int ExperienceToNextLevel { get; private set; } = DefaultExperienceToNextLevel;

    public int Gold { get; private set; }

    public List<EquipmentItem> Inventory { get; } = new();

    public EquipmentLoadout Loadout { get; } = new();

    public int EquipmentScore => GetEquippedItems().Sum(item => item.Power);

    public int BaseAttack => DefaultBaseAttack + ((Level - 1) / 2);

    public int BaseMaxHp => DefaultBaseMaxHp + ((Level - 1) * 2);

    public int Attack => BaseAttack + SumEquippedStat(EquipmentStatType.Attack);

    public int MaxHp => BaseMaxHp + SumEquippedStat(EquipmentStatType.MaxHp);

    public int CurrentHp => MaxHp;

    public PlayerCombatStats CombatStats => new(MaxHp, Attack, EquipmentScore);

    public void Load(
        int gold,
        IEnumerable<EquipmentItem>? inventory,
        EquipmentLoadout? loadout = null,
        int level = DefaultLevel,
        int experience = DefaultExperience,
        int experienceToNextLevel = DefaultExperienceToNextLevel)
    {
        Level = level <= 0 ? DefaultLevel : level;
        Experience = experience < 0 ? 0 : experience;
        ExperienceToNextLevel = experienceToNextLevel <= 0 ? GetExperienceToNextLevel(Level) : experienceToNextLevel;
        Gold = gold;
        Inventory.Clear();

        if (inventory is not null)
        {
            Inventory.AddRange(inventory);
        }

        Loadout.WeaponId = loadout?.WeaponId;
        Loadout.ArmorId = loadout?.ArmorId;
        Loadout.AccessoryId = loadout?.AccessoryId;
        RemoveMissingEquippedItems();
    }

    public void Apply(RewardBundle reward)
    {
        Gold += reward.Gold;

        if (reward.Equipment is not null)
        {
            Inventory.Add(reward.Equipment);
        }
    }

    public int AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var levelsGained = 0;
        Experience += amount;
        while (Experience >= ExperienceToNextLevel)
        {
            Experience -= ExperienceToNextLevel;
            Level++;
            levelsGained++;
            ExperienceToNextLevel = GetExperienceToNextLevel(Level);
        }

        return levelsGained;
    }

    public bool Equip(string itemId)
    {
        var item = Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null)
        {
            return false;
        }

        Loadout.Equip(item);
        return true;
    }

    public bool Unequip(EquipmentSlot slot)
    {
        if (Loadout.GetEquippedId(slot) is null)
        {
            return false;
        }

        Loadout.Unequip(slot);
        return true;
    }

    public bool SetEquipmentLocked(string itemId, bool isLocked)
    {
        var item = Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null)
        {
            return false;
        }

        item.IsLocked = isLocked;
        return true;
    }

    public bool SellEquipment(string itemId)
    {
        var item = Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null || item.IsLocked || Loadout.IsEquipped(item.Id))
        {
            return false;
        }

        Gold += item.SellPrice;
        Inventory.Remove(item);
        return true;
    }

    public int SellUnlockedEquipment(IEnumerable<string> itemIds)
    {
        var soldCount = 0;
        foreach (var itemId in itemIds.ToArray())
        {
            if (SellEquipment(itemId))
            {
                soldCount++;
            }
        }

        return soldCount;
    }

    public IReadOnlyList<EquipmentItem> GetEquippedItems()
    {
        return Inventory
            .Where(item => Loadout.IsEquipped(item.Id))
            .ToArray();
    }

    public bool IsEquipped(string itemId)
    {
        return Loadout.IsEquipped(itemId);
    }

    private int SumEquippedStat(EquipmentStatType statType)
    {
        return GetEquippedItems()
            .SelectMany(item => item.Modifiers)
            .Where(modifier => modifier.StatType == statType)
            .Sum(modifier => modifier.Value);
    }

    private void RemoveMissingEquippedItems()
    {
        if (Loadout.WeaponId is not null && Inventory.All(item => item.Id != Loadout.WeaponId))
        {
            Loadout.WeaponId = null;
        }

        if (Loadout.ArmorId is not null && Inventory.All(item => item.Id != Loadout.ArmorId))
        {
            Loadout.ArmorId = null;
        }

        if (Loadout.AccessoryId is not null && Inventory.All(item => item.Id != Loadout.AccessoryId))
        {
            Loadout.AccessoryId = null;
        }
    }

    public static int GetExperienceToNextLevel(int level)
    {
        var safeLevel = level <= 0 ? DefaultLevel : level;
        return DefaultExperienceToNextLevel + ((safeLevel - 1) * 80);
    }
}
