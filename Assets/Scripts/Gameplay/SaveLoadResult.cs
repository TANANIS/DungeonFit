namespace DungeonFit.Gameplay;

public enum SaveLoadStatus
{
    Missing,
    Loaded,
    Corrupted,
}

public sealed record SaveLoadResult(
    SaveLoadStatus Status,
    SaveGameState? State,
    string? Message)
{
    public static SaveLoadResult Missing() => new(SaveLoadStatus.Missing, null, null);

    public static SaveLoadResult Loaded(SaveGameState state) => new(SaveLoadStatus.Loaded, state, null);

    public static SaveLoadResult Corrupted(string message) => new(SaveLoadStatus.Corrupted, null, message);
}
