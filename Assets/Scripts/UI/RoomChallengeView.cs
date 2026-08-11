using Godot;
using System;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
using DungeonFit.Gameplay;

namespace DungeonFit.UI;

public partial class RoomChallengeView : Control
{
    private readonly RoomRunService _roomService = new();
    private readonly DungeonRouteRules _routeRules = new();
    private readonly MusicCatalog _musicCatalog = new();
    private readonly EnemyCatalog _enemyCatalog = new();
    private readonly RoomPhaseController _phase = new();

    private const double PrepareSeconds = 3;

    public event Action<RunSummary, ExerciseHistoryEntry?>? RoomContinueRequested;
    public event Action<int>? ReturnToTownRequested;
    public event Func<int, SupplyUseResult>? SmallPotionRequested;

    private PlayerState _player = new();
    private TaskTemplate _task = null!;
    private int _stageNumber = 1;
    private int _totalStages = 1;
    private int _initialPlayerHp;
    private RunSummary? _lastSummary;
    private RoomSupplyViewModel _supply = new(0, 3, false);
    private ExerciseHistoryEntry? _lastExerciseHistory;
    private ExercisePersonalRecord? _exercisePersonalRecord;
    private RoomRun _room = null!;
    private WorkoutTimingProfile _timing = null!;
    private string _selectedMusicId = string.Empty;
    private double _prepareRemainingSeconds;
    private string _pendingWaveMessage = string.Empty;
    private double _restRemainingSeconds;
    private bool _isRestCountingDown;
    private bool _isPaused;
    private bool _restartMusicOnResume;
    private RoomAudioBridge _audioBridge = null!;
    private RoomResultPresenter _resultPresenter = null!;
    private BattleEncounterView _battleEncounter = null!;
    private Label _goldLabel = null!;
    private Label _challengeName = null!;
    private Label _roomName = null!;
    private Label _waveMarkers = null!;
    private Label _battleMessage = null!;
    private Label _beatSubtitle = null!;
    private Label _actionName = null!;
    private Label _setStatus = null!;
    private Label _restStatus = null!;
    private Label _timerLabel = null!;
    private Label _pauseMusicLabel = null!;
    private PanelContainer _restPanel = null!;
    private PanelContainer _reportPanel = null!;
    private PanelContainer _resultPanel = null!;
    private PanelContainer _pausePanel = null!;
    private PanelContainer _supplyPanel = null!;
    private Label _supplyStatus = null!;
    private Button _smallPotionButton = null!;
    private Button _reduceSetButton = null!;
    private Button _increaseSetButton = null!;
    private Button _reduceRepButton = null!;
    private Button _increaseRepButton = null!;
    private WaveIndicatorView _waveIndicator = null!;

    public override void _Ready()
    {
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.RoomBackground);
        _goldLabel = GetNode<Label>("%GoldLabel");
        _challengeName = GetNode<Label>("%ChallengeName");
        _roomName = GetNode<Label>("%RoomName");
        _timerLabel = GetNode<Label>("%TimerLabel");
        _waveMarkers = GetNode<Label>("%WaveMarkers");
        _battleMessage = GetNode<Label>("%BattleMessage");
        _beatSubtitle = GetNode<Label>("%BeatSubtitle");
        _actionName = GetNode<Label>("%ActionName");
        _setStatus = GetNode<Label>("%SetStatus");
        _restStatus = GetNode<Label>("%RestStatus");
        var enemyName = GetNode<Label>("%EnemyName");
        var bossHealth = GetNode<ProgressBar>("%BossHealth");
        var resultTitle = GetNode<Label>("%ResultTitle");
        var rewardSummary = GetNode<Label>("%RewardSummary");
        var resultContinueButton = GetNode<Button>("%ReturnTownButton");
        GetNode<Label>("ReportPanel/ReportMargin/ReportLayout/ReportTitle").Text = Text.ReportTitle;
        GetNode<Label>("ReportPanel/ReportMargin/ReportLayout/ReportHint").Text = Text.ReportHint;
        _restPanel = GetNode<PanelContainer>("%RestPanel");
        _reportPanel = GetNode<PanelContainer>("%ReportPanel");
        _resultPanel = GetNode<PanelContainer>("%ResultPanel");
        _supplyPanel = GetNode<PanelContainer>("%SupplyPanel");
        _supplyStatus = GetNode<Label>("%SupplyStatus");
        _smallPotionButton = GetNode<Button>("%SmallPotionButton");
        _reduceSetButton = GetNode<Button>("%ReduceSetButton");
        _increaseSetButton = GetNode<Button>("%IncreaseSetButton");
        _reduceRepButton = GetNode<Button>("%ReduceRepButton");
        _increaseRepButton = GetNode<Button>("%IncreaseRepButton");
        _waveIndicator = GetNode<WaveIndicatorView>("%WaveIndicator");
        _battleEncounter = new BattleEncounterView(
            GetNode<PanelContainer>("%PlayerToken"),
            GetNode<Label>("%PlayerLabel"),
            GetNode<PanelContainer>("%EnemyToken"),
            GetNode<Label>("%EnemyLabel"),
            enemyName,
            bossHealth);
        ApplyArtStyles(bossHealth);
        BuildPausePanel();

