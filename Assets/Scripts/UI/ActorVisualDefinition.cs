namespace DungeonFit.UI;

public sealed record ActorVisualDefinition(
    string Id,
    float DisplayScale,
    float AnchorYOffset,
    string IdlePath,
    string AttackPath,
    string HurtPath,
    string DeathPath,
    string? BlockPath = null)
{
    public BattleActorAnimationSet ToAnimationSet()
    {
        return new BattleActorAnimationSet(
            IdlePath,
            AttackPath,
            HurtPath,
            DeathPath,
            BlockPath);
    }
}
