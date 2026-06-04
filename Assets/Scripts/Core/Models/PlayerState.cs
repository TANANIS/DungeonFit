using System.Collections.Generic;
using System.Linq;
using System;

namespace DungeonFit.Core.Models;

public sealed class PlayerState
{
    private const int DefaultLevel = 1;
    private const int DefaultExperience = 120;
    private const int DefaultExperienceToNextLevel = 180;
    private const int DefaultBaseAttack = 4;
    private const int DefaultBaseMaxHp = 30;

    public int Level { get; private set; } = DefaultLevel;

    public int Experience { get; private set; } = DefaultExperience;

    public int ExperienceToNextLevel { get; private set; } = DefaultExperienceToNextLevel;

    public int Gold { get; private set; }

    public int CurrentHp { get; private set; } = DefaultBaseMaxHp;

    public string DailyBlessingId { get; private set; } = DailyBlessing.None;

    public List<EquipmentItem> Inventory { get; } = new();

    public EquipmentLoadout Loadout { get; } = new();

    public int EquipmentScore => GetEquippedItems().Sum(item => item.GetEffectivePower(Level));

    public int BaseAttack => DefaultBaseAttack + ((Level - 1) / 2);

    public int BaseMaxHp => DefaultBaseMaxHp + ((Level - 1) * 3);

    public int Attack
    {
        get
        {
            var attack = BaseAttack + SumEquippedStat(EquipmentStatType.Attack);
            return DailyBlessingId == DailyBlessing.BladeMoon
                ? (int)Math.Ceiling(attack * 1.05)
                : attack;
        }
    }

    public int MaxHp
    {
        get
        {
            var maxHp = BaseMaxHp + SumEquippedStat(EquipmentStatType.MaxHp);
            return DailyBlessingId == DailyBlessing.MoonGuard
                ? (int)Math.Ceiling(maxHp * 1.1)
                : maxHp;
        }
    }

    public int DungeonGoldBonusPercent => SumEquippedStat(EquipmentStatType.DungeonGoldBonusPercent) +
        (DailyBlessingId == DailyBlessing.StarlightGold ? 10 : 0);

    public PlayerCombatStats CombatStats => new(MaxHp, Attack, EquipmentScore, DungeonGoldBonusPercent);

    public void Load(
        int gold,
        IEnumerable<EquipmentItem>? inventory,
        EquipmentLoadout? loadout = null,
        int level = DefaultLevel,
        int experience = DefaultExperience,
        int experienceToNextLevel = DefaultExperienceToNextLevel,
        int? currentHp = null,
        string? dailyBlessingId = null)
    {
        Level = level <= 0 ? DefaultLevel : level;
        Experience = experience < 0 ? 0 : experience;
        ExperienceToNextLevel = experienceToNextLevel <= 0 ? GetExperienceToNextLevel(Level) : experienceToNextLevel;
        Gold = Math.Max(0, gold);
        DailyBlessingId = DailyBlessing.IsValid(dailyBlessingId) ? dailyBlessingId! : DailyBlessing.None;
        Inventory.Clear();

        if (inventory is not null)
        {
            Inventory.AddRange(inventory);
        }

        Loadout.WeaponId = loadout?.WeaponId;
        Loadout.ArmorId = loadout?.ArmorId;
        Loadout.AccessoryId = loadout?.AccessoryId;
        RemoveMissingEquippedItems();
        CurrentHp = currentHp.HasValue ? ClampHp(currentHp.Value) : MaxHp;
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
            ClampCurrentHp();
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
        ClampCurrentHp();
        return true;
    }

    public bool Unequip(EquipmentSlot slot)
    {
        if (Loadout.GetEquippedId(slot) is null)
        {
            return false;
        }

        Loadout.Unequip(slot);
        ClampCurrentHp();
        return true;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount)
        {
            return false;
        }

        Gold -= amount;
        return true;
    }

    public void SetCurrentHp(int hp)
    {
        CurrentHp = ClampHp(hp);
    }

    public int HealPercent(double percent)
    {
        if (percent <= 0 || CurrentHp >= MaxHp)
        {
            return 0;
        }

        return Heal((int)Math.Ceiling(MaxHp * percent));
    }

    public int HealToFull()
    {
        if (CurrentHp >= MaxHp)
        {
            return 0;
        }

        var before = CurrentHp;
        CurrentHp = MaxHp;
        return CurrentHp - before;
    }

    public bool SetDailyBlessing(string blessingId)
    {
        if (!DailyBlessing.IsValid(blessingId))
        {
            return false;
        }

        if (DailyBlessingId == blessingId)
        {
            return true;
        }

        if (DailyBlessingId != DailyBlessing.None)
        {
            return false;
        }

        DailyBlessingId = blessingId;
        ClampCurrentHp();
        return true;
    }

    public void ClearDailyBlessing()
    {
        DailyBlessingId = DailyBlessing.None;
        ClampCurrentHp();
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

    public bool EnhanceEquipment(string itemId, int cost, int maxEnhancementLevel)
    {
        var item = Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null ||
            item.EnhancementLevel >= maxEnhancementLevel ||
            !SpendGold(cost))
        {
            return false;
        }

        item.EnhancementLevel++;
        item.Power++;
        return true;
    }

    public bool DismantleEnhancement(string itemId, int refundGold)
    {
        var item = Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null || item.EnhancementLevel <= 0)
        {
            return false;
        }

        item.Power = Math.Max(0, item.Power - item.EnhancementLevel);
        item.EnhancementLevel = 0;
        Gold += Math.Max(0, refundGold);
        return true;
    }

    public bool ExtendEquipmentLevelRange(string itemId, int cost, int maxExtension)
    {
        var item = Inventory.FirstOrDefault(equipment => equipment.Id == itemId);
        if (item is null ||
            item.LevelExtension >= maxExtension ||
            !SpendGold(cost))
        {
            return false;
        }

        item.LevelExtension++;
        ClampCurrentHp();
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
            .SelectMany(item => item.Modifiers.Select(modifier => new
            {
                Modifier = modifier,
                Value = item.GetEffectiveModifierValue(modifier, Level),
            }))
            .Where(entry => entry.Modifier.StatType == statType)
            .Sum(entry => entry.Value);
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

    private int Heal(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var before = CurrentHp;
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        return CurrentHp - before;
    }

    private void ClampCurrentHp()
    {
        CurrentHp = ClampHp(CurrentHp);
    }

    private int ClampHp(int hp)
    {
        return Math.Clamp(hp, 0, MaxHp);
    }

    public static int GetExperienceToNextLevel(int level)
    {
        var safeLevel = level <= 0 ? DefaultLevel : level;
        return DefaultExperienceToNextLevel + ((safeLevel - 1) * 55);
    }
}
