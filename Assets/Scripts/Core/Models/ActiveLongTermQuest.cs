using System;

namespace DungeonFit.Core.Models;

public sealed class ActiveLongTermQuest
{
    public string QuestId { get; set; } = string.Empty;

    public int Progress { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; }

    public bool IsClaimed { get; set; }
}
