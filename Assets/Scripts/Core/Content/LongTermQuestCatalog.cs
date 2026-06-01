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
            "鎮長的失蹤女兒",
            "鎮長 羅恩",
            "鎮長的女兒在月光巡禮後失去蹤影。她最後被看見時，正前往胸地城附近的古井。",
            new[]
            {
                "我知道你不是普通的冒險者。",
                "她留下的月光墜飾，在胸地城入口附近被找到。",
                "若你能帶回線索，整座城鎮都會記得你的名字。",
            },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms,
            "chest",
            3,
            500,
            "鎮長的信任",
            "person"),
        new(
            "blacksmith_unfinished_blade",
            "未完成的月刃",
            "鐵匠 葛蘭",
            "鐵匠正在鍛造一把能承受月光魔力的刃器，但需要真正打倒 Boss 的戰鬥紀錄來完成淬火。",
            new[]
            {
                "普通的金屬承受不了月光地城的壓力。",
                "我要的不是材料，是你和 Boss 正面交手後留下的戰意。",
                "去打倒幾個強敵，回來時我會讓這把刃開口唱歌。",
            },
            LongTermQuestObjectiveType.DefeatBosses,
            string.Empty,
            3,
            600,
            "月刃鍛造權",
            "sword"),
        new(
            "priest_faint_faith",
            "微弱的星燈",
            "神父 伊萊",
            "教堂的星燈逐漸黯淡。神父相信，只要在核心地城完成試煉，就能重新點亮祈禱。",
            new[]
            {
                "月光不是只有照亮勝利，它也照見人害怕的地方。",
                "核心地城會考驗你的呼吸與意志。",
                "請把那裡的星火帶回來，哪怕只有一點點。",
            },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms,
            "core",
            3,
            500,
            "星燈祝禱",
            "moon"),
        new(
            "herbalist_moondew_research",
            "月露研究筆記",
            "藥草師 米菈",
            "藥草師正在研究月露藥劑的穩定配方，需要大量金幣購買保存容器與萃取材料。",
            new[]
            {
                "月露很美，但它也很任性。",
                "沒有好的容器，藥性會在日出前消散。",
                "幫我籌到研究費，我會把成果留給整座城鎮。",
            },
            LongTermQuestObjectiveType.EarnGold,
            string.Empty,
            800,
            450,
            "月露配方",
            "herb"),
        new(
            "guard_gate_disturbance",
            "城門外的騷動",
            "守衛隊長 凱爾",
            "城門外的魔物活動越來越頻繁。守衛隊長需要你完成多個房間，確認地城壓力是否正在外溢。",
            new[]
            {
                "夜裡的城門聲音變多了。",
                "如果只是風，我不會請你來。",
                "完成幾個房間，讓我知道我們該把防線往哪裡推。",
            },
            LongTermQuestObjectiveType.CompleteRooms,
            string.Empty,
            6,
            650,
            "城門守衛徽章",
            "shield"),
        new(
            "traveler_distant_letter",
            "遠方旅人的信",
            "旅人 莉亞",
            "一名旅人寄來求助信，提到背地城與手臂地城之間的道路被月光霧遮蔽。她需要可靠的路線紀錄。",
            new[]
            {
                "我不是第一次迷路，但這次的霧很奇怪。",
                "它像是在避開火光，只追著人的影子走。",
                "如果你能清出路線，我就能把這封信送到下一座城。",
            },
            LongTermQuestObjectiveType.CompleteDungeonTypeRooms,
            "arms,back",
            4,
            550,
            "遠行者信物",
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
