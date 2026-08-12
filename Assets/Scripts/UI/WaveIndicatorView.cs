using System;
using Godot;
using DungeonFit.Core.Models;

namespace DungeonFit.UI;

public partial class WaveIndicatorView : Control
{
    private const string BeatCorePath = "res://Assets/Art/Generated/RoomChallenge/MoonBeatFlow/processed/single-1.png";
    private const string CrystalPath = "res://Assets/Art/Generated/RoomChallenge/MoonBeatFlow/processed/single-2.png";
    private const string NotePath = "res://Assets/Art/Generated/RoomChallenge/MoonBeatFlow/processed/single-3.png";
    private const string SparklePath = "res://Assets/Art/Generated/RoomChallenge/MoonBeatFlow/processed/single-4.png";

    [Signal]
    public delegate void SetWaveCompletedEventHandler();

    [Signal]
    public delegate void WavePeakReachedEventHandler();

    [Signal]
    public delegate void WaveAttackAnticipatedEventHandler();

    private const float MarkerXRatio = 0.5f;
    private const double PeakPhaseRatio = 0.25;
    private const double AttackWindupSeconds = 0.28;
    private int _bpm;
    private int _beatsPerRep;
    private int _reps;
    private double _elapsedSeconds;
    private int _nextAttackAnticipationIndex;
    private int _nextPeakIndex;
    private bool _isPaused;
    private bool _isRunning;
    private WaveDisplayMode _displayMode = WaveDisplayMode.Rest;
    private Texture2D? _beatCore;
    private Texture2D? _crystal;
    private Texture2D? _note;
    private Texture2D? _sparkle;

    public bool IsRunning => _isRunning;

    public bool IsPaused => _isPaused;

    public int CompletedReps => _reps == 0
        ? 0
        : Math.Clamp((int)Math.Floor(_elapsedSeconds / SecondsPerRep), 0, _reps);

    public double RemainingSeconds => _isRunning
        ? Math.Max(0, SetDurationSeconds - _elapsedSeconds)
        : 0;

    private double SecondsPerRep => 60.0 / Math.Max(_bpm, 1) * Math.Max(_beatsPerRep, 1);

    private double SetDurationSeconds => SecondsPerRep * Math.Max(_reps, 1);

    public override void _Process(double delta)
    {
        if (_isRunning && !_isPaused)
        {
            var previousElapsedSeconds = _elapsedSeconds;
            _elapsedSeconds += delta;
            EmitPassedAttackAnticipations(previousElapsedSeconds, _elapsedSeconds);
            EmitPassedWavePeaks(previousElapsedSeconds, _elapsedSeconds);

            if (_elapsedSeconds >= SetDurationSeconds)
            {
                _elapsedSeconds = SetDurationSeconds;
                _isRunning = false;
                EmitSignal(SignalName.SetWaveCompleted);
            }
        }

        QueueRedraw();
    }

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        _beatCore = GD.Load<Texture2D>(BeatCorePath);
        _crystal = GD.Load<Texture2D>(CrystalPath);
        _note = GD.Load<Texture2D>(NotePath);
        _sparkle = GD.Load<Texture2D>(SparklePath);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var size = GetSize();
        var graphRect = new Rect2(new Vector2(0, 0), size);
        DrawRect(graphRect, new Color(0.025f, 0.016f, 0.09f, 0.9f), true);
        DrawRect(graphRect.Grow(-2), new Color(0.54f, 0.24f, 0.9f, 0.75f), false, 3);
        DrawRect(graphRect.Grow(-8), new Color(0.22f, 0.08f, 0.42f, 0.35f), false, 1);

