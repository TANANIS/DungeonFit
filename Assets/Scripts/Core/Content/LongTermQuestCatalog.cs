using System;
using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class LongTermQuestCatalog
{
    private readonly LongTermQuestDefinition[] _quests =
    {
        new(
            "mayor_missing_daughter",
            "失蹤的女兒",
            "村長 艾德蒙",
            "村長的女兒昨夜沒有回家。她最後被人看見，是在月影森林附近。",
            new[]
            {
                "我的女兒昨晚沒有回來......",
                "有人說，她最後出現在月影森林附近。",
                "如果你願意，請替我尋找線索。",
            },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms,
            "chest",
            3,
            500,
            "尋光者",
            "person"),
        new(
            "blacksmith_unfinished_blade",
            "未完成的長劍",
            "鐵匠 布朗",
            "鐵匠需要強者的戰鬥痕跡，完成一把尚未淬成的長劍。",
            new[]
            {
                "這把長劍還缺最後一段火候。",
                "不是爐火，是戰鬥的痕跡。",
                "擊倒足夠的強敵後，我會知道該如何完成它。",
            },
            LongTermQuestObjectiveType.DefeatBosses,
            string.Empty,
            3,
            600,
            "爐火見證者",
            "sword"),
        new(
            "priest_faint_faith",
            "微弱的信仰",
            "祭司 伊蓮",
            "祭司請你在核心地城完成儀式訓練，穩住逐漸黯淡的月光信仰。",
            new[]
            {
                "月光仍在，只是變得很微弱。",
                "核心地城的節奏能讓祈禱重新穩定。",
                "請你替教堂完成這段儀式訓練。",
            },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms,
            "core",
            3,
            500,
            "靜月信徒",
            "moon"),
        new(
            "herbalist_moondew_research",
            "月露草研究",
            "藥師 米菈",
            "藥師正在研究月露草，需要你帶回足夠的旅費與材料紀錄。",
            new[]
            {
                "月露草不是稀有，只是很挑時間。",
                "我需要更多旅途紀錄與補給資金。",
                "等你累積足夠成果，我就能完成這份研究。",
            },
            LongTermQuestObjectiveType.EarnGold,
            string.Empty,
            800,
            450,
            "月露採集者",
            "herb"),
        new(
            "guard_gate_disturbance",
            "城門外的異動",
            "守衛 凱恩",
            "城門外的獸群躁動不安，守衛希望你多完成幾次巡邏訓練。",
            new[]
            {
                "城門外的聲音最近不太對。",
                "我們還不能確定來源，只能先加強巡邏。",
                "完成幾次訓練，幫我們穩住防線。",
            },
            LongTermQuestObjectiveType.CompleteRooms,
            string.Empty,
            6,
            650,
            "城門守望",
            "shield"),
        new(
            "traveler_distant_letter",
            "遠方來信",
            "旅人 諾亞",
            "旅人帶來一封遠方的信，需要你穿越手臂或背部地城路線尋找收件人的線索。",
            new[]
            {
                "這封信走了很遠，卻還沒找到收件人。",
                "線索指向手臂與背部地城附近的道路。",
                "如果你能沿路打聽，也許它終於能抵達。",
            },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms,
            "arms,back",
            4,
            550,
            "遠路信使",
            "letter"),
    };

    public IReadOnlyList<LongTermQuestDefinition> GetAll()
    {
        return _quests;
    }

    public IReadOnlyList<string> GetCandidateIds()
    {
        return _quests.Take(3).Select(quest => quest.Id).ToArray();
    }

    public LongTermQuestDefinition? GetById(string id)
    {
        return _quests.FirstOrDefault(quest => quest.Id == id);
    }

    public bool IsCandidate(string id)
    {
        return GetCandidateIds().Contains(id, StringComparer.Ordinal);
    }

    public static bool MatchesTarget(LongTermQuestDefinition definition, string dungeonTypeId)
    {
        if (string.IsNullOrWhiteSpace(definition.TargetDungeonTypeId))
        {
            return true;
        }

        return definition.TargetDungeonTypeId
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(dungeonTypeId, StringComparer.OrdinalIgnoreCase);
    }
}
