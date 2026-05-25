namespace DungeonFit.Core.Models;

public sealed record DungeonChest(
    string Id,
    string Tier,
    string SourceStageId,
    string SourceDungeonTypeId,
    string InstanceIdPrefix,
    CompletionResult Result,
    int SetNumber);
