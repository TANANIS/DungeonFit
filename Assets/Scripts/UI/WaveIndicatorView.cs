using System;
using Godot;
using DungeonFit.Core.Models;

namespace DungeonFit.UI;

public partial class WaveIndicatorView : Control
{
    [Signal]
    public delegate void SetWaveCompletedEventHandler();

    [Signal]
    public delegate void WavePeakReachedEventHandler();

    [Signal]
    public delegate void WaveAttackAnticipatedEventHandler();

    private const float MarkerXRatio = 0.18f;
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

    public bool IsRunning => _isRunning;

    public bool IsPaused => _isPaused;

    public int CompletedReps => _reps == 0
        ? 0
        : Math.Clamp((int)Math.Floor(_elapsedSeconds / SecondsPerRep), 0, _reps);

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

    public override void _Draw()
    {
        var size = GetSize();
        var graphRect = new Rect2(new Vector2(0, 0), size);
        DrawRect(graphRect, new Color(0.03f, 0.02f, 0.11f, 0.92f), true);
        DrawRect(graphRect.Grow(-3), new Color(0.48f, 0.2f, 0.82f, 0.95f), false, 4);

        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(24, 38),
            GetHeaderText(),
            HorizontalAlignment.Left,
            -1,
            24,
            new Color(0.93f, 0.82f, 1));

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
        var points = new Vector2[160];
        var markerX = size.X * MarkerXRatio;
        var centerY = size.Y * 0.56f;
        var amplitude = size.Y * 0.25f;
        var visibleCycles = 3.35f;
        var cycleOffset = _elapsedSeconds / SecondsPerRep;

        for (var index = 0; index < points.Length; index++)
        {
            var ratio = index / (float)(points.Length - 1);
            var x = ratio * size.X;
            var phase = ((x - markerX) / size.X * visibleCycles) + cycleOffset;
            var y = centerY - Mathf.Sin((float)(phase * Math.Tau)) * amplitude;
            points[index] = new Vector2(x, y);
        }

        DrawPolyline(points, new Color(0.96f, 0.42f, 1), 7, true);
    }

    private void DrawRhythmMarker(Vector2 size)
    {
        var marker = new Vector2(size.X * MarkerXRatio, size.Y * GetTargetWaveY());
        DrawCircle(marker, 22, new Color(0.99f, 0.38f, 0.86f));
        DrawArc(marker, 34, 0, Mathf.Tau, 40, new Color(1, 0.75f, 0.96f), 4, true);
        DrawLine(new Vector2(marker.X, 52), new Vector2(marker.X, size.Y - 18), new Color(0.55f, 0.3f, 0.88f, 0.48f), 3);
    }

    private string GetHeaderText()
    {
        if (_displayMode == WaveDisplayMode.Rest)
        {
            return "Break / Rest";
        }

        if (_isPaused)
        {
            return "Paused";
        }

        return $"Rhythm Guide  Rep {Math.Min(CompletedReps + 1, _reps)} / {_reps}";
    }

    private float GetTargetWaveY()
    {
        var centerY = 0.56f;
        var amplitude = 0.25f;
        var phase = _elapsedSeconds / SecondsPerRep;
        return centerY - Mathf.Sin((float)(phase * Math.Tau)) * amplitude;
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
