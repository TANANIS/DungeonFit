using Godot;

namespace DungeonFit.UI;

public sealed record HubHeaderControls(
    Label NameLevelLabel,
    ProgressBar ExpBar,
    Label GoldLabel,
    Button ActionButton);

public static class HubHeaderBuilder
{
    public static PanelContainer Build(string actionText, out HubHeaderControls controls)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 132),
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        panel.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 18);
        margin.AddChild(row);

        var portrait = new PanelContainer
        {
            CustomMinimumSize = new Vector2(92, 92),
        };
        row.AddChild(portrait);

        var portraitLabel = new Label
        {
            Text = "YOU",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        portraitLabel.AddThemeFontSizeOverride("font_size", 24);
        portrait.AddChild(portraitLabel);

        var identityColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        identityColumn.AddThemeConstantOverride("separation", 6);
        row.AddChild(identityColumn);

        var nameLevel = new Label();
        nameLevel.AddThemeFontSizeOverride("font_size", 32);
        identityColumn.AddChild(nameLevel);

        var expBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 20),
            ShowPercentage = false,
        };
        identityColumn.AddChild(expBar);

        var goldLabel = new Label
        {
            VerticalAlignment = VerticalAlignment.Center,
        };
        goldLabel.AddThemeFontSizeOverride("font_size", 34);
        row.AddChild(goldLabel);

        var actionButton = new Button
        {
            Text = actionText,
            CustomMinimumSize = new Vector2(86, 86),
        };
        actionButton.AddThemeFontSizeOverride("font_size", 28);
        row.AddChild(actionButton);

        controls = new HubHeaderControls(nameLevel, expBar, goldLabel, actionButton);
        return panel;
    }

    public static void Refresh(HubHeaderControls controls, int level, int experience, int experienceToNextLevel, int gold)
    {
        controls.NameLevelLabel.Text = $"冒險者    Lv.{level}";
        controls.ExpBar.MaxValue = Mathf.Max(1, experienceToNextLevel);
        controls.ExpBar.Value = Mathf.Clamp(experience, 0, Mathf.Max(1, experienceToNextLevel));
        controls.GoldLabel.Text = $"金幣  {gold}";
    }
}
