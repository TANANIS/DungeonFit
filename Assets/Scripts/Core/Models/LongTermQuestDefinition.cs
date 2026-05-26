using System.Collections.Generic;

namespace DungeonFit.Core.Models;

public sealed record LongTermQuestDefinition(
    string Id,
    string Title,
    string Requester,
    string Description,
    IReadOnlyList<string> DialogueLines,
    LongTermQuestObjectiveType ObjectiveType,
    string TargetDungeonTypeId,
    int RequiredAmount,
    int RewardGold,
    string RewardTitle,
    string IconType);
