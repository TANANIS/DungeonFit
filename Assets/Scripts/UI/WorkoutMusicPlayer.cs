using System;
using DungeonFit.Core.Models;
using Godot;

namespace DungeonFit.UI;

public sealed class WorkoutMusicPlayer
{
    private const float SilentVolumeDb = -40f;
    private const float ActiveVolumeDb = -6f;
    private const float RestVolumeDb = -18f;
    private const double FadeInSeconds = 1.2;
    private const double DuckFadeSeconds = 0.8;
    private const double SegmentPaddingSeconds = 8;
    private const double MinimumSegmentSeconds = 20;
    private const double MaximumSegmentSeconds = 60;
    private const double DefaultUsableEndPaddingSeconds = 8;

    private readonly AudioStreamPlayer _player;
    private readonly Random _random = new();

    private string _currentTrackId = "";
    private double _segmentStartSeconds;
    private double _segmentEndSeconds;
    private double _fadeElapsedSeconds;
    private double _fadeDurationSeconds;
    private float _fadeFromDb;
    private float _fadeToDb;
    private FadeMode _fadeMode = FadeMode.None;

    public WorkoutMusicPlayer(AudioStreamPlayer player)
    {
        _player = player;
        _player.VolumeDb = SilentVolumeDb;
    }

    public void Process(double delta)
    {
        if (_player.Playing && !_player.StreamPaused && _segmentEndSeconds > _segmentStartSeconds)
        {
            if (_player.GetPlaybackPosition() >= _segmentEndSeconds)
            {
                _player.Play((float)_segmentStartSeconds);
            }
        }

        if (_fadeMode == FadeMode.None)
        {
            return;
        }

        _fadeElapsedSeconds = Math.Min(_fadeElapsedSeconds + delta, _fadeDurationSeconds);
        var ratio = _fadeDurationSeconds <= 0 ? 1 : _fadeElapsedSeconds / _fadeDurationSeconds;
        _player.VolumeDb = Mathf.Lerp(_fadeFromDb, _fadeToDb, (float)ratio);

        if (_fadeElapsedSeconds < _fadeDurationSeconds)
        {
            return;
        }

        if (_fadeMode == FadeMode.Out)
        {
            _player.Stop();
            _player.StreamPaused = false;
            _player.VolumeDb = SilentVolumeDb;
        }

        _fadeMode = FadeMode.None;
    }

    public void StartSet(MusicTrack track, double activeSetSeconds)
    {
        if (string.IsNullOrWhiteSpace(track.ResourcePath))
        {
            StopImmediate();
            return;
        }

        var stream = GD.Load<AudioStream>(track.ResourcePath);
        if (stream is null)
        {
            GD.PushWarning($"Music stream not found: {track.ResourcePath}");
            StopImmediate();
            return;
        }

        var length = Math.Max(0, stream.GetLength());
        var range = ResolvePlayableRange(track, length);
        var segmentLength = CalculateSegmentLength(activeSetSeconds, range.EndSeconds - range.StartSeconds);
        var maxStart = Math.Max(range.StartSeconds, range.EndSeconds - segmentLength);
        _segmentStartSeconds = maxStart <= range.StartSeconds
            ? range.StartSeconds
            : range.StartSeconds + (_random.NextDouble() * (maxStart - range.StartSeconds));
        _segmentEndSeconds = Math.Min(range.EndSeconds, _segmentStartSeconds + segmentLength);

        if (_currentTrackId != track.Id || _player.Stream != stream || !_player.Playing)
        {
            _currentTrackId = track.Id;
            _player.Stream = stream;
            _player.StreamPaused = false;
            _player.VolumeDb = SilentVolumeDb;
            _player.Play((float)_segmentStartSeconds);
        }
        else
        {
            _player.StreamPaused = false;
        }

        StartFade(ActiveVolumeDb + track.VolumeOffsetDb, FadeInSeconds, FadeMode.In);
    }

    public void EnterRest()
    {
        if (!_player.Playing)
        {
            return;
        }

        StartFade(RestVolumeDb, DuckFadeSeconds, FadeMode.In);
    }

    public void StopWithFade()
    {
        if (!_player.Playing)
        {
            return;
        }

        StartFade(SilentVolumeDb, DuckFadeSeconds, FadeMode.Out);
    }

    public void StopImmediate()
    {
        _fadeMode = FadeMode.None;
        _currentTrackId = "";
        _player.Stop();
        _player.StreamPaused = false;
        _player.VolumeDb = SilentVolumeDb;
    }

    public void TogglePause()
    {
        if (_player.Playing)
        {
            _player.StreamPaused = !_player.StreamPaused;
        }
    }

    public void SetPaused(bool isPaused)
    {
        if (_player.Playing)
        {
            _player.StreamPaused = isPaused;
        }
    }

    private void StartFade(float targetDb, double durationSeconds, FadeMode mode)
    {
        _fadeMode = mode;
        _fadeElapsedSeconds = 0;
        _fadeDurationSeconds = Math.Max(0.01, durationSeconds);
        _fadeFromDb = _player.VolumeDb;
        _fadeToDb = targetDb;
    }

    private static double CalculateSegmentLength(double activeSetSeconds, double streamLengthSeconds)
    {
        var requested = Math.Clamp(
            activeSetSeconds + SegmentPaddingSeconds,
            MinimumSegmentSeconds,
            MaximumSegmentSeconds);

        return streamLengthSeconds <= 0
            ? requested
            : Math.Min(requested, streamLengthSeconds);
    }

    private static PlayableRange ResolvePlayableRange(MusicTrack track, double streamLengthSeconds)
    {
        if (streamLengthSeconds <= 0)
        {
            return new PlayableRange(0, 0);
        }

        var usableStart = Math.Clamp(
            Math.Max(track.UsableStartSeconds, track.BeatOffsetSeconds),
            0,
            streamLengthSeconds);
        var fallbackEnd = Math.Max(usableStart, streamLengthSeconds - DefaultUsableEndPaddingSeconds);
        var usableEnd = track.UsableEndSeconds > usableStart
            ? Math.Min(track.UsableEndSeconds, streamLengthSeconds)
            : fallbackEnd;

        var loopStart = track.LoopStartSeconds > 0 ? Math.Clamp(track.LoopStartSeconds, usableStart, usableEnd) : usableStart;
        var loopEnd = track.LoopEndSeconds > loopStart ? Math.Clamp(track.LoopEndSeconds, loopStart, usableEnd) : usableEnd;

        return loopEnd > loopStart
            ? new PlayableRange(loopStart, loopEnd)
            : new PlayableRange(0, streamLengthSeconds);
    }

    private enum FadeMode
    {
        None,
        In,
        Out,
    }

    private readonly record struct PlayableRange(double StartSeconds, double EndSeconds);
}
