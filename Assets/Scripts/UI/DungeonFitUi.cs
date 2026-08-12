using Godot;

namespace DungeonFit.UI;

public enum UiPanelStyle
{
    Main,
    Card,
    Overlay,
    Battle,
    Token,
}

public enum UiButtonStyle
{
    Primary,
    Secondary,
    Danger,
}

public static class DungeonFitUi
{
    private const int PanelTextureMargin = 32;
    private const int ButtonTextureMargin = 28;

    public static void ApplyTheme(Control root)
    {
        if (ResourceLoader.Exists(UiThemePaths.Theme))
        {
            root.Theme = GD.Load<Theme>(UiThemePaths.Theme);
        }
    }

    public static TextureRect AddBackground(Control root, string texturePath)
    {
        var background = CreateTextureRect(texturePath, "ArtBackground", TextureRect.StretchModeEnum.KeepAspectCovered);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        background.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(background);
        root.MoveChild(background, Mathf.Min(root.GetChildCount() - 1, 1));
        return background;
    }

    public static TextureRect AddDungeonPortalBackground(Control root, string texturePath)
    {
        var background = AddBackground(root, texturePath);
        background.OffsetTop = -70;
        background.OffsetBottom = -70;
        return background;
    }

    public static TextureRect AddMapBackground(Control root, string texturePath)
    {
        var background = CreateTextureRect(texturePath, "MapArtBackground", TextureRect.StretchModeEnum.KeepAspectCovered);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        background.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(background);
        root.MoveChild(background, 0);
        return background;
    }

    public static TextureRect AddPanelBackground(PanelContainer panel, string texturePath)
    {
        var background = CreateTextureRect(texturePath, "PanelArtBackground", TextureRect.StretchModeEnum.KeepAspectCovered);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        background.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.AddChild(background);
        panel.MoveChild(background, 0);
        return background;
    }

    public static TextureRect CreateIcon(string texturePath, int size, string name = "Icon")
    {
        var icon = CreateTextureRect(texturePath, name, TextureRect.StretchModeEnum.KeepAspectCentered);
        icon.CustomMinimumSize = new Vector2(size, size);
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        return icon;
    }

    public static Texture2D CreateAtlasTexture(string texturePath, Rect2 region)
    {
        return new AtlasTexture
        {
            Atlas = LoadTexture(texturePath),
            Region = region,
        };
    }

