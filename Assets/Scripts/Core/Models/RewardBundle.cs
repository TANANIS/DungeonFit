namespace DungeonFit.Core.Models;

public sealed record RewardBundle(
    RewardSource Source,
    int Gold,
    EquipmentItem? Equipment);
