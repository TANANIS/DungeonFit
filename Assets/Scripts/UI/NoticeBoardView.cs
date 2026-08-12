using System;
using System.Collections.Generic;
using DungeonFit.Core.Models;
using Godot;

namespace DungeonFit.UI;

public partial class NoticeBoardView : Control
{
    public event Action? BackToTownRequested;
    public event Action? EnterDungeonRequested;
    public event Action<string>? QuestAccepted;
    public event Func<string, bool>? QuestRewardClaimed;

    private IReadOnlyList<ShortTermQuestDefinition> _quests = Array.Empty<ShortTermQuestDefinition>();
    private IReadOnlyList<ActiveShortTermQuest> _activeQuests = Array.Empty<ActiveShortTermQuest>();
    private PlayerState _player = new();
    private int _selectedIndex;
    private HubHeaderControls _header = null!;
    private GridContainer _questGrid = null!;
    private Label _title = null!;
    private Label _description = null!;
    private Label _requirement = null!;
    private Label _progress = null!;
    private Label _reward = null!;
    private TextureRect _npcPortrait = null!;
    private Label _npcName = null!;
    private Button _primaryButton = null!;

    public override void _Ready()
    {
        BuildLayout();
        Refresh();
    }

    public void Initialize(
        IReadOnlyList<ShortTermQuestDefinition> quests,
        IReadOnlyList<ActiveShortTermQuest> activeQuests,
        PlayerState? player = null)
    {
        _quests = quests;
        _activeQuests = activeQuests;
        _player = player ?? new PlayerState();
        _selectedIndex = 0;

        if (IsNodeReady())
        {
            Refresh();
        }
    }

    private void BuildLayout()
    {
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.NoticeBoardBackground);

