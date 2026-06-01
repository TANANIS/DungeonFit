using System;
using System.Text.Json;
using Godot;

namespace DungeonFit.Gameplay;

public sealed class SaveService
{
    private const string SavePath = "user://save.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = false,
    };

    public SaveLoadResult Load()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            return SaveLoadResult.Missing();
        }

        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            var state = JsonSerializer.Deserialize<SaveGameState>(json, JsonOptions);
            return state is null
                ? SaveLoadResult.Corrupted("Save file was empty or unreadable.")
                : SaveLoadResult.Loaded(state);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Failed to load save: {exception.Message}");
            return SaveLoadResult.Corrupted(exception.Message);
        }
    }

    public void Save(SaveGameState state)
    {
        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            file.StoreString(JsonSerializer.Serialize(state, JsonOptions));
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Failed to save game: {exception.Message}");
        }
    }

    public bool HasSave()
    {
        return FileAccess.FileExists(SavePath);
    }

    public void Delete()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            return;
        }

        var absolutePath = ProjectSettings.GlobalizePath(SavePath);
        var error = DirAccess.RemoveAbsolute(absolutePath);

        if (error != Error.Ok)
        {
            GD.PushWarning($"Failed to delete save: {error}");
        }
    }
}
