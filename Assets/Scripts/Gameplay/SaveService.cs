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

    public SaveGameState? Load()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            return null;
        }

        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            return JsonSerializer.Deserialize<SaveGameState>(json, JsonOptions);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Failed to load save: {exception.Message}");
            return null;
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
