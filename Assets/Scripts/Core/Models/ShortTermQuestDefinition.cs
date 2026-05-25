namespace DungeonFit.Core.Models;

public sealed record ShortTermQuestDefinition(
    string Id,
    string Title,
    string NpcName,
    string Description,
    string RequirementText,
    string TargetDungeonTypeId,
    int RequiredAmount,
    int RewardGold,
    string IconType);
