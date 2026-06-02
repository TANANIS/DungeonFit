using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using Godot;

namespace DungeonFit.UI;

public sealed class RoomAudioBridge
{
    private readonly MusicCatalog _musicCatalog;
    private readonly WorkoutMusicPlayer _musicPlayer;

    public RoomAudioBridge(AudioStreamPlayer audioPlayer, MusicCatalog musicCatalog)
    {
        _musicCatalog = musicCatalog;
        _musicPlayer = new WorkoutMusicPlayer(audioPlayer);
    }

    public void Process(double delta)
    {
        _musicPlayer.Process(delta);
    }

    public void StartActiveSet(string musicId, double activeSetSeconds)
    {
        _musicPlayer.StartSet(_musicCatalog.GetById(musicId), activeSetSeconds);
    }

    public void EnterRest()
    {
        _musicPlayer.EnterRest();
    }

    public void StopForResult()
    {
        _musicPlayer.StopWithFade();
    }

    public void StopImmediate()
    {
        _musicPlayer.StopImmediate();
    }

    public void TogglePause()
    {
        _musicPlayer.TogglePause();
    }

    public void SetPaused(bool isPaused)
    {
        _musicPlayer.SetPaused(isPaused);
    }
}
