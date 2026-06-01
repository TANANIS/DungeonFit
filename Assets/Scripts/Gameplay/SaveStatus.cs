namespace DungeonFit.Gameplay;

public sealed record SaveStatus(
    bool HasSaveFile,
    int Gold,
    int RouteSlotCount,
    int CompletedStageCount,
    int BankedRewardCount,
    int BankedChestCount,
    bool DailyRewardsClaimed,
    string? WarningMessage = null)
{
    public string SummaryText =>
        !string.IsNullOrWhiteSpace(WarningMessage)
            ? WarningMessage
            : HasSaveFile
            ? $"Gold {Gold} / Route {RouteSlotCount} / Completed {CompletedStageCount} / Rewards {BankedRewardCount} / Chests {BankedChestCount}"
            : "No save file";
}
