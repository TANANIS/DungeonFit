using DungeonFit.Core.Models;

namespace DungeonFit.Core.Rules;

public sealed class RoomRewardResolver
{
    private readonly LootRoller _lootRoller = new();

    public RewardBundle Resolve(RoomRun room)
    {
        return _lootRoller.RollStagePreview(room);
    }
}
