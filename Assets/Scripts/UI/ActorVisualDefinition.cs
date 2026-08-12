namespace DungeonFit.UI;

public sealed record ActorVisualDefinition(
    string Id,
    float DisplayScale,
    float AnchorYOffset,
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
    public BattleActorAnimationSet ToAnimationSet()
    {
        return new BattleActorAnimationSet(
            IdlePath,
            AttackPath,
            HurtPath,
            DeathPath,
            BlockPath,
            IdleColumns,
            IdleRows,
            AttackColumns,
            AttackRows,
            HurtColumns,
            HurtRows,
            DeathColumns,
            DeathRows,
            FlipHorizontal);
    }
}
