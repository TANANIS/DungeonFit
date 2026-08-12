using Godot;

namespace DungeonFit.UI;

public static class SpriteSheetFramesBuilder
{
    private const int FrameSize = 100;

    public static SpriteFrames Build(BattleActorAnimationSet animationSet)
    {
        var frames = new SpriteFrames();

        ReplaceAnimation(frames, "idle", animationSet.IdlePath, animationSet.IdleColumns, animationSet.IdleRows, loop: true, framesPerSecond: 6);
        ReplaceAnimation(frames, "attack", animationSet.AttackPath, animationSet.AttackColumns, animationSet.AttackRows, loop: false, framesPerSecond: 12);
        ReplaceAnimation(frames, "hurt", animationSet.HurtPath, animationSet.HurtColumns, animationSet.HurtRows, loop: false, framesPerSecond: 8);
        ReplaceAnimation(frames, "death", animationSet.DeathPath, animationSet.DeathColumns, animationSet.DeathRows, loop: false, framesPerSecond: 7);
        ReplaceAnimation(frames, "block", animationSet.BlockPath ?? animationSet.HurtPath, animationSet.HurtColumns, animationSet.HurtRows, loop: false, framesPerSecond: 8);

        return frames;
    }

    private static void ReplaceAnimation(
        SpriteFrames frames,
        string animationName,
        string texturePath,
        int configuredColumns,
        int rows,
        bool loop,
        double framesPerSecond)
    {
        if (frames.HasAnimation(animationName))
        {
            frames.Clear(animationName);
        }
        else
        {
            frames.AddAnimation(animationName);
        }

        frames.SetAnimationLoop(animationName, loop);
        frames.SetAnimationSpeed(animationName, framesPerSecond);

        var texture = GD.Load<Texture2D>(texturePath);
        var columns = configuredColumns > 0
            ? configuredColumns
            : Mathf.Max(1, texture.GetWidth() / FrameSize);
        var frameRows = Mathf.Max(1, rows);

        for (var row = 0; row < frameRows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var frameTexture = new AtlasTexture
                {
                    Atlas = texture,
                    Region = new Rect2(column * FrameSize, row * FrameSize, FrameSize, FrameSize),
                };
                frames.AddFrame(animationName, frameTexture);
            }
        }
    }
}
