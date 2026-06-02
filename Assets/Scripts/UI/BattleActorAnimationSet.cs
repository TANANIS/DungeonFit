namespace DungeonFit.UI;

public sealed record BattleActorAnimationSet(
    string IdlePath,
    string AttackPath,
    string HurtPath,
    string DeathPath,
    string? BlockPath = null)
{
    public static BattleActorAnimationSet PlayerKnight { get; } = new(
        "res://Assets/Art/Actors/Player/Knight/Knight-Idle.png",
        "res://Assets/Art/Actors/Player/Knight/Knight-Attack01.png",
        "res://Assets/Art/Actors/Player/Knight/Knight-Hurt.png",
        "res://Assets/Art/Actors/Player/Knight/Knight-Death.png");

    public static BattleActorAnimationSet EnemySkeleton { get; } = new(
        "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Idle.png",
        "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Attack01.png",
        "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Hurt.png",
        "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Death.png");
}