        DrawTargetWave(size);
        DrawRhythmMarker(size);
    }

    public void Configure(int reps, int bpm, int beatsPerRep)
    {
        _reps = Math.Max(reps, 1);
        _bpm = Math.Max(bpm, 1);
        _beatsPerRep = Math.Max(beatsPerRep, 1);
        QueueRedraw();
    }

    public void Configure(WorkoutTimingProfile timing)
    {
        Configure(timing.TargetReps, timing.Bpm, timing.BeatsPerRep);
    }

    public void StartSet()
    {
        _elapsedSeconds = 0;
        _nextAttackAnticipationIndex = 0;
        _nextPeakIndex = 0;
        _isPaused = false;
        _isRunning = true;
        _displayMode = WaveDisplayMode.Active;
        SetProcess(true);
        QueueRedraw();
    }

    public void ShowRest()
    {
        _isRunning = false;
        _isPaused = false;
        _displayMode = WaveDisplayMode.Rest;
        QueueRedraw();
    }

    public void StopSet()
    {
        _isRunning = false;
        _isPaused = false;
        _displayMode = WaveDisplayMode.Rest;
        QueueRedraw();
    }

    public void TogglePause()
    {
        if (!_isRunning)
        {
            return;
        }

        _isPaused = !_isPaused;

        QueueRedraw();
    }

    public void SetPaused(bool isPaused)
    {
        if (!_isRunning)
        {
            _isPaused = false;
            QueueRedraw();
            return;
        }

        _isPaused = isPaused;
        QueueRedraw();
    }

    private void DrawTargetWave(Vector2 size)
    {
        var points = new Vector2[180];
        var markerX = size.X * MarkerXRatio;
        var centerY = size.Y * 0.56f;
        var amplitude = size.Y * 0.27f;
        var visibleCycles = 3.6f;
        var cycleOffset = _elapsedSeconds / SecondsPerRep;

        for (var index = 0; index < points.Length; index++)
        {
            var ratio = index / (float)(points.Length - 1);
            var x = ratio * size.X;
            var phase = ((x - markerX) / size.X * visibleCycles) + cycleOffset;
            var saw = Mathf.PosMod((float)phase, 1f);
            var pulse = saw < 0.5f
                ? (saw * 4f) - 1f
                : 3f - (saw * 4f);
            var y = centerY - pulse * amplitude;
            points[index] = new Vector2(x, y);
        }

        DrawSpectrumSpikes(size, points);
        DrawDecorativeEffects(size, centerY);
        DrawPolyline(points, new Color(0.55f, 0.08f, 0.95f, 0.2f), 24, true);
        DrawPolyline(points, new Color(0.8f, 0.18f, 1, 0.55f), 12, true);
        DrawPolyline(points, new Color(1, 0.58f, 1, 1), 5, true);
    }

    private void DrawRhythmMarker(Vector2 size)
    {
        var marker = new Vector2(size.X * MarkerXRatio, size.Y * GetTargetWaveY());
        var pulse = _isRunning
            ? 1f + 0.1f * Mathf.Sin((float)(_elapsedSeconds * Math.Tau / SecondsPerRep))
            : 1f;
        var coreSize = 108f * pulse;
        DrawCircle(marker, 49f * pulse, new Color(0.56f, 0.12f, 0.95f, 0.18f));
        DrawArc(marker, 47f * pulse, 0, Mathf.Tau, 48, new Color(0.75f, 0.38f, 1, 0.58f), 2, true);
        DrawLine(new Vector2(marker.X, 18), new Vector2(marker.X, size.Y - 12), new Color(0.74f, 0.38f, 1, 0.28f), 2);
        DrawEffect(_beatCore, new Rect2(marker - new Vector2(coreSize * 0.5f, coreSize * 0.5f), new Vector2(coreSize, coreSize)), Colors.White);
    }

    private void DrawSpectrumSpikes(Vector2 size, Vector2[] points)
    {
        var centerY = size.Y * 0.56f;
        var spikeRatios = new[] { 0.18f, 0.34f, 0.68f, 0.84f };

        foreach (var ratio in spikeRatios)
        {
            var pointIndex = Mathf.Clamp((int)Mathf.Round(ratio * (points.Length - 1)), 0, points.Length - 1);
            var point = points[pointIndex];
            var height = size.Y * (ratio is 0.34f or 0.84f ? 0.46f : 0.32f);
            DrawLine(new Vector2(point.X, centerY - height), new Vector2(point.X, centerY + height), new Color(0.63f, 0.15f, 1, 0.22f), 10);
            DrawLine(new Vector2(point.X, centerY - height * 0.72f), new Vector2(point.X, centerY + height * 0.72f), new Color(0.93f, 0.44f, 1, 0.72f), 4);
        }
    }

    private void DrawDecorativeEffects(Vector2 size, float centerY)
    {
        var beatPhase = (float)(_elapsedSeconds * Math.Tau / SecondsPerRep);
        var crystalSize = size.Y * 1.08f;
        var leftFloat = GetFloatOffset(beatPhase, 0.2f, 7f);
        var rightFloat = GetFloatOffset(beatPhase, 1.8f, 7f);
        DrawEffect(_crystal, new Rect2(size.X * 0.18f - crystalSize * 0.5f, centerY - crystalSize * 0.5f + leftFloat, crystalSize, crystalSize), new Color(1, 1, 1, 0.76f));
        DrawEffect(_crystal, new Rect2(size.X * 0.84f - crystalSize * 0.5f, centerY - crystalSize * 0.5f + rightFloat, crystalSize, crystalSize), new Color(1, 1, 1, 0.76f));

        var noteSize = size.Y * 0.42f;
        var noteFloat = GetFloatOffset(beatPhase, 2.6f, 6f);
        DrawEffect(_note, new Rect2(size.X * 0.67f, size.Y * 0.04f + noteFloat, noteSize, noteSize), new Color(1, 1, 1, 0.9f));

        var sparkleSize = size.Y * 0.34f;
        var leftSparkleFloat = GetFloatOffset(beatPhase, 3.7f, 4f);
        var rightSparkleFloat = GetFloatOffset(beatPhase, 5f, 4f);
        DrawEffect(_sparkle, new Rect2(size.X * 0.06f, size.Y * 0.12f + leftSparkleFloat, sparkleSize, sparkleSize), new Color(1, 1, 1, 0.75f));
        DrawEffect(_sparkle, new Rect2(size.X * 0.9f, size.Y * 0.08f + rightSparkleFloat, sparkleSize * 0.72f, sparkleSize * 0.72f), new Color(1, 1, 1, 0.7f));
    }

    private float GetFloatOffset(float beatPhase, float phaseOffset, float amplitude)
    {
        var restMultiplier = _isRunning ? 1f : 0.35f;
        return Mathf.Sin(beatPhase + phaseOffset) * amplitude * restMultiplier;
    }

    private void DrawEffect(Texture2D? texture, Rect2 rect, Color modulate)
    {
        if (texture is not null)
        {
            DrawTextureRect(texture, rect, false, modulate);
        }
    }

    private float GetTargetWaveY()
    {
        var centerY = 0.56f;
        var amplitude = 0.27f;
        var phase = _elapsedSeconds / SecondsPerRep;
        var saw = Mathf.PosMod((float)phase, 1f);
        var pulse = saw < 0.5f
            ? (saw * 4f) - 1f
            : 3f - (saw * 4f);
        return centerY - pulse * amplitude;
    }

    private void EmitPassedWavePeaks(double previousElapsedSeconds, double currentElapsedSeconds)
    {
        while (_nextPeakIndex < _reps)
        {
            var peakTime = ((_nextPeakIndex + PeakPhaseRatio) * SecondsPerRep);
            if (peakTime > currentElapsedSeconds)
            {
                return;
            }

            if (peakTime > previousElapsedSeconds)
            {
                EmitSignal(SignalName.WavePeakReached);
            }

            _nextPeakIndex++;
        }
    }

    private void EmitPassedAttackAnticipations(double previousElapsedSeconds, double currentElapsedSeconds)
    {
        while (_nextAttackAnticipationIndex < _reps)
        {
            var repStartTime = _nextAttackAnticipationIndex * SecondsPerRep;
            var peakTime = ((_nextAttackAnticipationIndex + PeakPhaseRatio) * SecondsPerRep);
            var anticipationTime = Math.Max(repStartTime, peakTime - AttackWindupSeconds);

            if (anticipationTime > currentElapsedSeconds)
            {
                return;
            }

            if (anticipationTime >= previousElapsedSeconds)
            {
                EmitSignal(SignalName.WaveAttackAnticipated);
            }

            _nextAttackAnticipationIndex++;
        }
    }

    private enum WaveDisplayMode
    {
        Rest,
        Active,
    }
}
