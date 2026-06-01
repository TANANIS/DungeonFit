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
    private Label _npcPortrait = null!;
    private Label _npcName = null!;
    private Label _reward = null!;
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
        DungeonFitUi.AddBackground(this, UiThemePaths.CommonBackground);

        var safeMargin = new MarginContainer();
        safeMargin.SetAnchorsPreset(LayoutPreset.FullRect);
        safeMargin.AddThemeConstantOverride("margin_left", 38);
        safeMargin.AddThemeConstantOverride("margin_top", 44);
        safeMargin.AddThemeConstantOverride("margin_right", 38);
        safeMargin.AddThemeConstantOverride("margin_bottom", 44);
        AddChild(safeMargin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 18);
        safeMargin.AddChild(layout);

        layout.AddChild(BuildHeader());
        layout.AddChild(BuildBoard());
        layout.AddChild(BuildButtonRow());
    }

    private Control BuildHeader()
    {
        var panel = HubHeaderBuilder.Build(Text.BackTownShort, out _header);
        _header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
        return panel;
    }

    private Control BuildBoard()
    {
        var panel = new PanelContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        DungeonFitUi.ApplyPanel(panel, UiPanelStyle.Main);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 26);
        margin.AddThemeConstantOverride("margin_top", 26);
        margin.AddThemeConstantOverride("margin_right", 26);
        margin.AddThemeConstantOverride("margin_bottom", 26);
        panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 22);
        margin.AddChild(layout);

        var title = new Label
        {
            Text = $"{Text.BoardTitle}\n{Text.RefreshHint}",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        title.AddThemeFontSizeOverride("font_size", 38);
        layout.AddChild(title);

        _questGrid = new GridContainer
        {
            Columns = 3,
            CustomMinimumSize = new Vector2(0, 520),
        };
        _questGrid.AddThemeConstantOverride("h_separation", 18);
        _questGrid.AddThemeConstantOverride("v_separation", 18);
        layout.AddChild(_questGrid);

        layout.AddChild(BuildDetailPanel());
        return panel;
    }

    private Control BuildDetailPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 520),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        DungeonFitUi.ApplyPanel(panel, UiPanelStyle.Card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 30);
        margin.AddThemeConstantOverride("margin_top", 28);
        margin.AddThemeConstantOverride("margin_right", 30);
        margin.AddThemeConstantOverride("margin_bottom", 28);
        panel.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 24);
        margin.AddChild(row);

        var textLayout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        textLayout.AddThemeConstantOverride("separation", 14);
        row.AddChild(textLayout);

        _title = CreateLabel(34);
        textLayout.AddChild(_title);

        _description = CreateLabel(25);
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textLayout.AddChild(_description);

        _requirement = CreateLabel(28);
        _requirement.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textLayout.AddChild(_requirement);

        _progress = CreateLabel(26);
        textLayout.AddChild(_progress);

        _reward = CreateLabel(25);
        _reward.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textLayout.AddChild(_reward);

        var npcLayout = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(270, 0),
        };
        npcLayout.AddThemeConstantOverride("separation", 16);
        row.AddChild(npcLayout);

        _npcPortrait = new Label
        {
            CustomMinimumSize = new Vector2(260, 240),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _npcPortrait.AddThemeFontSizeOverride("font_size", 28);
        npcLayout.AddChild(_npcPortrait);

        _npcName = CreateLabel(28, HorizontalAlignment.Center);
        npcLayout.AddChild(_npcName);

        _primaryButton = new Button
        {
            CustomMinimumSize = new Vector2(0, 86),
        };
        _primaryButton.AddThemeFontSizeOverride("font_size", 30);
        DungeonFitUi.ApplyButton(_primaryButton, UiButtonStyle.Primary);
        _primaryButton.Pressed += HandlePrimaryAction;
        npcLayout.AddChild(_primaryButton);

        return panel;
    }

    private Control BuildButtonRow()
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 112),
        };
        row.AddThemeConstantOverride("separation", 18);

        var townButton = new Button
        {
            Text = Text.BackTown,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        townButton.AddThemeFontSizeOverride("font_size", 32);
        DungeonFitUi.ApplyButton(townButton, UiButtonStyle.Secondary);
        townButton.Pressed += () => BackToTownRequested?.Invoke();
        row.AddChild(townButton);

        var dungeonButton = new Button
        {
            Text = Text.EnterDungeon,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        dungeonButton.AddThemeFontSizeOverride("font_size", 38);
        DungeonFitUi.ApplyButton(dungeonButton, UiButtonStyle.Primary);
        dungeonButton.Pressed += () => EnterDungeonRequested?.Invoke();
        row.AddChild(dungeonButton);

        return row;
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
            var card = new Button
            {
                Text = BuildCardText(quest, index == _selectedIndex),
                CustomMinimumSize = new Vector2(0, 245),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            card.AddThemeFontSizeOverride("font_size", 25);
            DungeonFitUi.ApplyButton(card, index == _selectedIndex ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            card.Pressed += () =>
            {
                _selectedIndex = questIndex;
                Refresh();
            };
            _questGrid.AddChild(card);
        }

        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (_quests.Count == 0)
        {
            return;
        }

        var quest = _quests[Math.Clamp(_selectedIndex, 0, _quests.Count - 1)];
        var activeQuest = FindActiveQuest(quest.Id);
        var isActive = activeQuest is not null;
        var isCompleted = activeQuest is not null && activeQuest.Progress >= quest.RequiredAmount;
        var isClaimed = activeQuest?.IsClaimed == true;
        _title.Text = quest.Title;
        _description.Text = quest.Description;
        _requirement.Text = $"{Text.RequirementTitle}\n{quest.RequirementText}";
        _progress.Text = string.Format(Text.ProgressFormat, activeQuest?.Progress ?? 0, quest.RequiredAmount);
        _reward.Text = string.Format(Text.RewardFormat, quest.RewardGold);
        _npcPortrait.Text = $"{Text.NpcPortrait}\n{GetIconText(quest.IconType)}";
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
            new()
            {
                QuestId = quest.Id,
            },
        };
        _activeQuests = activeQuests;
        QuestAccepted?.Invoke(quest.Id);
        Refresh();
    }

    private string BuildCardText(ShortTermQuestDefinition quest, bool selected)
    {
        var activeQuest = FindActiveQuest(quest.Id);
        var activeMarker = activeQuest is null
            ? string.Empty
            : activeQuest.IsClaimed
                ? Text.ClaimedMarker
                : activeQuest.Progress >= quest.RequiredAmount
                ? Text.CompletedMarker
                : Text.ActiveMarker;
        var selectedMarker = selected ? Text.SelectedMarker : string.Empty;
        return $"{selectedMarker}{activeMarker}{quest.Title}\n\n{Text.NpcPortrait}\n{GetIconText(quest.IconType)}";
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

    private static string GetIconText(string iconType)
    {
        return iconType switch
        {
            "herb" => Text.IconHerb,
            "chest" => Text.IconChest,
            "pick" => Text.IconPick,
            "heal" => Text.IconHeal,
            _ => Text.IconSword,
        };
    }

    private static Label CreateLabel(int fontSize, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            HorizontalAlignment = alignment,
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
        public const string Back = "<";
        public const string BackTownShort = "\u8fd4\u56de";
        public const string BoardTitle = "\u516c\u544a\u6b04";
        public const string RefreshHint = "24H \u56fa\u5b9a\u5237\u65b0";
        public const string BackTown = "\u8fd4\u56de\u57ce\u93ae";
        public const string EnterDungeon = "\u9032\u5165\u5730\u57ce";
        public const string RequirementTitle = "\u4efb\u52d9\u9700\u6c42";
        public const string ProgressFormat = "\u9032\u5ea6  {0} / {1}";
        public const string RewardFormat = "\u734e\u52f5\u9810\u89bd\uff1a\u91d1\u5e63 +{0}";
        public const string AcceptQuest = "\u63a5\u53d6\u4efb\u52d9";
        public const string Accepted = "\u5df2\u63a5\u53d6";
        public const string ClaimReward = "\u9818\u53d6\u734e\u52f5";
        public const string Claimed = "\u5df2\u5b8c\u6210";
        public const string NpcPortrait = "\u59d4\u8a17\u4eba";
        public const string ActiveMarker = "[*] ";
        public const string CompletedMarker = "[x] ";
        public const string ClaimedMarker = "[v] ";
        public const string SelectedMarker = "[>] ";
        public const string IconHerb = "\u85e5\u8349";
        public const string IconChest = "\u5305\u88f9";
        public const string IconPick = "\u7926\u77f3";
        public const string IconHeal = "\u7948\u9858";
        public const string IconSword = "\u8a0e\u4f10";
    }
}