        var safe = CreateMargin(28, 26);
        safe.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(safe);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 12);
        safe.AddChild(layout);

        var header = HubHeaderBuilder.Build(Text.BackTownShort, out _header);
        ApplyNoticePanel(header, new Color(0.018f, 0.012f, 0.05f, 0.94f), new Color(0.67f, 0.22f, 0.91f, 0.98f), 3);
        DungeonFitUi.ApplyButton(_header.ActionButton, UiButtonStyle.Primary);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        layout.AddChild(header);

        var boardTitle = new Control { CustomMinimumSize = new Vector2(0, 250) };
        layout.AddChild(boardTitle);
        var titlePlateCenter = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        titlePlateCenter.SetAnchorsPreset(LayoutPreset.TopWide);
        titlePlateCenter.OffsetTop = 80;
        titlePlateCenter.OffsetBottom = 194;
        boardTitle.AddChild(titlePlateCenter);
        var titlePlate = new PanelContainer { CustomMinimumSize = new Vector2(480, 104) };
        ApplyNoticePanel(titlePlate, new Color(0.16f, 0.075f, 0.045f, 0.96f), new Color(0.7f, 0.43f, 0.2f, 1f), 4);
        titlePlateCenter.AddChild(titlePlate);
        var boardTitleLayout = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        boardTitleLayout.SetAnchorsPreset(LayoutPreset.TopWide);
        boardTitleLayout.OffsetTop = 84;
        boardTitleLayout.OffsetBottom = 190;
        boardTitleLayout.AddThemeConstantOverride("separation", 2);
        boardTitle.AddChild(boardTitleLayout);
        var title = CreateCenteredLabel(Text.BoardTitle, 68);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0.5f));
        title.AddThemeColorOverride("font_outline_color", new Color(0.22f, 0.075f, 0.32f));
        title.AddThemeConstantOverride("outline_size", 9);
        boardTitleLayout.AddChild(title);
        var refresh = CreateCenteredLabel(Text.RefreshHint, 24);
        refresh.AddThemeColorOverride("font_color", new Color(0.9f, 0.75f, 1f));
        boardTitleLayout.AddChild(refresh);

        _questGrid = new GridContainer
        {
            Columns = 3,
            CustomMinimumSize = new Vector2(0, 640),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _questGrid.AddThemeConstantOverride("h_separation", 14);
        _questGrid.AddThemeConstantOverride("v_separation", 14);
        layout.AddChild(_questGrid);

        layout.AddChild(BuildDetailPanel());

        var enterDungeon = CreateButton(Text.EnterDungeon, 0, 126, 42, UiButtonStyle.Primary);
        AddEnterDungeonContent(enterDungeon);
        enterDungeon.Pressed += () => EnterDungeonRequested?.Invoke();
        layout.AddChild(enterDungeon);
    }

    private Control BuildDetailPanel()
    {
        var parchment = new PanelContainer { CustomMinimumSize = new Vector2(0, 520) };
        ApplyNoticePanel(parchment, new Color(0.75f, 0.59f, 0.39f, 0.94f), new Color(0.26f, 0.13f, 0.08f, 0.98f), 4);

        var margin = CreateMargin(26, 20);
        parchment.AddChild(margin);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 22);
        margin.AddChild(row);

        var textLayout = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        textLayout.AddThemeConstantOverride("separation", 9);
        row.AddChild(textLayout);

        _title = CreateLabel(34);
        _title.AddThemeColorOverride("font_color", new Color(0.24f, 0.06f, 0.42f));
        textLayout.AddChild(_title);
        _description = CreateLabel(22);
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.AddThemeColorOverride("font_color", new Color(0.14f, 0.08f, 0.05f));
        textLayout.AddChild(_description);
        var dividerCenter = new CenterContainer { CustomMinimumSize = new Vector2(0, 32) };
        textLayout.AddChild(dividerCenter);
        dividerCenter.AddChild(DungeonFitUi.CreateIcon(UiThemePaths.NoticeBoardDetailDivider, 260, "QuestDivider"));
        _requirement = CreateLabel(23);
        _requirement.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _requirement.AddThemeColorOverride("font_color", new Color(0.24f, 0.06f, 0.42f));
        textLayout.AddChild(_requirement);
        _progress = CreateLabel(20);
        _progress.AddThemeColorOverride("font_color", new Color(0.45f, 0.1f, 0.08f));
        textLayout.AddChild(_progress);
        _reward = CreateLabel(20);
        _reward.AddThemeColorOverride("font_color", new Color(0.4f, 0.19f, 0.04f));
        textLayout.AddChild(_reward);

        var npcColumn = new VBoxContainer { CustomMinimumSize = new Vector2(260, 0) };
        npcColumn.AddThemeConstantOverride("separation", 4);
        row.AddChild(npcColumn);
        var portraitFrame = new PanelContainer { CustomMinimumSize = new Vector2(250, 210) };
        ApplyNoticePanel(portraitFrame, new Color(0.035f, 0.02f, 0.08f, 0.94f), new Color(0.52f, 0.23f, 0.76f, 0.98f), 3);
        npcColumn.AddChild(portraitFrame);
        var portraitCenter = new CenterContainer();
        portraitFrame.AddChild(portraitCenter);
        _npcPortrait = DungeonFitUi.CreateIcon(UiThemePaths.NoticeBoardQuestGiver(0), 190, "QuestGiverPortrait");
        portraitCenter.AddChild(_npcPortrait);
        _npcName = CreateCenteredLabel(string.Empty, 22);
        _npcName.AddThemeColorOverride("font_color", new Color(0.25f, 0.07f, 0.4f));
        npcColumn.AddChild(_npcName);
        _primaryButton = CreateButton(string.Empty, 0, 84, 28, UiButtonStyle.Primary);
        _primaryButton.Pressed += HandlePrimaryAction;
        npcColumn.AddChild(_primaryButton);

        return parchment;
    }

    private void Refresh()
    {
        if (_questGrid is null)
        {
            return;
        }

        ClearChildren(_questGrid);
        HubHeaderBuilder.Refresh(_header, _player.Level, _player.Experience, _player.ExperienceToNextLevel, _player.Gold);

        for (var index = 0; index < _quests.Count; index++)
        {
            var quest = _quests[index];
            var questIndex = index;
            var card = CreateQuestCard(quest, index, index == _selectedIndex);
            card.Pressed += () =>
            {
                _selectedIndex = questIndex;
                Refresh();
            };
            _questGrid.AddChild(card);
        }

        RefreshDetail();
    }

    private Button CreateQuestCard(ShortTermQuestDefinition quest, int index, bool selected)
    {
        var card = new Button
        {
            Text = string.Empty,
            CustomMinimumSize = new Vector2(280, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        ApplyQuestCardStyle(card, selected);
        if (selected)
        {
            AddSelectionBorder(card);
        }
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        card.AddChild(center);
        var content = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        content.AddThemeConstantOverride("separation", 1);
        center.AddChild(content);
        var cardTitle = CreateCenteredLabel(quest.Title, 21);
        cardTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        cardTitle.CustomMinimumSize = new Vector2(158, 42);
        cardTitle.AddThemeColorOverride("font_color", new Color(0.17f, 0.08f, 0.06f));
        content.AddChild(cardTitle);
        content.AddChild(DungeonFitUi.CreateIcon(UiThemePaths.NoticeBoardQuestGiver(index), 128));

        var status = CreateCenteredLabel(BuildCardStatus(quest), 15);
        status.CustomMinimumSize = new Vector2(158, 19);
        status.AddThemeColorOverride("font_color", selected ? new Color(0.43f, 0.08f, 0.56f) : new Color(0.34f, 0.18f, 0.08f));
        content.AddChild(status);
        if (selected)
        {
            var emblem = DungeonFitUi.CreateIcon(UiThemePaths.NoticeBoardSelectedQuestEmblem, 56, "SelectedQuestEmblem");
            emblem.SetAnchorsPreset(LayoutPreset.TopRight);
            emblem.OffsetLeft = -64;
            emblem.OffsetTop = 4;
            emblem.OffsetRight = -8;
            emblem.OffsetBottom = 60;
            card.AddChild(emblem);
        }
        return card;
    }

    private void RefreshDetail()
    {
        if (_quests.Count == 0)
        {
            return;
        }

        var index = Math.Clamp(_selectedIndex, 0, _quests.Count - 1);
        var quest = _quests[index];
        var activeQuest = FindActiveQuest(quest.Id);
        var isActive = activeQuest is not null;
        var isCompleted = activeQuest is not null && activeQuest.Progress >= quest.RequiredAmount;
        var isClaimed = activeQuest?.IsClaimed == true;
        _title.Text = quest.Title;
        _description.Text = quest.Description;
        _requirement.Text = $"{Text.RequirementTitle}\n{quest.RequirementText}";
        _progress.Text = string.Format(Text.ProgressFormat, activeQuest?.Progress ?? 0, quest.RequiredAmount);
        _reward.Text = string.Format(Text.RewardFormat, quest.RewardGold);
        _npcPortrait.Texture = LoadTexture(UiThemePaths.NoticeBoardQuestGiver(index));
        _npcName.Text = quest.NpcName;
        _primaryButton.Text = isClaimed
            ? Text.Claimed
            : isCompleted
                ? Text.ClaimReward
                : isActive
                    ? Text.Accepted
                    : Text.AcceptQuest;
        _primaryButton.Disabled = isClaimed || (isActive && !isCompleted);
    }

    private void HandlePrimaryAction()
    {
        if (_quests.Count == 0)
        {
            return;
        }

        var quest = _quests[Math.Clamp(_selectedIndex, 0, _quests.Count - 1)];
        var activeQuest = FindActiveQuest(quest.Id);
        if (activeQuest is not null)
        {
            if (activeQuest.Progress >= quest.RequiredAmount && !activeQuest.IsClaimed)
            {
                var claimed = QuestRewardClaimed?.Invoke(quest.Id) ?? false;
                if (claimed)
                {
                    activeQuest.IsClaimed = true;
                    Refresh();
                }
            }

            return;
        }

        var activeQuests = new List<ActiveShortTermQuest>(_activeQuests)
        {
            new() { QuestId = quest.Id },
        };
        _activeQuests = activeQuests;
        QuestAccepted?.Invoke(quest.Id);
        Refresh();
    }

    private string BuildCardStatus(ShortTermQuestDefinition quest)
    {
        var activeQuest = FindActiveQuest(quest.Id);
        if (activeQuest is null)
        {
            return Text.Available;
        }

        return activeQuest.IsClaimed
            ? Text.Claimed
            : activeQuest.Progress >= quest.RequiredAmount
                ? Text.Completed
                : Text.Accepted;
    }

    private ActiveShortTermQuest? FindActiveQuest(string questId)
    {
        foreach (var quest in _activeQuests)
        {
            if (quest.QuestId == questId)
            {
                return quest;
            }
        }

        return null;
    }

    private static void ApplyNoticePanel(PanelContainer panel, Color background, Color border, int borderWidth)
    {
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ShadowColor = new Color(0.12f, 0.01f, 0.28f, 0.75f),
            ShadowSize = 4,
        });
    }

    private static void ApplyQuestCardStyle(Button button, bool selected)
    {
        button.AddThemeStyleboxOverride("normal", CreateQuestCardStyle(selected, 1f));
        button.AddThemeStyleboxOverride("hover", CreateQuestCardStyle(selected, 1.08f));
        button.AddThemeStyleboxOverride("pressed", CreateQuestCardStyle(selected, 0.9f));
    }

    private static void AddSelectionBorder(Control card)
    {
        var purple = new Color(0.92f, 0.3f, 1f, 1f);
        AddBorderSegment(card, 0, 0, 1, 0, 0, 0, 0, 4, purple);
        AddBorderSegment(card, 0, 1, 1, 1, 0, -4, 0, 0, purple);
        AddBorderSegment(card, 0, 0, 0, 1, 0, 0, 4, 0, purple);
        AddBorderSegment(card, 1, 0, 1, 1, -4, 0, 0, 0, purple);
    }

    private static void AddBorderSegment(
        Control parent,
        float left,
        float top,
        float right,
        float bottom,
        float offsetLeft,
        float offsetTop,
        float offsetRight,
        float offsetBottom,
        Color color)
    {
        var segment = new ColorRect { Color = color, MouseFilter = MouseFilterEnum.Ignore };
        segment.AnchorLeft = left;
        segment.AnchorTop = top;
        segment.AnchorRight = right;
        segment.AnchorBottom = bottom;
        segment.OffsetLeft = offsetLeft;
        segment.OffsetTop = offsetTop;
        segment.OffsetRight = offsetRight;
        segment.OffsetBottom = offsetBottom;
        parent.AddChild(segment);
    }

    private static StyleBox CreateQuestCardStyle(bool selected, float brightness)
    {
        var parchment = LoadTexture(UiThemePaths.NoticeBoardQuestParchment);
        if (parchment is not null)
        {
            return new StyleBoxTexture
            {
                Texture = parchment,
                TextureMarginLeft = 38,
                TextureMarginTop = 38,
                TextureMarginRight = 38,
                TextureMarginBottom = 38,
                ContentMarginLeft = 18,
                ContentMarginTop = 16,
                ContentMarginRight = 18,
                ContentMarginBottom = 16,
                ModulateColor = new Color(brightness, brightness, brightness, 1f),
            };
        }

        return new StyleBoxFlat
        {
            BgColor = new Color(0.7f * brightness, 0.53f * brightness, 0.33f * brightness, 1f),
            BorderColor = selected ? new Color(0.92f, 0.38f, 1f, 1f) : new Color(0.19f, 0.1f, 0.07f, 1f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomRight = 5,
            CornerRadiusBottomLeft = 5,
        };
    }

    private static Texture2D? LoadTexture(string path)
    {
        return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    private static MarginContainer CreateMargin(int horizontal, int vertical)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", horizontal);
        margin.AddThemeConstantOverride("margin_top", vertical);
        margin.AddThemeConstantOverride("margin_right", horizontal);
        margin.AddThemeConstantOverride("margin_bottom", vertical);
        return margin;
    }

    private static Button CreateButton(string text, int width, int height, int fontSize, UiButtonStyle style)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(width, height) };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        DungeonFitUi.ApplyButton(button, style);
        return button;
    }

    private static void AddEnterDungeonContent(Button button)
    {
        button.Text = string.Empty;
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        button.AddChild(center);
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        center.AddChild(row);
        row.AddChild(DungeonFitUi.CreateIcon(UiThemePaths.NoticeBoardSelectedQuestEmblem, 54));
        var label = new Label
        {
            Text = Text.EnterDungeon,
            AutowrapMode = TextServer.AutowrapMode.Off,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 42);
        row.AddChild(label);
    }

    private static Label CreateLabel(int fontSize)
    {
        var label = new Label();
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static Label CreateCenteredLabel(string text, int fontSize)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static void ClearChildren(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static class Text
    {
        public const string BackTownShort = "\u8fd4\u56de";
        public const string BoardTitle = "\u516c\u544a\u6b04";
        public const string RefreshHint = "\u6bcf\u65e5\u59d4\u8a17  \u00b7  24H \u56fa\u5b9a\u5237\u65b0";
        public const string EnterDungeon = "\u9032\u5165\u5730\u57ce";
        public const string RequirementTitle = "\u4efb\u52d9\u9700\u6c42";
        public const string ProgressFormat = "\u9032\u5ea6  {0} / {1}";
        public const string RewardFormat = "\u734e\u52f5\u9810\u89bd\uff1a\u91d1\u5e63 +{0}";
        public const string AcceptQuest = "\u63a5\u53d6\u4efb\u52d9";
        public const string Accepted = "\u5df2\u63a5\u53d6";
        public const string ClaimReward = "\u9818\u53d6\u734e\u52f5";
        public const string Claimed = "\u5df2\u5b8c\u6210";
        public const string Completed = "\u53ef\u9818\u734e\u52f5";
        public const string Available = "\u53ef\u63a5\u53d6";
    }
}
