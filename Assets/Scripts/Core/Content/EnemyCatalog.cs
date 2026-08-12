using System.Collections.Generic;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class EnemyCatalog
{
    private readonly Dictionary<string, EnemyDefinition> _enemies = new()
    {
        ["chest"] = new("moon_guard", "\u6708\u5149\u885b\u5175", "\u6708\u5149\u91cd\u76fe\u968a\u9577", 30, 2, 60, 4, ActorVisualIds.SkeletonArmored, ActorVisualIds.MoonGuardBoss, ActorVisualIds.AxemanArmored),
        ["shoulders"] = new("banner_knight", "\u65d7\u5e5f\u9a0e\u58eb", "\u96d9\u80a9\u8a66\u7149\u5b98", 32, 2, 64, 4, ActorVisualIds.SkeletonArcher, ActorVisualIds.OrcRiderBoss, ActorVisualIds.OrcArmored),
        ["back"] = new("chain_watcher", "\u9396\u934a\u770b\u5b88", "\u80cc\u810a\u5de8\u50cf", 36, 3, 72, 5, ActorVisualIds.SkeletonBasic, ActorVisualIds.WerewolfBoss, ActorVisualIds.SkeletonGreatsword),
        ["legs"] = new("stone_runner", "\u77f3\u5eca\u5954\u8005", "\u6c89\u8db3\u5b88\u9580\u8005", 38, 3, 76, 5, ActorVisualIds.OrcBasic, ActorVisualIds.OrcRiderBoss, ActorVisualIds.OrcElite),
        ["core"] = new("crystal_wisp", "\u6676\u6838\u6d6e\u9748", "\u6838\u5fc3\u6cd5\u9663\u4e3b", 40, 3, 80, 5, ActorVisualIds.SlimeBasic, ActorVisualIds.WerebearBoss, ActorVisualIds.SkeletonArcher),
        ["arms"] = new("iron_squire", "\u9435\u81c2\u5f9e\u8005", "\u96d9\u5203\u6559\u982d", 34, 3, 68, 5, ActorVisualIds.OrcArmored, ActorVisualIds.WerewolfBoss, ActorVisualIds.AxemanArmored),
    };

    public EnemyDefinition GetForDungeon(string dungeonTypeId)
    {
        return _enemies.TryGetValue(dungeonTypeId, out var enemy)
            ? enemy
            : new EnemyDefinition("training_dummy", "\u8a13\u7df4\u5047\u4eba", "\u8a13\u7df4\u6559\u5b98", 30, 2, 60, 4);
    }
}
