using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;

namespace DungeonFit.UI;

public partial class RouteSlotDialogView : Control
{
    public event Action<DungeonRouteSlot>? RouteSlotConfirmed;

    private static readonly int[] RepCycle = { 8, 10, 12, 15, 20 };

    private readonly ExerciseCatalog _exerciseCatalog = new();
    private readonly MusicCatalog _musicCatalog = new();
    private readonly DungeonRouteRules _routeRules = new();
    private readonly List<Button> _exerciseButtons = new();
    private readonly List<Button> _filterButtons = new();
    private readonly List<Button> _restButtons = new();
    private readonly List<Button> _musicButtons = new();

    private DungeonCategory _category = null!;
    private string _selectedExerciseId = string.Empty;
    private int _selectedSets = DungeonRouteRules.DefaultSets;
    private int _selectedReps = DungeonRouteRules.DefaultReps;
    private int _selectedRestSeconds = DungeonRouteRules.DefaultRestSeconds;
    private int _selectedMusicIndex;
    private ExerciseFilter _activeFilter = ExerciseFilter.All;

    private Label _title = null!;
    private Label _subtitle = null!;
    private Button _setCard = null!;
    private Button _repCard = null!;
    private Label _musicText = null!;
    private Label _currentExerciseName = null!;
    private Label _currentExerciseTags = null!;
    private Label _currentExerciseDetail = null!;
    private VBoxContainer _exerciseList = null!;
    private Control _musicPopup = null!;
    private VBoxContainer _musicList = null!;
    private Label _musicPopupTitle = null!;
    private AudioStreamPlayer _previewPlayer = null!;

    public override void _Ready()
    {
        BuildMobileOverlay();
        BuildMusicPopup();
        Visible = false;
    }

    public void OpenForDungeon(DungeonCategory category)
    {
        _category = category;
        _title.Text = $"{category.ShortName}地城・討伐契約";
        _subtitle.Text = GetCategorySubtitle(category.Id);
        _selectedSets = DungeonRouteRules.DefaultSets;
        _selectedReps = DungeonRouteRules.DefaultReps;
        _selectedRestSeconds = DungeonRouteRules.DefaultRestSeconds;
        _selectedMusicIndex = 0;
        _activeFilter = ExerciseFilter.All;

        var defaultExercise = _exerciseCatalog.GetDefaultForDungeon(category.Id);
        _selectedExerciseId = defaultExercise.Id;
        RefreshAll();
        Visible = true;
    }

    public bool SmokeOpenMusicPopup()
    {
        if (!Visible)
        {
            return false;
        }

        OpenMusicPopup();
        return _musicPopup.Visible;
    }

    private void BuildMobileOverlay()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var scrim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.66f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(scrim);

        var dialogMargin = new MarginContainer();
        dialogMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        dialogMargin.AddThemeConstantOverride("margin_left", 28);
        dialogMargin.AddThemeConstantOverride("margin_top", 118);
        dialogMargin.AddThemeConstantOverride("margin_right", 28);
        dialogMargin.AddThemeConstantOverride("margin_bottom", 90);
        AddChild(dialogMargin);

