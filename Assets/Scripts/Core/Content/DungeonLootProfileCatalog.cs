using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class DungeonLootProfileCatalog
{
    private static readonly EquipmentCatalog Equipment = new();

    private static readonly DungeonChestLootRule NormalChest = new(
        Gold: 10,
        DropsEquipment: true,
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
            Equipment.GetDefinitionIdsForDungeon("chest"),
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
            Equipment.GetDefinitionIdsForDungeon("shoulders"),
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
            Equipment.GetDefinitionIdsForDungeon("back"),
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
            Equipment.GetDefinitionIdsForDungeon("legs"),
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
            Equipment.GetDefinitionIdsForDungeon("core"),
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
            Equipment.GetDefinitionIdsForDungeon("arms"),
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
