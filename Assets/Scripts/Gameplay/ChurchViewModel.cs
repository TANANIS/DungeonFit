using System;
using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;

namespace DungeonFit.Gameplay;

public sealed class ChurchViewModel
{
    private readonly LongTermQuestCatalog _catalog = new();

    public ChurchViewModel(
        PlayerState player,
        ActiveLongTermQuest? activeQuest,
        IReadOnlyCollection<string> claimedQuestIds,
        IReadOnlyCollection<string> unlockedTitles,
        string? selectedQuestId)
    {
        Player = new ChurchCharacterSummary(
            player.Level,
            player.Experience,
            player.ExperienceToNextLevel,
            player.Gold);
        ActiveQuest = activeQuest;
        ClaimedQuestIds = claimedQuestIds.ToArray();
        UnlockedTitles = unlockedTitles.ToArray();
        Cards = BuildCards(activeQuest, claimedQuestIds);
        SelectedQuestId = ResolveSelectedQuestId(selectedQuestId, activeQuest);
        SelectedQuest = Cards.FirstOrDefault(card => card.Id == SelectedQuestId) ?? Cards.FirstOrDefault();
        Detail = SelectedQuest is null ? null : BuildDetail(SelectedQuest, activeQuest, claimedQuestIds);
    }

    public ChurchCharacterSummary Player { get; }

    public ActiveLongTermQuest? ActiveQuest { get; }

    public IReadOnlyList<string> ClaimedQuestIds { get; }

    public IReadOnlyList<string> UnlockedTitles { get; }

    public IReadOnlyList<ChurchQuestCardViewModel> Cards { get; }

    public string? SelectedQuestId { get; }

    public ChurchQuestCardViewModel? SelectedQuest { get; }

    public ChurchQuestDetailViewModel? Detail { get; }

    private IReadOnlyList<ChurchQuestCardViewModel> BuildCards(
        ActiveLongTermQuest? activeQuest,
        IReadOnlyCollection<string> claimedQuestIds)
    {
        var candidateIds = _catalog.GetCandidateIds();
        return _catalog.GetAll()
            .Select(definition =>
            {
                var isCandidate = candidateIds.Contains(definition.Id);
                var isActive = activeQuest?.QuestId == definition.Id;
                var isClaimed = claimedQuestIds.Contains(definition.Id);
                var progress = isActive ? activeQuest!.Progress : isClaimed ? definition.RequiredAmount : 0;
                var status = ResolveStatus(definition, isCandidate, isActive, isClaimed, activeQuest);
                return new ChurchQuestCardViewModel(
                    definition.Id,
                    definition.Title,
                    definition.Requester,
                    definition.IconType,
                    status,
                    GetStatusLabel(status),
                    progress,
                    definition.RequiredAmount,
                    isCandidate && !isClaimed);
            })
            .ToArray();
    }

    private ChurchQuestDetailViewModel BuildDetail(
        ChurchQuestCardViewModel card,
        ActiveLongTermQuest? activeQuest,
        IReadOnlyCollection<string> claimedQuestIds)
    {
        var definition = _catalog.GetById(card.Id)!;
        var isActive = activeQuest?.QuestId == definition.Id;
        var isCompleted = isActive && activeQuest!.Progress >= definition.RequiredAmount;
        var isClaimed = claimedQuestIds.Contains(definition.Id);
        var canAccept = card.CanSelect &&
            activeQuest is null &&
            !isClaimed;
        var canClaim = isCompleted && activeQuest!.IsCompleted && !activeQuest.IsClaimed;
        var canAbandon = isActive && !activeQuest!.IsClaimed;
        return new ChurchQuestDetailViewModel(
            definition.Id,
            definition.Title,
            definition.Requester,
            definition.Description,
            BuildRequirementText(definition),
            Math.Min(card.Progress, definition.RequiredAmount),
            definition.RequiredAmount,
            definition.RewardGold,
            definition.RewardTitle,
            definition.DialogueLines,
            canAccept,
            canClaim,
            canAbandon,
            BuildDisabledReason(card, activeQuest, isClaimed));
    }

    private string ResolveSelectedQuestId(string? selectedQuestId, ActiveLongTermQuest? activeQuest)
    {
        if (!string.IsNullOrWhiteSpace(selectedQuestId) &&
            _catalog.GetById(selectedQuestId) is not null)
        {
            return selectedQuestId;
        }

        if (!string.IsNullOrWhiteSpace(activeQuest?.QuestId) &&
            _catalog.GetById(activeQuest.QuestId) is not null)
        {
            return activeQuest.QuestId;
        }

        return _catalog.GetCandidateIds().FirstOrDefault() ?? _catalog.GetAll().First().Id;
    }

    private static ChurchQuestStatus ResolveStatus(
        LongTermQuestDefinition definition,
        bool isCandidate,
        bool isActive,
        bool isClaimed,
        ActiveLongTermQuest? activeQuest)
    {
        if (isClaimed)
        {
            return ChurchQuestStatus.Claimed;
        }

        if (isActive && activeQuest is not null)
        {
            return activeQuest.Progress >= definition.RequiredAmount
                ? ChurchQuestStatus.Completed
                : ChurchQuestStatus.Active;
        }

        return isCandidate ? ChurchQuestStatus.Available : ChurchQuestStatus.Locked;
    }

    private static string GetStatusLabel(ChurchQuestStatus status)
    {
        return status switch
        {
            ChurchQuestStatus.Available => "可接",
            ChurchQuestStatus.Active => "進行中",
            ChurchQuestStatus.Completed => "完成",
            ChurchQuestStatus.Claimed => "已完成",
            _ => "線索",
        };
    }

    private static string BuildRequirementText(LongTermQuestDefinition definition)
    {
        return definition.ObjectiveType switch
        {
            LongTermQuestObjectiveType.DefeatBosses => $"擊破 Boss {definition.RequiredAmount} 次",
            LongTermQuestObjectiveType.EarnGold => $"累積獲得 {definition.RequiredAmount} Gold",
            LongTermQuestObjectiveType.CompleteRooms => $"完成任意房間 {definition.RequiredAmount} 次",
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms => $"完成指定地城房間 {definition.RequiredAmount} 次",
            _ => $"完成目標 {definition.RequiredAmount} 次",
        };
    }

    private static string BuildDisabledReason(
        ChurchQuestCardViewModel card,
        ActiveLongTermQuest? activeQuest,
        bool isClaimed)
    {
        if (isClaimed)
        {
            return "此委託已完成。";
        }

        if (card.Status == ChurchQuestStatus.Locked)
        {
            return "目前只有線索，尚未開放接取。";
        }

        if (activeQuest is not null && activeQuest.QuestId != card.Id)
        {
            return "一次只能持有一個誓約。";
        }

        return string.Empty;
    }
}

public sealed record ChurchCharacterSummary(
    int Level,
    int Experience,
    int ExperienceToNextLevel,
    int Gold);

public sealed record ChurchQuestCardViewModel(
    string Id,
    string Title,
    string Requester,
    string IconType,
    ChurchQuestStatus Status,
    string StatusLabel,
    int Progress,
    int RequiredAmount,
    bool CanSelect);

public sealed record ChurchQuestDetailViewModel(
    string Id,
    string Title,
    string Requester,
    string Description,
    string RequirementText,
    int Progress,
    int RequiredAmount,
    int RewardGold,
    string RewardTitle,
    IReadOnlyList<string> DialogueLines,
    bool CanAccept,
    bool CanClaim,
    bool CanAbandon,
    string DisabledReason);

public enum ChurchQuestStatus
{
    Available,
    Active,
    Completed,
    Claimed,
    Locked,
}
