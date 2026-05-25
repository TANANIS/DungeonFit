using System.Collections.Generic;

namespace DungeonFit.Core.Models;

public sealed record DungeonLootProfile(
    string DungeonTypeId,
    IReadOnlyList<string> EquipmentDefinitionIds,
    IReadOnlyList<EquipmentModifier> ExtraModifierCandidates,
    DungeonChestLootRule NormalChest,
    DungeonChestLootRule BossChest,
    DungeonChestLootRule PartialBossChest);

public sealed record DungeonChestLootRule(
    int Gold,
    bool DropsEquipment,
    RarityDropTable RarityTable,
    int ExtraModifierPenalty = 0);

public sealed record RarityDropTable(
    int CommonWeight,
    int RareWeight,
    int EpicWeight)
{
    public int TotalWeight => CommonWeight + RareWeight + EpicWeight;
}