        var sheet = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        DungeonFitUi.ApplyPanel(sheet, UiPanelStyle.Overlay);
        dialogMargin.AddChild(sheet);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 22);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_right", 22);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        sheet.AddChild(margin);

        var layout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", 10);
        margin.AddChild(layout);

        layout.AddChild(BuildHeader());
        layout.AddChild(BuildParameterPanel());
        layout.AddChild(BuildCurrentExercisePanel());
        layout.AddChild(BuildFilterRow());
        layout.AddChild(BuildExerciseScroll());
        layout.AddChild(BuildActionRow());

        _previewPlayer = new AudioStreamPlayer
        {
            Name = "MusicPreviewPlayer",
        };
        AddChild(_previewPlayer);
    }

    private Control BuildHeader()
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 84),
        };
        row.AddThemeConstantOverride("separation", 14);

        var icon = CreateToken("盾");
        row.AddChild(icon);

        var titleStack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _title = CreateLabel(Text.RouteSettings, 34, HorizontalAlignment.Left);
        _subtitle = CreateLabel(string.Empty, 20, HorizontalAlignment.Left);
        _subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        titleStack.AddChild(_title);
        titleStack.AddChild(_subtitle);
        row.AddChild(titleStack);

        var closeButton = new Button
        {
            Text = "X",
            CustomMinimumSize = new Vector2(62, 62),
        };
        closeButton.AddThemeFontSizeOverride("font_size", 30);
        DungeonFitUi.ApplyButton(closeButton, UiButtonStyle.Danger);
        closeButton.Pressed += Close;
        row.AddChild(closeButton);

        return row;
    }

    private Control BuildParameterPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 184),
        };
        DungeonFitUi.ApplyPanel(panel, UiPanelStyle.Card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        var title = CreateLabel(Text.TrainingParams, 21, HorizontalAlignment.Left);
        layout.AddChild(title);

        var cards = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 66),
        };
        cards.AddThemeConstantOverride("separation", 8);
        layout.AddChild(cards);

        _setCard = CreateParameterCard();
        _setCard.Pressed += CycleSets;
        cards.AddChild(_setCard);

        _repCard = CreateParameterCard();
        _repCard.Pressed += CycleReps;
        cards.AddChild(_repCard);

        var restGroup = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 52),
        };
        restGroup.AddThemeConstantOverride("separation", 6);
        layout.AddChild(restGroup);

        foreach (var seconds in DungeonRouteRules.RestSecondOptions)
        {
            var restButton = new Button
            {
                Text = $"{seconds}s",
                CustomMinimumSize = new Vector2(0, 50),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            restButton.AddThemeFontSizeOverride("font_size", 20);
            restButton.Pressed += () =>
            {
                _selectedRestSeconds = seconds;
                RefreshRestButtons();
            };
            _restButtons.Add(restButton);
            restGroup.AddChild(restButton);
        }

        layout.AddChild(BuildMusicRow());
        return panel;
    }

    private Button CreateParameterCard()
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(0, 66),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        button.AddThemeFontSizeOverride("font_size", 22);
        DungeonFitUi.ApplyButton(button, UiButtonStyle.Secondary);
        return button;
    }

    private Button BuildMusicRow()
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(0, 56),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        DungeonFitUi.ApplyButton(button, UiButtonStyle.Secondary);
        button.Pressed += OpenMusicPopup;

        _musicText = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
        };
        _musicText.AddThemeFontSizeOverride("font_size", 21);
        button.AddChild(_musicText);
        _musicText.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, margin: 12);

        return button;
    }

    private Control BuildCurrentExercisePanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 110),
        };
        DungeonFitUi.ApplyPanel(panel, UiPanelStyle.Card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 5);
        margin.AddChild(layout);

        var header = new HBoxContainer();
        layout.AddChild(header);
        var title = CreateLabel(Text.CurrentExercise, 21, HorizontalAlignment.Left);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        header.AddChild(CreateLabel(Text.CollapseHint, 18, HorizontalAlignment.Right));

        _currentExerciseName = CreateLabel(string.Empty, 28, HorizontalAlignment.Left);
        layout.AddChild(_currentExerciseName);
        _currentExerciseTags = CreateLabel(string.Empty, 18, HorizontalAlignment.Left);
        layout.AddChild(_currentExerciseTags);
        _currentExerciseDetail = CreateLabel(string.Empty, 17, HorizontalAlignment.Left);
        _currentExerciseDetail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        layout.AddChild(_currentExerciseDetail);
        return panel;
    }

    private Control BuildFilterRow()
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 48),
        };
        row.AddThemeConstantOverride("separation", 6);

        foreach (var filter in new[] { ExerciseFilter.All, ExerciseFilter.Recommended, ExerciseFilter.Machine, ExerciseFilter.Dumbbell, ExerciseFilter.Bodyweight })
        {
            var button = new Button
            {
                Text = GetFilterLabel(filter),
                CustomMinimumSize = new Vector2(0, 46),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.AddThemeFontSizeOverride("font_size", 18);
            button.SetMeta(Meta.Filter, (int)filter);
            button.Pressed += () =>
            {
                _activeFilter = filter;
                RefreshExerciseChoices();
                RefreshFilterButtons();
            };
            _filterButtons.Add(button);
            row.AddChild(button);
        }

        return row;
    }

    private Control BuildExerciseScroll()
    {
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 248),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        _exerciseList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _exerciseList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_exerciseList);
        return scroll;
    }

    private Control BuildActionRow()
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 74),
        };
        row.AddThemeConstantOverride("separation", 14);

        var cancelButton = new Button
        {
            Text = Text.Cancel,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        cancelButton.AddThemeFontSizeOverride("font_size", 28);
        DungeonFitUi.ApplyButton(cancelButton, UiButtonStyle.Secondary);
        cancelButton.Pressed += Close;
        row.AddChild(cancelButton);

        var confirmButton = new Button
        {
            Text = Text.AddToRoute,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        confirmButton.AddThemeFontSizeOverride("font_size", 28);
        DungeonFitUi.ApplyButton(confirmButton, UiButtonStyle.Primary);
        confirmButton.Pressed += Confirm;
        row.AddChild(confirmButton);

        return row;
    }

    private void BuildMusicPopup()
    {
        _musicPopup = new Control
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _musicPopup.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_musicPopup);

        var scrim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.74f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _musicPopup.AddChild(scrim);

        var dialogMargin = new MarginContainer();
        dialogMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        dialogMargin.AddThemeConstantOverride("margin_left", 28);
        dialogMargin.AddThemeConstantOverride("margin_top", 150);
        dialogMargin.AddThemeConstantOverride("margin_right", 28);
        dialogMargin.AddThemeConstantOverride("margin_bottom", 130);
        _musicPopup.AddChild(dialogMargin);

        var sheet = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        DungeonFitUi.ApplyPanel(sheet, UiPanelStyle.Overlay);
        dialogMargin.AddChild(sheet);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        sheet.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 12);
        margin.AddChild(layout);

        var header = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 58),
        };
        layout.AddChild(header);
        _musicPopupTitle = CreateLabel(Text.MusicPopupTitle, 30, HorizontalAlignment.Left);
        _musicPopupTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_musicPopupTitle);

        var closeButton = new Button
        {
            Text = "X",
            CustomMinimumSize = new Vector2(58, 58),
        };
        DungeonFitUi.ApplyButton(closeButton, UiButtonStyle.Danger);
        closeButton.Pressed += CloseMusicPopup;
        header.AddChild(closeButton);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 530),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        layout.AddChild(scroll);

        _musicList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _musicList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_musicList);

        var doneButton = new Button
        {
            Text = Text.ConfirmMusic,
            CustomMinimumSize = new Vector2(0, 68),
        };
        doneButton.AddThemeFontSizeOverride("font_size", 28);
        DungeonFitUi.ApplyButton(doneButton, UiButtonStyle.Primary);
        doneButton.Pressed += CloseMusicPopup;
        layout.AddChild(doneButton);
    }

    private void RefreshAll()
    {
        RefreshParameterCards();
        RefreshRestButtons();
        RefreshMusicRow();
        RefreshCurrentExercise();
        RefreshFilterButtons();
        RefreshExerciseChoices();
    }

    private void RefreshParameterCards()
    {
        _setCard.Text = $"{Text.SetCount}\n{_selectedSets}";
        _repCard.Text = $"{Text.RepCount}\n{_selectedReps}";
    }

    private void RefreshRestButtons()
    {
        foreach (var button in _restButtons)
        {
            var secondsText = button.Text.TrimEnd('s');
            var seconds = int.TryParse(secondsText, out var parsed) ? parsed : 0;
            DungeonFitUi.ApplyButton(button, seconds == _selectedRestSeconds ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
        }
    }

    private void RefreshMusicRow()
    {
        var track = GetSelectedTrack();
        _musicText.Text = $"{Text.Music}   {GetCompactTrackName(track)}  /  {track.Bpm} BPM     >";
    }

    private void RefreshCurrentExercise()
    {
        var selectedExercise = _exerciseCatalog.GetById(_category.Id, _selectedExerciseId);
        _currentExerciseName.Text = selectedExercise.Name;
        _currentExerciseTags.Text = BuildExerciseTags(selectedExercise);
        _currentExerciseDetail.Text = $"{selectedExercise.Summary}\n注意：{selectedExercise.SafetyNote}";
    }

    private void RefreshFilterButtons()
    {
        foreach (var button in _filterButtons)
        {
            var filter = (ExerciseFilter)button.GetMeta(Meta.Filter).AsInt32();
            DungeonFitUi.ApplyButton(button, filter == _activeFilter ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
        }
    }

    private void RefreshExerciseChoices()
    {
        ClearChildren(_exerciseList);
        _exerciseButtons.Clear();

        foreach (var exercise in GetFilteredExercises())
        {
            var button = new Button
            {
                Text = BuildExerciseButtonText(exercise),
                CustomMinimumSize = new Vector2(0, 72),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            button.AddThemeFontSizeOverride("font_size", 20);
            button.SetMeta(Meta.ExerciseId, exercise.Id);
            button.Pressed += () => SelectExercise(exercise.Id);
            _exerciseButtons.Add(button);
            _exerciseList.AddChild(button);
        }

        RefreshExerciseButtonStyles();
    }

    private IEnumerable<ExerciseDefinition> GetFilteredExercises()
    {
        return _exerciseCatalog.GetForDungeon(_category.Id)
            .Where(exercise => _activeFilter switch
            {
                ExerciseFilter.Recommended => exercise.IsRecommended,
                ExerciseFilter.Machine => exercise.TrainingType == Text.Machine,
                ExerciseFilter.Dumbbell => exercise.TrainingType == Text.Dumbbell,
                ExerciseFilter.Bodyweight => exercise.TrainingType == Text.Bodyweight,
                _ => true,
            });
    }

    private void SelectExercise(string exerciseId)
    {
        _selectedExerciseId = exerciseId;
        RefreshCurrentExercise();
        RefreshExerciseButtonStyles();
    }

    private void RefreshExerciseButtonStyles()
    {
        foreach (var button in _exerciseButtons)
        {
            var id = button.GetMeta(Meta.ExerciseId, string.Empty).AsString();
            DungeonFitUi.ApplyButton(button, id == _selectedExerciseId ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
        }
    }

    private static string BuildExerciseButtonText(ExerciseDefinition exercise)
    {
        var selectedMark = exercise.IsRecommended ? "  ★推薦" : string.Empty;
        return $"{exercise.Name}{selectedMark}\n{exercise.TrainingType} · {exercise.Summary}";
    }

    private static string BuildExerciseTags(ExerciseDefinition exercise)
    {
        var recommended = exercise.IsRecommended ? " / 推薦" : string.Empty;
        return $"{exercise.TrainingType}{recommended}";
    }

    private void CycleSets()
    {
        _selectedSets = _selectedSets >= DungeonRouteRules.MaxSets
            ? DungeonRouteRules.MinSets
            : _selectedSets + 1;
        RefreshParameterCards();
    }

    private void CycleReps()
    {
        var index = Array.IndexOf(RepCycle, _selectedReps);
        _selectedReps = RepCycle[(index + 1) % RepCycle.Length];
        RefreshParameterCards();
    }

    private void OpenMusicPopup()
    {
        StopPreview();
        RefreshMusicPopup();
        _musicPopup.Visible = true;
        _musicPopup.MoveToFront();
    }

    private void RefreshMusicPopup()
    {
        ClearChildren(_musicList);
        _musicButtons.Clear();
        var tracks = _musicCatalog.GetAll();

        for (var index = 0; index < tracks.Count; index++)
        {
            var track = tracks[index];
            var row = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(0, 70),
            };
            row.AddThemeConstantOverride("separation", 8);

            var selectButton = new Button
            {
                Text = $"{(index == _selectedMusicIndex ? "✓ " : string.Empty)}{GetCompactTrackName(track)}\n{track.Bpm} BPM",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 70),
            };
            selectButton.AddThemeFontSizeOverride("font_size", 20);
            selectButton.SetMeta(Meta.MusicIndex, index);
            selectButton.Pressed += () =>
            {
                _selectedMusicIndex = index;
                StopPreview();
                RefreshMusicRow();
                RefreshMusicPopup();
            };
            DungeonFitUi.ApplyButton(selectButton, index == _selectedMusicIndex ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            _musicButtons.Add(selectButton);
            row.AddChild(selectButton);

            var previewButton = new Button
            {
                Text = Text.Preview,
                CustomMinimumSize = new Vector2(104, 70),
            };
            previewButton.AddThemeFontSizeOverride("font_size", 20);
            DungeonFitUi.ApplyButton(previewButton, UiButtonStyle.Secondary);
            previewButton.Disabled = !ResourceLoader.Exists(track.ResourcePath);
            previewButton.Pressed += () => PreviewTrack(track);
            row.AddChild(previewButton);

            _musicList.AddChild(row);
        }
    }

    private void PreviewTrack(MusicTrack track)
    {
        StopPreview();
        if (!ResourceLoader.Exists(track.ResourcePath))
        {
            GD.PushWarning($"Music stream not found: {track.ResourcePath}");
            return;
        }

        _previewPlayer.Stream = GD.Load<AudioStream>(track.ResourcePath);
        _previewPlayer.Play();
    }

    private void CloseMusicPopup()
    {
        StopPreview();
        _musicPopup.Visible = false;
    }

    private void StopPreview()
    {
        if (_previewPlayer is null)
        {
            return;
        }

        _previewPlayer.Stop();
        _previewPlayer.Stream = null;
    }

    private MusicTrack GetSelectedTrack()
    {
        var tracks = _musicCatalog.GetAll();
        return _selectedMusicIndex >= 0 && _selectedMusicIndex < tracks.Count
            ? tracks[_selectedMusicIndex]
            : tracks[0];
    }

    private void Confirm()
    {
        var slot = new DungeonRouteSlot(
            _category.Id,
            _selectedSets,
            _selectedReps,
            GetSelectedTrack().Id,
            _selectedRestSeconds,
            _selectedExerciseId);
        RouteSlotConfirmed?.Invoke(_routeRules.Normalize(slot));
        Close();
    }

    private void Close()
    {
        StopPreview();
        _musicPopup.Visible = false;
        Visible = false;
    }

    private static Label CreateLabel(string text, int fontSize, HorizontalAlignment alignment)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static PanelContainer CreateToken(string text)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(74, 74),
        };
        DungeonFitUi.ApplyPanel(panel, UiPanelStyle.Token);

        var label = CreateLabel(text, 30, HorizontalAlignment.Center);
        label.VerticalAlignment = VerticalAlignment.Center;
        panel.AddChild(label);
        return panel;
    }

    private static string GetFilterLabel(ExerciseFilter filter)
    {
        return filter switch
        {
            ExerciseFilter.Recommended => Text.Recommended,
            ExerciseFilter.Machine => Text.Machine,
            ExerciseFilter.Dumbbell => Text.Dumbbell,
            ExerciseFilter.Bodyweight => Text.Bodyweight,
            _ => Text.All,
        };
    }

    private static string GetCategorySubtitle(string categoryId)
    {
        return categoryId switch
        {
            "chest" => "穩定訓練，適合作為起始房間。",
            "shoulders" => "垂直推舉與肩部控制訓練。",
            "back" => "划船與下拉路線，建立背部穩定。",
            "legs" => "下肢推蹬與單腳控制訓練。",
            "core" => "核心穩定與呼吸節奏訓練。",
            "arms" => "手臂彎舉與下壓的短回合訓練。",
            _ => "選擇熟悉且可穩定完成的訓練動作。",
        };
    }

    private static string GetCompactTrackName(MusicTrack track)
    {
        return track.DisplayName.Replace($" ({track.Bpm} BPM)", string.Empty);
    }

    private static void ClearChildren(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private enum ExerciseFilter
    {
        All,
        Recommended,
        Machine,
        Dumbbell,
        Bodyweight,
    }

    private static class Meta
    {
        public const string ExerciseId = "exercise_id";
        public const string Filter = "filter";
        public const string MusicIndex = "music_index";
    }

    private static class Text
    {
        public const string RouteSettings = "討伐設定";
        public const string TrainingParams = "訓練參數";
        public const string SetCount = "組數";
        public const string RepCount = "次數";
        public const string Music = "音樂";
        public const string CurrentExercise = "當前動作";
        public const string CollapseHint = "收合動作 ▲";
        public const string Recommended = "推薦";
        public const string All = "全部";
        public const string Machine = "器械";
        public const string Dumbbell = "啞鈴";
        public const string Bodyweight = "徒手";
        public const string AddToRoute = "加入今日路線";
        public const string Cancel = "取消";
        public const string MusicPopupTitle = "選擇音樂";
        public const string Preview = "試聽";
        public const string ConfirmMusic = "套用音樂";
    }
}