        var audioPlayer = new AudioStreamPlayer
        {
            Name = "WorkoutMusicPlayer",
        };
        AddChild(audioPlayer);
        _audioBridge = new RoomAudioBridge(audioPlayer, _musicCatalog);
        _resultPresenter = new RoomResultPresenter(_resultPanel, resultTitle, rewardSummary, resultContinueButton);
        _resultPresenter.ContinueRequested += RequestRoomExit;

        var completeButton = GetNode<Button>("%CompleteButton");
        completeButton.Text = Text.FinishSet;
        DungeonFitUi.ApplyButton(completeButton, UiButtonStyle.Primary);
        completeButton.Pressed += ReportSet;
        var partialButton = GetNode<Button>("%PartialButton");
        partialButton.Visible = false;
        partialButton.Disabled = true;
        var skipButton = GetNode<Button>("%SkipButton");
        skipButton.Text = Text.Withdraw;
        DungeonFitUi.ApplyButton(skipButton, UiButtonStyle.Danger);
        skipButton.Pressed += SkipRoom;
        var pauseButton = GetNode<Button>("%PauseButton");
        pauseButton.Text = Text.PauseButton;
        DungeonFitUi.ApplyButton(pauseButton, UiButtonStyle.Secondary);
        pauseButton.Pressed += PauseDungeon;
        var readyButton = GetNode<Button>("%ReadyNowButton");
        DungeonFitUi.ApplyButton(readyButton, UiButtonStyle.Primary);
        readyButton.Pressed += CompleteRestNow;
        var extendButton = GetNode<Button>("%ExtendRestButton");
        DungeonFitUi.ApplyButton(extendButton, UiButtonStyle.Secondary);
        extendButton.Pressed += ExtendRest;
        DungeonFitUi.ApplyButton(_reduceSetButton, UiButtonStyle.Secondary);
        DungeonFitUi.ApplyButton(_increaseSetButton, UiButtonStyle.Secondary);
        DungeonFitUi.ApplyButton(_reduceRepButton, UiButtonStyle.Secondary);
        DungeonFitUi.ApplyButton(_increaseRepButton, UiButtonStyle.Secondary);
        _reduceSetButton.Pressed += () => AdjustRoomTargets(-1, 0);
        _increaseSetButton.Pressed += () => AdjustRoomTargets(1, 0);
        _reduceRepButton.Pressed += () => AdjustRoomTargets(0, -1);
        _increaseRepButton.Pressed += () => AdjustRoomTargets(0, 1);
        DungeonFitUi.ApplyButton(_smallPotionButton, UiButtonStyle.Secondary);
        DungeonFitUi.ApplyIconTextContent(_smallPotionButton, UiThemePaths.RoomPotion, Text.UsePotion, 32, 22, vertical: false);
        _smallPotionButton.Pressed += UseSmallPotion;
        _waveIndicator.SetWaveCompleted += EnterBreak;
        _waveIndicator.WaveAttackAnticipated += TriggerWaveAttackWindup;
        _waveIndicator.WavePeakReached += TriggerWavePeakHit;

