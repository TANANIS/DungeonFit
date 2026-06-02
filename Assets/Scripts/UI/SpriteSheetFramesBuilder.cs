using Godot;

namespace DungeonFit.UI;

public static class SpriteSheetFramesBuilder
{
    private const int FrameSize = 100;

    public static SpriteFrames Build(BattleActorAnimationSet animationSet)
    {
        var frames = new SpriteFrames();

        ReplaceAnimation(frames, "idle", animationSet.IdlePath, loop: true, framesPerSecond: 6);
        ReplaceAnimation(frames, "attack", animationSet.AttackPath, loop: false, framesPerSecond: 12);
        ReplaceAnimation(frames, "hurt", animationSet.HurtPath, loop: false, framesPerSecond: 8);
        ReplaceAnimation(frames, "death", animationSet.DeathPath, loop: false, framesPerSecond: 7);
        ReplaceAnimation(frames, "block", animationSet.BlockPath ?? animationSet.HurtPath, loop: false, framesPerSecond: 8);

        return frames;
    }

    private static void ReplaceAnimation(
        SpriteFrames frames,
        string animationName,
        string texturePath,
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
        var frameCount = Mathf.Max(1, texture.GetWidth() / FrameSize);

        for (var index = 0; index < frameCount; index++)
        {
            var frameTexture = new AtlasTexture
            {
                Atlas = texture,
                Region = new Rect2(index * FrameSize, 0, FrameSize, FrameSize),
            };
            frames.AddFrame(animationName, frameTexture);
        }
    }
}
