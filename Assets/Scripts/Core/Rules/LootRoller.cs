using DungeonFit.Core.Models;

namespace DungeonFit.Core.Rules;

public sealed class LootRoller
{
    private readonly LootTable _lootTable = new();

    public RewardBundle RollStagePreview(RoomRun room)
    {
        return _lootTable.RollStagePreview(room);
    }

    public RewardBundle RollDungeonChest(DungeonChest chest)
    {
        return _lootTable.RollDungeonChest(chest);
    }
}
