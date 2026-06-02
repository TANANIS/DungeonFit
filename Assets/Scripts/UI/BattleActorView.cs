using Godot;

namespace DungeonFit.UI;

public sealed class BattleActorView
{
    private const float BaseSpriteScale = 9.5f;

    private readonly PanelContainer _token;
    private readonly Label _label;
    private readonly AnimatedSprite2D _sprite;
    private readonly ProgressBar _hpBar;
    private readonly Label _hpLabel;
    private string _displayName;
    private float _displayScale = 1.0f;
    private float _anchorYOffset;
    private BattleActorState _state = BattleActorState.Idle;

    public Vector2 TokenSize => _token.Size;

    public BattleActorView(
        PanelContainer token,
        Label label,
        string displayName,
        BattleActorAnimationSet animationSet,
        bool flipHorizontal)
    {
        _token = token;
        _label = label;
        _displayName = displayName;
        _sprite = new AnimatedSprite2D
        {
            Name = "ActorSprite",
            SpriteFrames = SpriteSheetFramesBuilder.Build(animationSet),
            Animation = "idle",
            Centered = true,
            FlipH = flipHorizontal,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ZIndex = 1,
        };
        _hpBar = new ProgressBar
        {
            Name = "ActorHpBar",
            ShowPercentage = false,
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            Visible = false,
            ZIndex = 3,
            CustomMinimumSize = new Vector2(118, 12),
        };
        _hpLabel = new Label
        {
            Name = "ActorHpLabel",
            Visible = false,
            ZIndex = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = Colors.White,
        };

        _token.AddChild(_sprite);
        _token.AddChild(_hpBar);
        _token.AddChild(_hpLabel);
        _token.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _token.Resized += CenterSprite;
        _label.ZIndex = 2;
        _label.Visible = false;
        _label.Modulate = new Color(1, 1, 1, 0.68f);
        _label.VerticalAlignment = VerticalAlignment.Bottom;
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.AddThemeFontSizeOverride("font_size", 24);
        _hpLabel.AddThemeFontSizeOverride("font_size", 16);

        CenterSprite();
        SetState(BattleActorState.Idle);
    }

    public void SetDisplayName(string displayName)
    {
        _displayName = displayName;
        RefreshLabel(_state);
    }

    public void SetAnimationSet(BattleActorAnimationSet animationSet, float displayScale = 1.0f, float anchorYOffset = 0.0f)
    {
        _displayScale = Mathf.Max(0.1f, displayScale);
        _anchorYOffset = anchorYOffset;
        _sprite.SpriteFrames = SpriteSheetFramesBuilder.Build(animationSet);
        _sprite.Animation = GetAnimationName(_state);
        _sprite.Play();
        CenterSprite();
    }

    public void SetState(BattleActorState state)
    {
        _state = state;
        RefreshLabel(state);
        PlayAnimationForState(state);

        _token.Modulate = state switch
        {
            BattleActorState.Rest => new Color(0.82f, 0.82f, 0.92f, 1),
            BattleActorState.Evading => new Color(0.72f, 0.86f, 1.18f, 1),
            BattleActorState.Moving => new Color(0.9f, 1.02f, 0.9f, 1),
            BattleActorState.Hit => new Color(1.25f, 0.78f, 0.78f, 1),
            BattleActorState.Defeated => new Color(0.55f, 0.55f, 0.65f, 0.86f),
            BattleActorState.Victory => new Color(1.14f, 1.08f, 0.82f, 1),
            _ => Colors.White,
        };

        _sprite.Modulate = state == BattleActorState.Defeated
            ? new Color(0.82f, 0.82f, 0.88f, 1)
            : Colors.White;
        _sprite.Scale = GetSpriteScale(state);
        CenterSprite();
    }

    public void SetTokenPosition(Vector2 position)
    {
        _token.Position = position;
        CenterSprite();
    }

    public void ShowHp(int currentHp, int maxHp, bool isPlayer, bool isEvading)
    {
        var safeMax = Mathf.Max(1, maxHp);
        _hpBar.Visible = true;
        _hpLabel.Visible = true;
        _hpBar.MaxValue = safeMax;
        _hpBar.Value = Mathf.Clamp(currentHp, 0, safeMax);
        _hpLabel.Text = isPlayer && isEvading
            ? $"迴避中 HP {currentHp} / {safeMax}"
            : $"HP {currentHp} / {safeMax}";
        ApplyHpColor(isPlayer, isEvading);
        CenterSprite();
    }

    public void HideHp()
    {
        _hpBar.Visible = false;
        _hpLabel.Visible = false;
    }

    private void RefreshLabel(BattleActorState state)
    {
        _label.Text = _displayName;
    }

    private void PlayAnimationForState(BattleActorState state)
    {
        var animationName = GetAnimationName(state);

        if (_sprite.Animation != animationName)
        {
            _sprite.Play(animationName);
        }
        else if (!_sprite.IsPlaying())
        {
            _sprite.Play();
        }
    }

    private static string GetAnimationName(BattleActorState state)
    {
        return state switch
        {
            BattleActorState.Active => "attack",
            BattleActorState.Hit => "hurt",
            BattleActorState.Defeated => "death",
            _ => "idle",
        };
    }

    private Vector2 GetSpriteScale(BattleActorState state)
    {
        return Vector2.One * BaseSpriteScale * _displayScale;
    }

    private void CenterSprite()
    {
        _sprite.Position = new Vector2(_token.Size.X * 0.5f, _token.Size.Y * (0.58f + _anchorYOffset));
        var hpWidth = Mathf.Min(138, Mathf.Max(90, _token.Size.X * 0.72f));
        _hpBar.Size = new Vector2(hpWidth, 12);
        _hpBar.Position = new Vector2((_token.Size.X - hpWidth) * 0.5f, _token.Size.Y * 0.78f);
        _hpLabel.Size = new Vector2(Mathf.Max(150, hpWidth + 34), 24);
        _hpLabel.Position = new Vector2((_token.Size.X - _hpLabel.Size.X) * 0.5f, _token.Size.Y * 0.79f + 10);
    }

    private void ApplyHpColor(bool isPlayer, bool isEvading)
    {
        var fillColor = isEvading
            ? new Color(0.35f, 0.52f, 0.75f, 1)
            : isPlayer
                ? new Color(0.62f, 0.24f, 0.9f, 1)
                : new Color(0.88f, 0.18f, 0.22f, 1);
        var background = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.07f, 0.1f, 0.92f),
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
        };
        var fill = new StyleBoxFlat
        {
            BgColor = fillColor,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
        };
        _hpBar.AddThemeStyleboxOverride("background", background);
        _hpBar.AddThemeStyleboxOverride("fill", fill);
    }
}
