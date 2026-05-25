namespace DungeonFit.Core.Models;

public sealed class ActiveShortTermQuest
{
    public string QuestId { get; set; } = string.Empty;

    public int Progress { get; set; }

    public bool IsClaimed { get; set; }
}
