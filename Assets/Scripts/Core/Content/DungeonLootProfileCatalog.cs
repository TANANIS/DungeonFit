using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class DungeonLootProfileCatalog
{
    private static readonly DungeonChestLootRule NormalChest = new(
        Gold: 10,
        DropsEquipment: false,
        RarityTable: new RarityDropTable(100, 0, 0));

    private static readonly DungeonChestLootRule BossChest = new(
        Gold: 20,
        DropsEquipment: true,
        RarityTable: new RarityDropTable(52, 36, 12));

    private static readonly DungeonChestLootRule PartialBossChest = new(
        Gold: 15,
        DropsEquipment: true,
        RarityTable: new RarityDropTable(75, 23, 2),
        ExtraModifierPenalty: 1);

    private readonly DungeonLootProfile[] _profiles =
    {
        new(
            "chest",
            new[] { "chest_vanguard_blade", "chest_guard_plate", "chest_oath_ring" },
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 2, string.Empty),
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 3, "chest"),
            },
            NormalChest,
            BossChest,
            PartialBossChest),
        new(
            "shoulders",
            new[] { "shoulder_moon_halberd", "shoulder_guard_mantle", "shoulder_captain_medal" },
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 2, string.Empty),
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 2, string.Empty),
            },
            NormalChest,
            BossChest,
            PartialBossChest),
        new(
            "back",
            new[] { "back_shadow_axe", "back_watcher_coat", "back_raven_charm" },
            new[]
            {
                new EquipmentModifier(EquipmentStatType.Attack, 2, string.Empty),
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 3, "back"),
            },
            NormalChest,
            BossChest,
            PartialBossChest),
        new(
            "legs",
            new[] { "leg_miner_pick", "leg_deepstride_greaves", "leg_ore_badge" },
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 15, string.Empty),
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 3, "legs"),
            },
            NormalChest,
            BossChest,
            PartialBossChest),
        new(
            "core",
            new[] { "core_moon_staff", "core_stability_vest", "core_prayer_beads" },
            new[]
            {
                new EquipmentModifier(EquipmentStatType.MaxHp, 12, string.Empty),
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 2, string.Empty),
            },
            NormalChest,
            BossChest,
            PartialBossChest),
        new(
            "arms",
            new[] { "arm_courier_sabre", "arm_parcel_guard", "arm_signal_bracelet" },
            new[]
            {
                new EquipmentModifier(EquipmentStatType.DungeonGoldBonusPercent, 4, "arms"),
                new EquipmentModifier(EquipmentStatType.QuestRewardBonusPercent, 2, string.Empty),
            },
            NormalChest,
            BossChest,
            PartialBossChest),
    };

    public DungeonLootProfile GetByDungeonType(string dungeonTypeId)
    {
        return _profiles.FirstOrDefault(profile => profile.DungeonTypeId == dungeonTypeId) ?? _profiles[0];
    }

    public IReadOnlyList<DungeonLootProfile> GetAll()
    {
        return _profiles;
    }
}