    public static TextureRect DecorateExistingSpritePanel(
        PanelContainer panel,
        string texturePath,
        Rect2 region,
        int spriteSize)
    {
        ApplyPanel(panel, UiPanelStyle.Token);
        foreach (var child in panel.GetChildren())
        {
            panel.RemoveChild(child);
            child.QueueFree();
        }

        var center = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(center);

        var sprite = new TextureRect
        {
            Name = "Sprite",
            Texture = CreateAtlasTexture(texturePath, region),
            CustomMinimumSize = new Vector2(spriteSize, spriteSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        center.AddChild(sprite);
        return sprite;
    }

    public static void ApplyPanel(PanelContainer panel, UiPanelStyle style = UiPanelStyle.Main)
    {
        panel.AddThemeStyleboxOverride("panel", CreateTexturePanelStyle(GetPanelTexturePath(style), PanelTextureMargin) ?? CreatePanelStyle(style));
    }

    public static void ApplyExplorationPanel(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.035f, 0.025f, 0.09f, 0.56f),
            BorderColor = new Color(0.68f, 0.34f, 0.9f, 0.98f),
            BorderWidthLeft = 4,
            BorderWidthTop = 4,
            BorderWidthRight = 4,
            BorderWidthBottom = 4,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10,
            ContentMarginLeft = 12,
            ContentMarginTop = 10,
            ContentMarginRight = 12,
            ContentMarginBottom = 10,
        });
    }

    public static void ApplyPortraitFrame(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.025f, 0.16f, 0.96f),
            BorderColor = new Color(0.9f, 0.7f, 0.3f, 1f),
            BorderWidthLeft = 4,
            BorderWidthTop = 4,
            BorderWidthRight = 4,
            BorderWidthBottom = 4,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ShadowColor = new Color(0.43f, 0.12f, 0.72f, 0.72f),
            ShadowSize = 6,
            ContentMarginLeft = 4,
            ContentMarginTop = 4,
            ContentMarginRight = 4,
            ContentMarginBottom = 4,
        });
    }

    public static void ApplyExplorationActorPanel(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }

    public static void ApplyDungeonPlanHeader(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }

    public static void ApplyDungeonPlanSurface(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", CreateDungeonPlanPanelStyle(0.16f, 4));
    }

    public static void ApplyDungeonRouteRow(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", CreateDungeonPlanPanelStyle(0.7f, 2));
    }

    public static void ApplyDungeonEmblemButton(Button button, bool selected)
    {
        button.AddThemeStyleboxOverride("normal", CreateDungeonEmblemButtonStyle(selected, 1f));
        button.AddThemeStyleboxOverride("hover", CreateDungeonEmblemButtonStyle(selected, 1.14f));
        button.AddThemeStyleboxOverride("pressed", CreateDungeonEmblemButtonStyle(selected, 0.9f));
        button.AddThemeStyleboxOverride("disabled", CreateDungeonEmblemButtonStyle(false, 0.42f));
        button.AddThemeColorOverride("font_color", new Color(1f, 0.83f, 0.45f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.93f, 0.72f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.5f, 0.6f));
    }

    public static void ApplyArenaPanel(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.025f, 0.027f, 0.08f, 0.38f),
            BorderColor = new Color(0.62f, 0.52f, 0.31f, 0.95f),
            BorderWidthLeft = 4,
            BorderWidthTop = 4,
            BorderWidthRight = 4,
            BorderWidthBottom = 4,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomRight = 18,
            CornerRadiusBottomLeft = 18,
        });
    }

    public static void ApplyButton(Button button, UiButtonStyle style = UiButtonStyle.Secondary)
    {
        button.AddThemeStyleboxOverride("normal", CreateTextureButtonStyle(GetButtonTexturePath(style), ButtonTextureMargin, 1.0f) ?? CreateButtonStyle(style, false, 1.0f));
        button.AddThemeStyleboxOverride("hover", CreateTextureButtonStyle(GetButtonTexturePath(style), ButtonTextureMargin, 1.12f) ?? CreateButtonStyle(style, false, 1.12f));
        button.AddThemeStyleboxOverride("pressed", CreateTextureButtonStyle(GetButtonTexturePath(style), ButtonTextureMargin, 0.9f) ?? CreateButtonStyle(style, true, 0.9f));
        button.AddThemeStyleboxOverride("disabled", CreateTextureButtonStyle(GetButtonTexturePath(style), ButtonTextureMargin, 0.45f) ?? CreateButtonStyle(style, false, 0.45f));
        button.AddThemeColorOverride("font_color", new Color(0.96f, 0.92f, 0.78f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.96f, 0.84f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.9f, 0.84f, 0.68f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.52f, 0.5f, 0.55f));
    }

    public static Button CreateIconTextButton(
        string texturePath,
        string labelText,
        int iconSize,
        UiButtonStyle style,
        int fontSize = 24,
        bool vertical = true)
    {
        var button = new Button
        {
            Text = string.Empty,
        };
        ApplyButton(button, style);
        ApplyIconTextContent(button, texturePath, labelText, iconSize, fontSize, vertical);
        return button;
    }

    public static void ApplyProgressBar(ProgressBar progressBar, Color fill)
    {
        var background = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.16f, 0.95f),
            BorderColor = new Color(0.55f, 0.47f, 0.28f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusBottomLeft = 4,
        };
        var fillStyle = new StyleBoxFlat
        {
            BgColor = fill,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusBottomLeft = 3,
        };
        progressBar.AddThemeStyleboxOverride("background", background);
        progressBar.AddThemeStyleboxOverride("fill", fillStyle);
    }

    public static void DecorateExistingIconPanel(PanelContainer panel, string texturePath, int iconSize)
    {
        ApplyPanel(panel, UiPanelStyle.Token);
        var label = panel.GetChildCount() > 0 ? panel.GetChild(0) as Control : null;
        if (label is not null)
        {
            panel.RemoveChild(label);
        }

        var layout = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        layout.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(layout);
        layout.AddChild(CreateIcon(texturePath, iconSize));

        if (label is not null)
        {
            label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            layout.AddChild(label);
        }
    }

    public static void DecorateExistingFacilityPanel(
        PanelContainer panel,
        string texturePath,
        string titleText,
        string subtitleText)
    {
        panel.AddThemeStyleboxOverride("panel", CreateFacilityPanelStyle());
        foreach (var child in panel.GetChildren())
        {
            panel.RemoveChild(child);
            child.QueueFree();
        }

        var center = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(center);

        var layout = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        layout.AddThemeConstantOverride("separation", 2);
        center.AddChild(layout);
        layout.AddChild(CreateIcon(texturePath, 50));

        var title = new Label
        {
            Text = titleText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.42f));
        layout.AddChild(title);

        var subtitle = new Label
        {
            Text = subtitleText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        subtitle.AddThemeFontSizeOverride("font_size", 15);
        subtitle.AddThemeColorOverride("font_color", new Color(0.93f, 0.84f, 1f));
        layout.AddChild(subtitle);
    }

    public static void ApplyIconTextContent(
        Button button,
        string texturePath,
        string labelText,
        int iconSize,
        int fontSize,
        bool vertical = true)
    {
        button.Text = string.Empty;
        foreach (var child in button.GetChildren())
        {
            if (child is Node node && node.Name == "IconTextContent")
            {
                button.RemoveChild(node);
                node.QueueFree();
            }
        }

        var center = new CenterContainer
        {
            Name = "IconTextContent",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        BoxContainer layout = vertical
            ? new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }
            : new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        layout.AddThemeConstantOverride("separation", vertical ? 2 : 8);
        layout.MouseFilter = Control.MouseFilterEnum.Ignore;

        layout.AddChild(CreateIcon(texturePath, iconSize));
        var label = new Label
        {
            Text = labelText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color(0.96f, 0.92f, 0.78f));
        layout.AddChild(label);
        center.AddChild(layout);
        button.AddChild(center);
    }

    private static TextureRect CreateTextureRect(
        string texturePath,
        string name,
        TextureRect.StretchModeEnum stretchMode)
    {
        return new TextureRect
        {
            Name = name,
            Texture = LoadTexture(texturePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = stretchMode,
        };
    }

    private static Texture2D? LoadTexture(string texturePath)
    {
        if (IsPlaceholderUiTexture(texturePath))
        {
            return CreateFallbackBackgroundTexture(texturePath);
        }

        if (ResourceLoader.Exists(texturePath))
        {
            return GD.Load<Texture2D>(texturePath);
        }

        GD.PushWarning($"Missing UI texture: {texturePath}");
        return null;
    }

    private static StyleBox? CreateTexturePanelStyle(string texturePath, int margin)
    {
        if (IsPlaceholderChromeTexture(texturePath))
        {
            return null;
        }

        var texture = LoadTexture(texturePath);
        if (texture is null)
        {
            return null;
        }

        return CreateTextureStyle(texture, margin, Colors.White);
    }

    private static StyleBox? CreateTextureButtonStyle(string texturePath, int margin, float brightness)
    {
        if (IsPlaceholderChromeTexture(texturePath))
        {
            return null;
        }

        var texture = LoadTexture(texturePath);
        if (texture is null)
        {
            return null;
        }

        return CreateTextureStyle(texture, margin, new Color(brightness, brightness, brightness, 1f));
    }

    private static StyleBoxTexture CreateTextureStyle(Texture2D texture, int margin, Color modulate)
    {
        return new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = margin,
            TextureMarginTop = margin,
            TextureMarginRight = margin,
            TextureMarginBottom = margin,
            ContentMarginLeft = 12,
            ContentMarginTop = 10,
            ContentMarginRight = 12,
            ContentMarginBottom = 10,
            ModulateColor = modulate,
        };
    }

    private static bool IsPlaceholderUiTexture(string texturePath)
    {
        return texturePath.StartsWith("res://Assets/Art/UI/", System.StringComparison.Ordinal);
    }

    private static bool IsPlaceholderChromeTexture(string texturePath)
    {
        return texturePath.Contains("/Common/", System.StringComparison.Ordinal) ||
            texturePath.EndsWith("/battle_stage.png", System.StringComparison.Ordinal);
    }

    private static Texture2D? CreateFallbackBackgroundTexture(string texturePath)
    {
        if (!texturePath.Contains("/bg_", System.StringComparison.Ordinal))
        {
            return null;
        }

        var color = texturePath.Contains("/Town/", System.StringComparison.Ordinal)
            ? new Color(0.025f, 0.027f, 0.08f, 1f)
            : texturePath.Contains("/RoomChallenge/", System.StringComparison.Ordinal)
                ? new Color(0.027f, 0.035f, 0.094f, 1f)
                : texturePath.Contains("/DungeonPlan/", System.StringComparison.Ordinal)
                    ? new Color(0.025f, 0.024f, 0.075f, 1f)
                    : new Color(0.022f, 0.023f, 0.07f, 1f);
        var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8);
        image.Fill(color);
        return ImageTexture.CreateFromImage(image);
    }

    private static string GetPanelTexturePath(UiPanelStyle style)
    {
        return style switch
        {
            UiPanelStyle.Battle => UiThemePaths.BattleStage,
            UiPanelStyle.Card or UiPanelStyle.Token => UiThemePaths.CardPanel,
            _ => UiThemePaths.MainPanel,
        };
    }

    private static string GetButtonTexturePath(UiButtonStyle style)
    {
        return style switch
        {
            UiButtonStyle.Primary => UiThemePaths.PrimaryButton,
            UiButtonStyle.Danger => UiThemePaths.DangerButton,
            _ => UiThemePaths.SecondaryButton,
        };
    }

    private static StyleBoxFlat CreatePanelStyle(UiPanelStyle style)
    {
        var panel = new StyleBoxFlat
        {
            BgColor = style switch
            {
                UiPanelStyle.Battle => new Color(0.05f, 0.055f, 0.13f, 0.9f),
                UiPanelStyle.Card => new Color(0.08f, 0.075f, 0.16f, 0.88f),
                UiPanelStyle.Overlay => new Color(0.055f, 0.05f, 0.12f, 0.96f),
                UiPanelStyle.Token => new Color(0.06f, 0.055f, 0.12f, 0.72f),
                _ => new Color(0.07f, 0.065f, 0.14f, 0.9f),
            },
            BorderColor = new Color(0.62f, 0.52f, 0.31f, 0.95f),
            BorderWidthLeft = style == UiPanelStyle.Token ? 2 : 4,
            BorderWidthTop = style == UiPanelStyle.Token ? 2 : 4,
            BorderWidthRight = style == UiPanelStyle.Token ? 2 : 4,
            BorderWidthBottom = style == UiPanelStyle.Token ? 2 : 4,
            CornerRadiusTopLeft = style == UiPanelStyle.Token ? 10 : 18,
            CornerRadiusTopRight = style == UiPanelStyle.Token ? 10 : 18,
            CornerRadiusBottomRight = style == UiPanelStyle.Token ? 10 : 18,
            CornerRadiusBottomLeft = style == UiPanelStyle.Token ? 10 : 18,
            ContentMarginLeft = 12,
            ContentMarginTop = 10,
            ContentMarginRight = 12,
            ContentMarginBottom = 10,
        };
        return panel;
    }

    private static StyleBoxFlat CreateFacilityPanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.055f, 0.025f, 0.12f, 0.86f),
            BorderColor = new Color(0.64f, 0.3f, 0.9f, 0.98f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            CornerRadiusBottomRight = 7,
            CornerRadiusBottomLeft = 7,
            ShadowColor = new Color(0.1f, 0.0f, 0.24f, 0.82f),
            ShadowSize = 4,
            ContentMarginLeft = 4,
            ContentMarginTop = 3,
            ContentMarginRight = 4,
            ContentMarginBottom = 3,
        };
    }

    private static StyleBoxFlat CreateDungeonPlanPanelStyle(float alpha, int borderWidth)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.014f, 0.07f, alpha),
            BorderColor = new Color(0.58f, 0.22f, 0.82f, 0.96f),
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ShadowColor = new Color(0.14f, 0f, 0.3f, 0.64f),
            ShadowSize = borderWidth == 2 ? 3 : 6,
            ContentMarginLeft = 10,
            ContentMarginTop = 8,
            ContentMarginRight = 10,
            ContentMarginBottom = 8,
        };
    }

    private static StyleBoxFlat CreateDungeonEmblemButtonStyle(bool selected, float brightness)
    {
        var baseColor = selected
            ? new Color(0.28f, 0.12f, 0.47f, 0.94f)
            : new Color(0.045f, 0.025f, 0.12f, 0.9f);
        return new StyleBoxFlat
        {
            BgColor = new Color(baseColor.R * brightness, baseColor.G * brightness, baseColor.B * brightness, baseColor.A),
            BorderColor = selected
                ? new Color(0.95f, 0.55f, 1f, 1f)
                : new Color(0.54f, 0.24f, 0.78f, 0.98f),
            BorderWidthLeft = selected ? 4 : 3,
            BorderWidthTop = selected ? 4 : 3,
            BorderWidthRight = selected ? 4 : 3,
            BorderWidthBottom = selected ? 4 : 3,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            CornerRadiusBottomRight = 7,
            CornerRadiusBottomLeft = 7,
            ShadowColor = new Color(0.48f, 0.08f, 0.75f, selected ? 0.92f : 0.55f),
            ShadowSize = selected ? 7 : 4,
            ContentMarginLeft = 6,
            ContentMarginTop = 4,
            ContentMarginRight = 6,
            ContentMarginBottom = 4,
        };
    }

    private static StyleBoxFlat CreateButtonStyle(UiButtonStyle style, bool pressed, float brightness)
    {
        var color = style switch
        {
            UiButtonStyle.Primary => new Color(0.46f, 0.35f, 0.66f),
            UiButtonStyle.Danger => new Color(0.5f, 0.18f, 0.22f),
            _ => new Color(0.22f, 0.27f, 0.48f),
        };

        color = new Color(color.R * brightness, color.G * brightness, color.B * brightness, pressed ? 0.98f : 0.94f);
        return new StyleBoxFlat
        {
            BgColor = color,
            BorderColor = new Color(0.72f, 0.58f, 0.32f, pressed ? 0.75f : 0.95f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 14,
            ContentMarginTop = 8,
            ContentMarginRight = 14,
            ContentMarginBottom = 8,
        };
    }
}