        if (_task is not null)
        {
            StartRoom();
        }
    }

    public override void _Process(double delta)
    {
        _audioBridge.Process(delta);

        if (_isPaused)
        {
            return;
        }

        if (_phase.IsPreparing)
        {
            _prepareRemainingSeconds = Math.Max(0, _prepareRemainingSeconds - delta);
            Refresh(_room.Progress);

            if (_prepareRemainingSeconds <= 0)
            {
                StartWave(_pendingWaveMessage);
            }

            return;
        }

        if (!_isRestCountingDown)
        {
            return;
        }

        _restRemainingSeconds = Math.Max(0, _restRemainingSeconds - delta);
        Refresh(_room.Progress);

        if (_restRemainingSeconds > 0)
        {
            return;
        }

        _isRestCountingDown = false;
        _phase.AwaitReport();
        _restPanel.Visible = false;
        _reportPanel.Visible = true;
        _battleMessage.Text = _room.Progress.IsBossWave
            ? Text.RestCompleteBoss
            : Text.RestComplete;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!_phase.IsResult)
        {
            return;
        }

        if (_resultPresenter.HandleInput(inputEvent))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public void Initialize(
        PlayerState player,
        TaskTemplate task,
        int stageNumber,
        int totalStages,
        int initialPlayerHp,
        RoomSupplyViewModel? supply = null,
        ExerciseHistoryEntry? lastExerciseHistory = null,
        ExercisePersonalRecord? exercisePersonalRecord = null)
    {
        _player = player;
        _task = task;
        _stageNumber = stageNumber;
        _totalStages = totalStages;
        _initialPlayerHp = initialPlayerHp;
        _supply = supply ?? new RoomSupplyViewModel(0, 3, false);
        _lastExerciseHistory = lastExerciseHistory;
        _exercisePersonalRecord = exercisePersonalRecord;

        if (IsNodeReady())
        {
            StartRoom();
        }
    }

    public bool SmokeOpenPauseMenu()
    {
        PauseDungeon();
        return _isPaused && _pausePanel.Visible;
    }

    public bool SmokeResumePauseMenu()
    {
        ResumeDungeon();
        return !_isPaused && !_pausePanel.Visible;
    }

    public bool SmokeIsPreparing()
    {
        return _phase.IsPreparing && _prepareRemainingSeconds > 0;
    }

    public string SmokeTimerText()
    {
        return _timerLabel.Text;
    }

    public bool SmokeShowEnemyVisual(string dungeonTypeId, int currentSet, int totalSets, bool isBossWave)
    {
        if (!IsNodeReady())
        {
            return false;
        }

        var enemy = _enemyCatalog.GetForDungeon(dungeonTypeId);
        _battleEncounter.SetEnemy(enemy, 1);
        _battleEncounter.ShowActiveWave(
            new RoomProgress(currentSet, totalSets, isBossWave, false, false),
            new ActiveSetCombatState(
                currentSet,
                isBossWave,
                _player.CurrentHp,
                _player.MaxHp,
                isBossWave ? enemy.GetBossMaxHp(1) : enemy.GetNormalMaxHp(1),
                isBossWave ? enemy.GetBossMaxHp(1) : enemy.GetNormalMaxHp(1),
                0,
                0,
                0,
                false,
                false));
        return true;
    }

    private void StartRoom()
    {
        var enemy = _enemyCatalog.GetForDungeon(_task.DungeonTypeId);
        _room = _roomService.Start(_task, _player.CombatStats, enemy, _initialPlayerHp);
        _selectedMusicId = _task.MusicId;
        RebuildTimingProfile();
        _battleEncounter.SetEnemy(enemy, _task.DungeonLevel);
        _isPaused = false;
        _restartMusicOnResume = false;
        _pausePanel.Visible = false;
        _restPanel.Visible = false;
        _reportPanel.Visible = false;
        _resultPresenter.Hide();
        StartPreparing(Text.WaveActive);
        Refresh(_room.Progress);
    }

    private void ApplyArtStyles(ProgressBar bossHealth)
    {
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/BattleStage"), UiPanelStyle.Battle);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/BeatFlow"), UiPanelStyle.Card);
        DungeonFitUi.ApplyPanel(_restPanel, UiPanelStyle.Card);
        DungeonFitUi.ApplyPanel(_supplyPanel, UiPanelStyle.Card);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/WorkoutStatus"), UiPanelStyle.Card);
        DungeonFitUi.ApplyPanel(_reportPanel, UiPanelStyle.Overlay);
        DungeonFitUi.ApplyPanel(_resultPanel, UiPanelStyle.Overlay);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("%PlayerToken"), UiPanelStyle.Token);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("%EnemyToken"), UiPanelStyle.Token);
        DungeonFitUi.ApplyProgressBar(bossHealth, new Color(0.78f, 0.2f, 0.28f));
    }

    private void BuildPausePanel()
    {
        _pausePanel = new PanelContainer
        {
            Name = "PausePanel",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _pausePanel.SetAnchorsPreset(LayoutPreset.FullRect);
        DungeonFitUi.ApplyPanel(_pausePanel, UiPanelStyle.Overlay);
        AddChild(_pausePanel);
        _pausePanel.MoveToFront();

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 58);
        margin.AddThemeConstantOverride("margin_top", 210);
        margin.AddThemeConstantOverride("margin_right", 58);
        margin.AddThemeConstantOverride("margin_bottom", 210);
        _pausePanel.AddChild(margin);

        var layout = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        layout.AddThemeConstantOverride("separation", 22);
        margin.AddChild(layout);

        var title = new Label
        {
            Text = Text.PauseTitle,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 58);
        layout.AddChild(title);

        var status = new Label
        {
            Text = Text.PauseDescription,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        status.AddThemeFontSizeOverride("font_size", 28);
        layout.AddChild(status);

        _pauseMusicLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _pauseMusicLabel.AddThemeFontSizeOverride("font_size", 26);
        layout.AddChild(_pauseMusicLabel);

        var musicRow = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 72),
        };
        musicRow.AddThemeConstantOverride("separation", 14);
        layout.AddChild(musicRow);

        var previousMusicButton = CreatePauseMenuButton(Text.PreviousMusic, UiButtonStyle.Secondary);
        previousMusicButton.Pressed += () => CycleMusic(-1);
        musicRow.AddChild(previousMusicButton);

        var nextMusicButton = CreatePauseMenuButton(Text.NextMusic, UiButtonStyle.Secondary);
        nextMusicButton.Pressed += () => CycleMusic(1);
        musicRow.AddChild(nextMusicButton);

        var resumeButton = CreatePauseMenuButton(Text.ResumeDungeon, UiButtonStyle.Primary);
        resumeButton.Pressed += ResumeDungeon;
        layout.AddChild(resumeButton);

        var returnButton = CreatePauseMenuButton(Text.ReturnTown, UiButtonStyle.Secondary);
        returnButton.Pressed += ReturnTownFromPause;
        layout.AddChild(returnButton);
    }

    private static Button CreatePauseMenuButton(string text, UiButtonStyle style)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 92),
        };
        button.AddThemeFontSizeOverride("font_size", 34);
        DungeonFitUi.ApplyButton(button, style);
        return button;
    }

    private void StartPreparing(string message)
    {
        _isPaused = false;
        _restartMusicOnResume = false;
        _pausePanel.Visible = false;
        _phase.StartPreparing();
        _prepareRemainingSeconds = PrepareSeconds;
        _pendingWaveMessage = message;
        _isRestCountingDown = false;
        _restPanel.Visible = false;
        _reportPanel.Visible = false;
        _waveIndicator.ShowRest();
        _audioBridge.StopImmediate();
        _beatSubtitle.Text = Text.Preparing;
        _battleMessage.Text = $"{message}\n{string.Format(Text.PreparingMessage, Math.Ceiling(_prepareRemainingSeconds))}";
        Refresh(_room.Progress);
    }

    private void StartWave(string message)
    {
        _isPaused = false;
        _pausePanel.Visible = false;
        _phase.StartWave();
        _isRestCountingDown = false;
        _restPanel.Visible = false;
        _reportPanel.Visible = false;
        _beatSubtitle.Text = _room.Progress.IsBossWave ? Text.BossBeatFlow : Text.BeatFlow;
        _battleMessage.Text = message;
        RebuildTimingProfile();
        _waveIndicator.Configure(_timing);
        var combatState = _roomService.BeginActiveSet(_room);
        _waveIndicator.StartSet();
        _audioBridge.StartActiveSet(_selectedMusicId, _timing.ActiveSetSeconds);
        _battleEncounter.ShowActiveWave(_room.Progress, combatState);
        Refresh(_room.Progress);
    }

    private void EnterBreak()
    {
        if (!_phase.TryEnterRest())
        {
            return;
        }

        _waveIndicator.ShowRest();
        _audioBridge.EnterRest();
        _restRemainingSeconds = _timing.RestSeconds;
        _isRestCountingDown = true;
        _restPanel.Visible = true;
        _reportPanel.Visible = false;
        _beatSubtitle.Text = Text.Rest;
        _battleMessage.Text = _room.Progress.IsBossWave
            ? Text.BossWaveEnded
            : Text.WaveEnded;
        _battleEncounter.ShowRest(_room.Progress, _room.ActiveCombatState);
        Refresh(_room.Progress);
    }

    private void TriggerWaveAttackWindup()
    {
        if (!_phase.IsActiveWave)
        {
            return;
        }

        _battleEncounter.ShowWaveAttackWindup(_room.Progress, _room.ActiveCombatState);
    }

    private void TriggerWavePeakHit()
    {
        if (!_phase.IsActiveWave)
        {
            return;
        }

        var repResult = _roomService.ResolveRepHit(_room);
        _battleEncounter.ShowWavePeakHit(_room.Progress, repResult, _room.ActiveCombatState);
    }

    private void ReportSet()
    {
        if (!_phase.CanReportSet())
        {
            return;
        }

        var combatResult = _roomService.ReportSet(_room);
        if (combatResult is null)
        {
            return;
        }

        var progress = _room.Progress;
        _isRestCountingDown = false;
        _phase.Clear();
        _restPanel.Visible = false;
        _reportPanel.Visible = false;
        _battleEncounter.ShowSetReported(progress, combatResult);

        if (progress.IsComplete)
        {
            FinishRoom(combatResult.EnemyDefeated && combatResult.IsBoss ? "Boss Cleared" : "Room Finished");
            return;
        }

        var message = BuildSetResultMessage(combatResult);
        StartPreparing(message);
        Refresh(progress);
    }

    private void SkipRoom()
    {
        _roomService.Skip(_room);
        FinishRoom("Room Withdrawn");
    }

    private void FinishRoom(string title)
    {
        var reward = _roomService.ResolveReward(_room);

        _isPaused = false;
        _pausePanel.Visible = false;
        _waveIndicator.StopSet();
        _audioBridge.StopForResult();
        _isRestCountingDown = false;
        _phase.ShowResult();
        _restPanel.Visible = false;
        _reportPanel.Visible = false;
        _lastSummary = new RunSummary(
            title,
            _task.RoomName,
            _room.Progress.CompletedSets,
            _room.TargetSets,
            reward,
            _room.SetResults.ToArray(),
            _room.CombatResults.ToArray(),
            _room.CurrentPlayerHp,
            TrainingExperienceRules.Calculate(_room.Progress.CompletedSets, _room.TargetSets, _room.CombatResults),
            TargetReps: _room.TargetReps,
            ExerciseId: _task.ExerciseId,
            DungeonTypeId: _task.DungeonTypeId);
        _battleEncounter.ShowResult(_room.Progress, _room.CombatResults.LastOrDefault());
        _resultPresenter.Show(_lastSummary, _task, _lastExerciseHistory, _exercisePersonalRecord);
        _supplyPanel.Visible = false;
        _battleMessage.Text = title == "Boss Cleared"
            ? Text.StageRewardBanked
            : Text.WithdrawRewardBanked;
        Refresh(_room.Progress);
    }

    private void Refresh(RoomProgress progress)
    {
        _roomName.Text = string.Format(Text.RoomNameFormat, _stageNumber, _totalStages, GetDungeonName(_task));
        _challengeName.Text = _task.ChallengeName;
        _actionName.Text = string.Format(Text.ActionFormat, _task.ActionName, _room.TargetSets, _room.TargetReps);
        _setStatus.Text = progress.IsComplete
            ? string.Format(Text.SetCompleteFormat, progress.TotalSets, progress.TotalSets)
            : progress.IsSkipped
                ? Text.WithdrawnStatus
                : _isRestCountingDown
                    ? string.Format(Text.SetRestFormat, progress.CurrentSet, progress.TotalSets, Math.Ceiling(_restRemainingSeconds))
                    : _phase.IsPreparing
                        ? string.Format(Text.SetPreparingFormat, progress.CurrentSet, progress.TotalSets)
                    : _phase.IsActiveWave
                        ? string.Format(Text.SetActiveFormat, progress.CurrentSet, progress.TotalSets)
                        : string.Format(Text.SetWaitingRestFormat, progress.CurrentSet, progress.TotalSets, _timing.RestSeconds);
        _restStatus.Text = _isRestCountingDown
            ? string.Format(Text.RestTimerFormat, FormatSeconds(_restRemainingSeconds))
            : _phase.IsPreparing
                ? string.Format(Text.PrepareTimerFormat, FormatSeconds(_prepareRemainingSeconds))
            : _phase.IsActiveWave
                ? string.Format(Text.ActiveWaveFormat, _timing.RepsPerMinute)
                : string.Format(Text.RestTimerFormat, FormatSeconds(_timing.RestSeconds));
        _timerLabel.Text = BuildTopTimerText();
        if (_isPaused)
        {
            _setStatus.Text = Text.PausedStatus;
            _restStatus.Text = Text.PausedStatus;
            _timerLabel.Text = Text.PausedStatus;
        }

        _waveMarkers.Text = BuildWaveMarkers(progress);
        _goldLabel.Text = string.Format(Text.GoldFormat, _player.Gold);
        GetNode<Label>("%NowPlaying").Text = string.Format(
            Text.NowPlayingFormat,
            _musicCatalog.GetById(_selectedMusicId).DisplayName,
            _timing.RepsPerMinute);
        RefreshRestAdjustmentButtons(progress);
        RefreshPauseMusicLabel();
        RefreshSupply();
    }

    private string BuildTopTimerText()
    {
        if (_phase.IsPreparing)
        {
            return string.Format(Text.TopPrepareTimerFormat, FormatSeconds(_prepareRemainingSeconds));
        }

        if (_isRestCountingDown)
        {
            return string.Format(Text.TopRestTimerFormat, FormatSeconds(_restRemainingSeconds));
        }

        if (_phase.IsActiveWave)
        {
            return string.Format(Text.TopActiveTimerFormat, FormatSeconds(_waveIndicator.RemainingSeconds));
        }

        if (_phase.IsAwaitingReport)
        {
            return Text.AwaitingReportTimer;
        }

        if (_phase.IsResult)
        {
            return Text.ResultTimer;
        }

        return Text.ReadyTimer;
    }

    private void RefreshRestAdjustmentButtons(RoomProgress progress)
    {
        var canAdjust = _isRestCountingDown;
        _reduceSetButton.Disabled = !canAdjust || _room.TargetSets <= progress.CurrentSet;
        _increaseSetButton.Disabled = !canAdjust || _room.TargetSets >= DungeonRouteRules.MaxSets;
        _reduceRepButton.Disabled = !canAdjust || _room.TargetReps <= DungeonRouteRules.MinReps;
        _increaseRepButton.Disabled = !canAdjust || _room.TargetReps >= DungeonRouteRules.MaxReps;
        _reduceSetButton.Text = Text.ReduceSet;
        _increaseSetButton.Text = Text.IncreaseSet;
        _reduceRepButton.Text = Text.ReduceRep;
        _increaseRepButton.Text = Text.IncreaseRep;
    }

    private void RefreshPauseMusicLabel()
    {
        if (_pauseMusicLabel is null)
        {
            return;
        }

        var track = _musicCatalog.GetById(_selectedMusicId);
        _pauseMusicLabel.Text = string.Format(Text.PauseMusicFormat, track.DisplayName);
    }

    private void AdjustRoomTargets(int setDelta, int repDelta)
    {
        if (!_isRestCountingDown)
        {
            return;
        }

        _room.AdjustTargets(_room.TargetSets + setDelta, _room.TargetReps + repDelta);
        RebuildTimingProfile();
        _battleMessage.Text = string.Format(Text.RoomTargetsAdjusted, _room.TargetSets, _room.TargetReps);
        Refresh(_room.Progress);
    }

    private void CycleMusic(int delta)
    {
        var tracks = _musicCatalog.GetAll();
        var currentIndex = 0;
        for (var index = 0; index < tracks.Count; index++)
        {
            if (tracks[index].Id == _selectedMusicId)
            {
                currentIndex = index;
                break;
            }
        }

        var nextIndex = (currentIndex + delta + tracks.Count) % tracks.Count;
        _selectedMusicId = tracks[nextIndex].Id;
        RebuildTimingProfile();
        if (_phase.IsActiveWave)
        {
            _audioBridge.StopImmediate();
            _restartMusicOnResume = _isPaused;
        }

        _battleMessage.Text = string.Format(Text.MusicChanged, tracks[nextIndex].DisplayName);
        Refresh(_room.Progress);
    }

    private void RebuildTimingProfile()
    {
        _timing = _routeRules.CreateTimingProfile(
            new DungeonRouteSlot(
                _task.DungeonTypeId,
                _room.TargetSets,
                _room.TargetReps,
                _selectedMusicId,
                _task.RestSeconds,
                _task.ExerciseId),
            _task.Bpm);
    }

    private void UseSmallPotion()
    {
        if (SmallPotionRequested is null)
        {
            return;
        }

        var result = SmallPotionRequested.Invoke(_room.CurrentPlayerHp);
        if (!result.Used)
        {
            _battleMessage.Text = Text.NoPotionUsed;
            _supply = result.Supply;
            RefreshSupply();
            return;
        }

        _roomService.HealPlayer(_room, result.Healed);
        _supply = result.Supply;
        _battleEncounter.RefreshActiveHealth(_room.Progress, _room.ActiveCombatState);
        _battleMessage.Text = string.Format(Text.PotionUsed, result.Healed, _room.CurrentPlayerHp);
        Refresh(_room.Progress);
    }

    private void RefreshSupply()
    {
        if (_supplyPanel is null)
        {
            return;
        }

        _supplyPanel.Visible = !_phase.IsResult;
        _supplyStatus.Text = string.Format(Text.SupplyStatus, _supply.SmallPotionCount, _supply.CarryLimit);
        _smallPotionButton.Disabled = !_supply.CanUseSmallPotion || _room.CurrentPlayerHp >= _player.MaxHp;
    }

    private static string BuildSetResultMessage(CombatSetResult result)
    {
        if (result.WasEvading)
        {
            return string.Format(Text.EvadedSet, result.PlayerHpAfter, result.Gold);
        }

        return result.EnemyDefeated
            ? string.Format(Text.EnemyDefeatedSet, result.DamageDealt, result.PlayerHpAfter, result.Gold)
            : string.Format(Text.EnemySurvivedSet, result.DamageDealt, result.DamageTaken, result.PlayerHpAfter, result.Gold);
    }

    private static string BuildWaveMarkers(RoomProgress progress)
    {
        var markers = new string[progress.TotalSets];

        for (var index = 0; index < markers.Length; index++)
        {
            markers[index] = index < progress.CompletedSets ? "[x]" : "[ ]";
        }

        return string.Join(" ", markers);
    }

    private void PauseDungeon()
    {
        if (_phase.IsResult || _isPaused)
        {
            return;
        }

        _isPaused = true;
        _waveIndicator.SetPaused(true);
        _audioBridge.SetPaused(true);
        _pausePanel.Visible = true;
        _pausePanel.MoveToFront();
        _battleMessage.Text = Text.PausedMessage;
        Refresh(_room.Progress);
    }

    private void ResumeDungeon()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        _waveIndicator.SetPaused(false);
        _audioBridge.SetPaused(false);
        if (_restartMusicOnResume && _phase.IsActiveWave)
        {
            _audioBridge.StartActiveSet(_selectedMusicId, _timing.ActiveSetSeconds);
            _restartMusicOnResume = false;
        }

        _pausePanel.Visible = false;
        _battleMessage.Text = Text.ResumedMessage;
        Refresh(_room.Progress);
    }

    private void ReturnTownFromPause()
    {
        _isPaused = false;
        _waveIndicator.StopSet();
        _audioBridge.StopImmediate();
        _pausePanel.Visible = false;
        ReturnToTownRequested?.Invoke(_room.CurrentPlayerHp);
    }

    private void CompleteRestNow()
    {
        if (!_isRestCountingDown)
        {
            return;
        }

        _restRemainingSeconds = 0;
        _isRestCountingDown = false;
        _phase.AwaitReport();
        _restPanel.Visible = false;
        _reportPanel.Visible = true;
        _battleMessage.Text = Text.ReadyNow;
        Refresh(_room.Progress);
    }

    private void ExtendRest()
    {
        if (!_isRestCountingDown)
        {
            return;
        }

        _restRemainingSeconds += 30;
        _battleMessage.Text = Text.RestExtended;
        Refresh(_room.Progress);
    }

    private void RequestRoomExit(RunSummary summary, ExerciseHistoryEntry? exerciseRecord)
    {
        _audioBridge.StopImmediate();
        RoomContinueRequested?.Invoke(summary, exerciseRecord);
    }

    private static string FormatSeconds(double seconds)
    {
        var clamped = Math.Max(0, (int)Math.Ceiling(seconds));
        return $"{clamped / 60:00}:{clamped % 60:00}";
    }

    private static string GetDungeonName(TaskTemplate task)
    {
        return task.DungeonTypeId switch
        {
            "chest" => Text.ChestDungeon,
            "shoulders" => Text.ShoulderDungeon,
            "back" => Text.BackDungeon,
            "legs" => Text.LegDungeon,
            "core" => Text.CoreDungeon,
            "arms" => Text.ArmDungeon,
            _ => task.DungeonTypeName,
        };
    }

    private static class Text
    {
        public const string RestCompleteBoss = "\u4f11\u606f\u7d50\u675f\u3002\u6309\u4e0b\u5b8c\u6210\u672c\u7d44\uff0c\u4ee5\u6230\u9b25\u6578\u503c\u7d50\u7b97 Boss\u3002";
        public const string RestComplete = "\u4f11\u606f\u7d50\u675f\u3002\u6309\u4e0b\u5b8c\u6210\u672c\u7d44\uff0c\u4ee5\u6230\u9b25\u6578\u503c\u7d50\u7b97\u9019\u7d44\u3002";
        public const string WaveActive = "Wave \u555f\u52d5\u3002\u8ddf\u8457\u7bc0\u594f\u5b8c\u6210\u9019\u7d44\uff0c\u76f4\u5230\u9032\u5165\u4f11\u606f\u3002";
        public const string BossBeatFlow = "Boss \u7bc0\u594f";
        public const string BeatFlow = "\u7bc0\u594f\u6307\u793a";
        public const string Preparing = "\u6e96\u5099";
        public const string PreparingMessage = "\u6e96\u5099\u958b\u59cb\uff1a{0:0}";
        public const string Rest = "\u4f11\u606f";
        public const string BossWaveEnded = "Boss Wave \u7d50\u675f\u3002\u4f11\u606f\u5012\u6578\u958b\u59cb\u3002";
        public const string WaveEnded = "Wave \u7d50\u675f\u3002\u4f11\u606f\u5012\u6578\u958b\u59cb\u3002";
        public const string PartialSet = "\u672c\u7d44\u5df2\u90e8\u5206\u5b8c\u6210\u3002\u4e0b\u4e00\u7d44\u6e96\u5099\u958b\u59cb\u3002";
        public const string SetCleared = "Wave \u5df2\u5b8c\u6210\u3002\u4e0b\u4e00\u500b\u6575\u4eba\u4e0a\u524d\u3002";
        public const string ReportTitle = "\u5b8c\u6210\u672c\u7d44";
        public const string ReportHint = "\u6309\u4e0b\u5f8c\u6703\u4f9d\u89d2\u8272\u8207\u6575\u4eba\u6578\u503c\u7d50\u7b97\u6536\u76ca\u3002";
        public const string FinishSet = "\u5b8c\u6210\u672c\u7d44";
        public const string Withdraw = "\u64a4\u9000";
        public const string EnemyDefeatedSet = "\u9020\u6210 {0} \u50b7\u5bb3\uff0c\u64ca\u7834\u6575\u4eba\u3002HP {1}\uff0c\u91d1\u5e63 +{2}\u3002";
        public const string EnemySurvivedSet = "\u9020\u6210 {0} \u50b7\u5bb3\uff0c\u6575\u4eba\u672a\u5012\u4e0b\u4e26\u53cd\u64ca {1}\u3002HP {2}\uff0c\u91d1\u5e63 +{3}\u3002";
        public const string EvadedSet = "HP \u4e0d\u8db3\uff0c\u89d2\u8272\u6539\u70ba\u8eb2\u907f\u3002HP {0}\uff0c\u91d1\u5e63 +{1}\u3002";
        public const string StageRewardBanked = "\u623f\u9593\u6536\u76ca\u5df2\u5b58\u5165\u4eca\u65e5\u7d50\u7b97\u3002";
        public const string WithdrawRewardBanked = "\u64a4\u9000\u6536\u76ca\u5df2\u5b58\u5165\u4eca\u65e5\u7d50\u7b97\u3002";
        public const string RoomNameFormat = "\u623f\u9593 {0} / {1}  -  {2}";
        public const string ActionFormat = "{0}  {1} \u7d44 x {2} \u6b21";
        public const string SetCompleteFormat = "\u7d44\u6578 {0} / {1}  \u4f11\u606f\u5b8c\u6210";
        public const string WithdrawnStatus = "\u5df2\u64a4\u9000\uff0c\u6311\u6230\u66ab\u505c";
        public const string SetRestFormat = "\u7d44\u6578 {0} / {1}  \u4f11\u606f {2:0}s";
        public const string SetPreparingFormat = "\u7d44\u6578 {0} / {1}  \u6e96\u5099\u4e2d";
        public const string SetActiveFormat = "\u7d44\u6578 {0} / {1}  Wave \u9032\u884c\u4e2d";
        public const string SetWaitingRestFormat = "\u7d44\u6578 {0} / {1}  \u4f11\u606f {2}s";
        public const string RestTimerFormat = "\u4f11\u606f  {0}";
        public const string PrepareTimerFormat = "\u6e96\u5099  {0}";
        public const string ActiveWaveFormat = "Wave \u9032\u884c\u4e2d  {0} BPM";
        public const string TopPrepareTimerFormat = "\u6e96\u5099 {0}";
        public const string TopActiveTimerFormat = "\u52d5\u4f5c {0}";
        public const string TopRestTimerFormat = "\u4f11\u606f {0}";
        public const string AwaitingReportTimer = "\u7b49\u5f85\u56de\u5831";
        public const string ResultTimer = "\u623f\u9593\u7d50\u7b97";
        public const string ReadyTimer = "\u5f85\u547d";
        public const string GoldFormat = "\u91d1\u5e63 {0}";
        public const string NowPlayingFormat = "\u64ad\u653e\u4e2d  {0}  /  Wave {1} BPM";
        public const string PauseToggled = "Wave \u66ab\u505c\u72c0\u614b\u5df2\u5207\u63db\u3002";
        public const string ReadyNow = "\u5df2\u6e96\u5099\u597d\u3002\u8acb\u56de\u5831\u9019\u7d44\u5b8c\u6210\u72c0\u614b\u3002";
        public const string RestExtended = "\u4f11\u606f\u5ef6\u9577 30 \u79d2\u3002";
        public const string ReduceSet = "\u7d44\u6578 -";
        public const string IncreaseSet = "\u7d44\u6578 +";
        public const string ReduceRep = "\u6b21\u6578 -";
        public const string IncreaseRep = "\u6b21\u6578 +";
        public const string RoomTargetsAdjusted = "\u672c\u623f\u9593\u8abf\u6574\u70ba {0} \u7d44 x {1} \u6b21\u3002";
        public const string PauseMusicFormat = "\u95dc\u5361\u97f3\u6a02\uff1a{0}";
        public const string PreviousMusic = "\u4e0a\u4e00\u9996";
        public const string NextMusic = "\u4e0b\u4e00\u9996";
        public const string MusicChanged = "\u97f3\u6a02\u5df2\u5207\u63db\uff1a{0}";
        public const string SupplyStatus = "\u5c0f\u578b\u85e5\u6c34 {0} / {1}";
        public const string UsePotion = "\u85e5\u6c34";
        public const string PotionUsed = "\u4f7f\u7528\u5c0f\u578b\u85e5\u6c34\uff0c\u6062\u5fa9 {0} HP\u3002\u76ee\u524d HP {1}\u3002";
        public const string NoPotionUsed = "\u76ee\u524d\u7121\u6cd5\u4f7f\u7528\u5c0f\u578b\u85e5\u6c34\u3002";
        public const string PauseButton = "\u2161";
        public const string PauseTitle = "\u66ab\u505c";
        public const string PauseDescription = "\u526f\u672c\u9032\u5ea6\u6703\u4fdd\u7559\u5728\u76ee\u524d\u623f\u9593\uff0c\u8fd4\u56de\u57ce\u93ae\u5f8c\u53ef\u518d\u6b21\u9032\u5165\u7e7c\u7e8c\u6311\u6230\u3002";
        public const string ResumeDungeon = "\u7e7c\u7e8c\u526f\u672c";
        public const string ReturnTown = "\u8fd4\u56de\u57ce\u93ae";
        public const string PausedStatus = "\u526f\u672c\u66ab\u505c\u4e2d";
        public const string PausedMessage = "\u526f\u672c\u5df2\u66ab\u505c\u3002";
        public const string ResumedMessage = "\u526f\u672c\u7e7c\u7e8c\u3002";
        public const string ChestDungeon = "\u80f8\u5730\u57ce";
        public const string ShoulderDungeon = "\u80a9\u5730\u57ce";
        public const string BackDungeon = "\u80cc\u5730\u57ce";
        public const string LegDungeon = "\u817f\u5730\u57ce";
        public const string CoreDungeon = "\u6838\u5fc3\u5730\u57ce";
        public const string ArmDungeon = "\u624b\u81c2\u5730\u57ce";
    }
}
