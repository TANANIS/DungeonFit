using System.Collections.Generic;
using DungeonFit.Core.Models;

namespace DungeonFit.UI;

public sealed class ActorVisualCatalog
{
    private readonly Dictionary<string, ActorVisualDefinition> _visuals = new()
    {
        [ActorVisualIds.EnemySkeleton] = new(
            ActorVisualIds.EnemySkeleton,
            1.0f,
            0.0f,
            "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Idle.png",
            "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Attack01.png",
            "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Hurt.png",
            "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Death.png",
            "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Block.png"),
        [ActorVisualIds.SlimeBasic] = Build(ActorVisualIds.SlimeBasic, 0.92f, 0.05f),
        [ActorVisualIds.SkeletonBasic] = Build(ActorVisualIds.SkeletonBasic, 1.0f, 0.0f, hasBlock: true),
        [ActorVisualIds.SkeletonArcher] = Build(ActorVisualIds.SkeletonArcher, 1.0f, 0.0f),
        [ActorVisualIds.SkeletonArmored] = Build(ActorVisualIds.SkeletonArmored, 1.08f, 0.0f),
        [ActorVisualIds.SkeletonGreatsword] = Build(ActorVisualIds.SkeletonGreatsword, 1.08f, 0.0f),
        [ActorVisualIds.OrcBasic] = Build(ActorVisualIds.OrcBasic, 1.02f, 0.0f),
        [ActorVisualIds.OrcArmored] = Build(ActorVisualIds.OrcArmored, 1.08f, 0.0f, hasBlock: true),
        [ActorVisualIds.OrcElite] = Build(ActorVisualIds.OrcElite, 1.12f, 0.0f),
        [ActorVisualIds.OrcRiderBoss] = Build(ActorVisualIds.OrcRiderBoss, 1.18f, -0.01f, hasBlock: true),
        [ActorVisualIds.AxemanArmored] = Build(ActorVisualIds.AxemanArmored, 1.12f, 0.0f),
        [ActorVisualIds.WerewolfBoss] = Build(ActorVisualIds.WerewolfBoss, 1.18f, -0.01f),
        [ActorVisualIds.WerebearBoss] = Build(ActorVisualIds.WerebearBoss, 1.23f, -0.02f),
        [ActorVisualIds.MoonGuardBoss] = new(
            ActorVisualIds.MoonGuardBoss,
            0.82f,
            -0.02f,
            "res://Assets/Art/Generated/RoomChallenge/MoonGuardBoss/processed/idle/sheet-transparent.png",
            "res://Assets/Art/Generated/RoomChallenge/MoonGuardBoss/processed/attack/sheet-transparent.png",
            "res://Assets/Art/Generated/RoomChallenge/MoonGuardBoss/processed/hurt/sheet-transparent.png",
            "res://Assets/Art/Generated/RoomChallenge/MoonGuardBoss/processed/death/sheet-transparent.png",
            null,
            2,
            2,
            1,
            1,
            2,
            2,
            2,
            2,
            false),
    };

    public ActorVisualDefinition Get(string? visualId)
    {
        return !string.IsNullOrWhiteSpace(visualId) && _visuals.TryGetValue(visualId, out var visual)
            ? visual
            : _visuals[ActorVisualIds.EnemySkeleton];
    }

    private static ActorVisualDefinition Build(
        string id,
        float displayScale,
        float anchorYOffset,
        bool hasBlock = false)
    {
        var basePath = $"res://Assets/Art/Actors/Enemies/{id}";
        return new ActorVisualDefinition(
            id,
            displayScale,
            anchorYOffset,
            $"{basePath}/idle.png",
            $"{basePath}/attack_01.png",
            $"{basePath}/hurt.png",
            $"{basePath}/death.png",
            hasBlock ? $"{basePath}/block.png" : null);
    }
}
