using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class MusicCatalog
{
    private const string MusicRoot = "res://Assets/Audio/Music/";

    private readonly MusicTrack[] _tracks =
    {
        new("chest_quest_01", "Chest Quest 01 (140 BPM)", 140, MusicRoot + "chest_quest_01_140bpm.mp3"),
        new("chest_quest_02", "Chest Quest 02 (140 BPM)", 140, MusicRoot + "chest_quest_02_140bpm.mp3"),
        new("iron_chest_run_01", "Iron Chest Run 01 (140 BPM)", 140, MusicRoot + "iron_chest_run_01_140bpm.mp3"),
        new("iron_chest_run_02", "Iron Chest Run 02 (140 BPM)", 140, MusicRoot + "iron_chest_run_02_140bpm.mp3"),
        new("plate_press_ritual_01", "Plate Press Ritual 01 (144 BPM)", 144, MusicRoot + "plate_press_ritual_01_144bpm.mp3"),
        new("plate_press_ritual_02", "Plate Press Ritual 02 (144 BPM)", 144, MusicRoot + "plate_press_ritual_02_144bpm.mp3"),
        new("press_mode_01", "Press Mode 01 (148 BPM)", 148, MusicRoot + "press_mode_01_148bpm.mp3"),
        new("press_mode_02", "Press Mode 02 (148 BPM)", 148, MusicRoot + "press_mode_02_148bpm.mp3"),
        new("iron_delts_01", "Iron Delts 01 (150 BPM)", 150, MusicRoot + "iron_delts_01_150bpm.mp3"),
        new("iron_delts_02", "Iron Delts 02 (150 BPM)", 150, MusicRoot + "iron_delts_02_150bpm.mp3"),
        new("pixel_pump_loop_01", "Pixel Pump Loop 01 (152 BPM)", 152, MusicRoot + "pixel_pump_loop_01_152bpm.mp3"),
        new("pixel_pump_loop_02", "Pixel Pump Loop 02 (152 BPM)", 152, MusicRoot + "pixel_pump_loop_02_152bpm.mp3"),
        new("iron_curl_loop_01", "Iron Curl Loop 01 (154 BPM)", 154, MusicRoot + "iron_curl_loop_01_154bpm.mp3"),
        new("iron_curl_loop_02", "Iron Curl Loop 02 (154 BPM)", 154, MusicRoot + "iron_curl_loop_02_154bpm.mp3"),
        new("iron_chest_gauntlet_01", "Iron Chest Gauntlet 01 (155 BPM)", 155, MusicRoot + "iron_chest_gauntlet_01_155bpm.mp3"),
        new("iron_chest_gauntlet_02", "Iron Chest Gauntlet 02 (155 BPM)", 155, MusicRoot + "iron_chest_gauntlet_02_155bpm.mp3"),
    };

    public IReadOnlyList<MusicTrack> GetAll()
    {
        return _tracks;
    }

    public MusicTrack GetById(string id)
    {
        return _tracks.FirstOrDefault(track => track.Id == id) ?? _tracks[0];
    }

    public MusicTrack GetByDisplayName(string displayName)
    {
        return _tracks.FirstOrDefault(track => track.DisplayName == displayName) ?? _tracks[0];
    }

    public string ResolveId(string idOrDisplayName)
    {
        var track = _tracks.FirstOrDefault(track => track.Id == idOrDisplayName)
            ?? _tracks.FirstOrDefault(track => track.DisplayName == idOrDisplayName)
            ?? _tracks[0];
        return track.Id;
    }
}
