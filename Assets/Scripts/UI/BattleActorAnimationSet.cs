namespace DungeonFit.UI;

public sealed record BattleActorAnimationSet(
    string IdlePath,
    string AttackPath,
    string HurtPath,
    string DeathPath,
    string? BlockPath = null,
    int IdleColumns = 0,
    int IdleRows = 1,
    int AttackColumns = 0,
    int AttackRows = 1,
    int HurtColumns = 0,
    int HurtRows = 1,
    int DeathColumns = 0,
    int DeathRows = 1,
    bool FlipHorizontal = true)
{
    public static BattleActorAnimationSet PlayerKnight { get; } = new(
        "res://Assets/Art/Actors/Player/Knight/Knight-Idle.png",
        "res://Assets/Art/Actors/Player/Knight/Knight-Attack01.png",
        "res://Assets/Art/Actors/Player/Knight/Knight-Hurt.png",
        "res://Assets/Art/Actors/Player/Knight/Knight-Death.png",
        FlipHorizontal: false);

    public static BattleActorAnimationSet EnemySkeleton { get; } = new(
        "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Idle.png",
        "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Attack01.png",
        "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Hurt.png",
        "res://Assets/Art/Actors/Enemies/Skeleton/Skeleton-Death.png");
}
