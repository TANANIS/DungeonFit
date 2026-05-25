using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;

namespace DungeonFit.Gameplay;

public sealed class RoomRunService
{
    private readonly RoomRewardResolver _rewardResolver = new();

    public RoomRun Start(TaskTemplate task, PlayerCombatStats playerStats, EnemyDefinition enemy, int initialPlayerHp)
    {
        return new RoomRun(task, playerStats, enemy, initialPlayerHp);
    }

    public CombatSetResult? ReportSet(RoomRun room)
    {
        return room.ReportSet();
    }

    public ActiveSetCombatState BeginActiveSet(RoomRun room)
    {
        return room.BeginActiveSet();
    }

    public CombatRepResult? ResolveRepHit(RoomRun room)
    {
        return room.ResolveRepHit();
    }

    public RoomProgress Skip(RoomRun room)
    {
        room.Skip();
        return room.Progress;
    }

    public RewardBundle ResolveReward(RoomRun room)
    {
        return _rewardResolver.Resolve(room);
    }
}
