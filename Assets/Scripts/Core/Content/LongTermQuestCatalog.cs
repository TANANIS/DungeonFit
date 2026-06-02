using System;
using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class LongTermQuestCatalog
{
    private readonly LongTermQuestDefinition[] _quests =
    {
        Quest("mayor_missing_daughter", "鎮長的失蹤女兒", "鎮長 羅恩",
            "鎮長的女兒在月光巡禮後失去蹤影。她最後被看見時，正前往胸地城附近的古井。",
            new[] { "我不能離開城鎮，但我也不能只坐著等。", "如果你願意訓練並深入胸地城，我會把城鎮的信任交給你。", "找到線索就回來，我會準備報酬。" },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms, "chest", 3, 500, "鎮長的信任", "person"),
        Quest("blacksmith_unfinished_blade", "未完成的月刃", "鐵匠 魯德",
            "魯德打造的月刃缺少 Boss 核心。每一次擊破 Boss，都能讓刀身更接近完成。",
            new[] { "這把刀不是給牆上看的。", "你去打倒 Boss，我把它磨成真正能守護城鎮的武器。", "三個核心，夠我完成第一階段。" },
            LongTermQuestObjectiveType.DefeatBosses, string.Empty, 3, 600, "月刃見證者", "sword"),
        Quest("priest_faint_faith", "微弱的信火", "修女 露希亞",
            "教堂的信火正在變暗，需要核心地城中的穩定節奏重新點亮。",
            new[] { "信火不需要轟烈，只需要你一次次穩定回來。", "核心訓練能讓月光重新聚攏。", "請把你的呼吸借給這座教堂。" },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms, "core", 3, 500, "信火守望", "moon"),
        Quest("guard_gate_disturbance", "城門騷動", "守備隊長 凱爾",
            "城門外的魔物正在試探巡邏路線。守備隊需要你完成多個房間，證明路線仍然安全。",
            new[] { "我們缺的不是英雄，是穩定完成任務的人。", "完成六個房間，讓大家知道城門還站得住。", "你回來時，我會把守備隊勳章交給你。" },
            LongTermQuestObjectiveType.CompleteRooms, string.Empty, 6, 650, "守備隊勳章", "shield"),
        Quest("herbalist_moondew_research", "月露研究", "藥草師 米菈",
            "米菈需要大量實戰回收的金幣和材料來完成月露研究。",
            new[] { "研究不是便宜的事，尤其是會發光的藥草。", "你在地城累積的金幣能讓我換到更好的器皿。", "帶回足夠資源，我會把成果分給你。" },
            LongTermQuestObjectiveType.EarnGold, string.Empty, 800, 450, "月露協力者", "herb"),
        Quest("traveler_distant_letter", "遠方旅人的信", "旅人 莉亞",
            "莉亞要寄出的信被分成數段散落在背與手臂地城之間。",
            new[] { "這封信不能再晚了。", "背地城和手臂地城都有線索，哪邊先找到都可以。", "如果你幫我送出它，我會記住你的名字。" },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms, "arms,back", 4, 550, "遠行信使", "letter"),
        Quest("leg_day_oath", "下盤誓約", "礦工 多恩",
            "礦坑深處的路需要真正穩定的腳步。完成多個腿地城房間，證明你能扛住長路。",
            new[] { "礦石不會自己走回來。", "腿地城會教你每一步都算數。", "完成它，我就讓礦工們承認你。" },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms, "legs", 5, 750, "深步礦友", "pick"),
        Quest("shoulder_wall_repair", "城牆修復", "塔樓守衛 艾登",
            "城牆上層需要重新吊掛石材。肩地城訓練是最直接的準備。",
            new[] { "我們能守住城牆，但得先把它修好。", "肩地城的訓練會讓你知道每一塊石頭的重量。", "完成後，塔樓會為你開門。" },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms, "shoulders", 5, 760, "塔樓協作者", "shield"),
        Quest("daily_route_finisher", "完整路線證明", "訓練導師 瑪爾",
            "導師要求你完成多條完整路線，證明訓練不是一時興起。",
            new[] { "一天完成一個房間很容易，完整走完路線才難。", "不要急，穩定比爆發更重要。", "完成八個房間後再來找我。" },
            LongTermQuestObjectiveType.CompleteRooms, string.Empty, 8, 900, "路線完成者", "person"),
        Quest("boss_hunter_contract", "Boss 獵手契約", "獵手 諾克",
            "諾克只承認擊破 Boss 的結果。任何地城 Boss 都算數。",
            new[] { "小怪不算。", "我只看你能不能在最後一組站住。", "五個 Boss，然後我們談真正的獎勵。" },
            LongTermQuestObjectiveType.DefeatBosses, string.Empty, 5, 1000, "Boss 獵手", "sword"),
        Quest("gold_reserve_drive", "城鎮儲備金", "會計官 薇拉",
            "城鎮正在建立防災儲備。你從地城帶回的金幣會成為第一筆基金。",
            new[] { "冒險不是只有浪漫，還有帳本。", "你賺到的每一枚金幣都能讓城鎮多撐一天。", "累積一千五百金幣，我會給你正式憑證。" },
            LongTermQuestObjectiveType.EarnGold, string.Empty, 1500, 900, "城鎮出資者", "chest"),
        Quest("balanced_training_oath", "均衡訓練誓約", "月光泉守護者",
            "守護者希望你不要只訓練單一部位。完成任意十個房間，讓身體保持均衡。",
            new[] { "月光不偏向某一塊肌肉。", "走不同路線，讓身體知道自己還有很多地方能變強。", "十個房間後，泉水會記住你的節奏。" },
            LongTermQuestObjectiveType.CompleteRooms, string.Empty, 10, 1200, "均衡誓約者", "moon"),
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

    private static LongTermQuestDefinition Quest(
        string id,
        string title,
        string requester,
        string description,
        IReadOnlyList<string> dialogueLines,
        LongTermQuestObjectiveType objectiveType,
        string targetDungeonTypeId,
        int requiredAmount,
        int rewardGold,
        string rewardTitle,
        string iconType)
    {
        return new LongTermQuestDefinition(
            id,
            title,
            requester,
            description,
            dialogueLines,
            objectiveType,
            targetDungeonTypeId,
            requiredAmount,
            rewardGold,
            rewardTitle,
            iconType);
    }
}
