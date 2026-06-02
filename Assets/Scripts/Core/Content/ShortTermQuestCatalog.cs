using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class ShortTermQuestCatalog
{
    private const int DailyQuestCount = 6;

    private readonly ShortTermQuestDefinition[] _quests =
    {
        Quest("herbal_chest", "藥草採集", "翠葉長者", "月影森林的藥草只會在訓練者的呼吸節奏中發光。", "完整完成 1 個胸地城房間。", "chest", 1, 40, "herb"),
        Quest("lost_necklace", "失落的項鍊", "凱爾", "父親留下的項鍊遺失在胸地城入口附近。", "完整完成 1 個胸地城房間。", "chest", 1, 60, "chest"),
        Quest("guard_chest_watch", "城門壓制", "守門隊長", "城門前的魔物受到胸地城波動吸引，需要一次穩定推進。", "完整完成 1 個胸地城房間。", "chest", 1, 55, "sword"),

        Quest("shoulder_banner", "高塔旗幟", "塔樓守衛", "高塔旗幟被風暴撕落，守衛需要能扛住重量的人協助。", "完整完成 1 個肩地城房間。", "shoulders", 1, 45, "sword"),
        Quest("shoulder_relay", "補給接力", "莉娜", "商隊需要把補給箱送上城牆平台。", "完整完成 1 個肩地城房間。", "shoulders", 1, 50, "chest"),
        Quest("shoulder_moon_lamp", "月燈修復", "燈塔學徒", "月燈支架鬆脫，需要穩定的肩部力量扶正。", "完整完成 1 個肩地城房間。", "shoulders", 1, 55, "heal"),

        Quest("monster_back", "怪物討伐", "黑翼守望者", "背地城的陰影變得騷動，守望者需要你壓住這場波動。", "完整完成 1 個背地城房間。", "back", 1, 50, "sword"),
        Quest("raven_mail", "渡鴉信件", "遠行者 莉亞", "渡鴉把信件掉進背地城深處，回收前別讓魔物啃碎。", "完整完成 1 個背地城房間。", "back", 1, 65, "chest"),
        Quest("back_bridge", "橋梁支撐", "木匠 布朗", "舊橋需要臨時支撐，背地城中的木材最適合補強。", "完整完成 1 個背地城房間。", "back", 1, 55, "pick"),

        Quest("ore_legs", "礦石收集", "鐵砧礦工", "深層礦石需要穩定的下盤才能搬回城裡。", "完整完成 1 個腿地城房間。", "legs", 1, 55, "pick"),
        Quest("leg_patrol", "南門巡邏", "巡防兵 羅伊", "南門外的路線需要重新踏查，腿地城會給你需要的耐力。", "完整完成 1 個腿地城房間。", "legs", 1, 50, "sword"),
        Quest("leg_caravan", "商隊護送", "商人 米娜", "商隊被迫停在斜坡下，需要有人清出安全道路。", "完整完成 1 個腿地城房間。", "legs", 1, 60, "chest"),

        Quest("healing_core", "治癒的祈願", "月白修女", "月光泉的儀式需要核心穩定的訓練者完成引導。", "完整完成 1 個核心地城房間。", "core", 1, 50, "heal"),
        Quest("core_ward", "結界校準", "結界師 奧蘭", "城鎮結界偏移，需要你用穩定呼吸協助重新定位。", "完整完成 1 個核心地城房間。", "core", 1, 55, "heal"),
        Quest("core_archive", "檔案搬運", "圖書館員 薇拉", "古代卷軸被封在核心地城石室中，不能劇烈晃動。", "完整完成 1 個核心地城房間。", "core", 1, 60, "chest"),

        Quest("parcel_arms", "送達包裹", "琳娜", "商隊的包裹被魔物擋在路上，需要一位可靠的冒險者幫忙送達。", "完整完成 1 個手臂地城房間。", "arms", 1, 45, "chest"),
        Quest("arm_blacksmith", "鐵匠鉚釘", "鐵匠 魯德", "一批鉚釘散落在手臂地城，找回它們能修好訓練器材。", "完整完成 1 個手臂地城房間。", "arms", 1, 55, "pick"),
        Quest("arm_signal", "信號旗", "哨兵 艾琳", "信號旗被扯落，哨兵需要你清出手臂地城的通道。", "完整完成 1 個手臂地城房間。", "arms", 1, 50, "sword"),
    };

    public IReadOnlyList<ShortTermQuestDefinition> GetDailyBoard()
    {
        var key = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return _quests
            .OrderBy(quest => StableRank($"{key}:{quest.Id}"))
            .Take(DailyQuestCount)
            .ToArray();
    }

    public ShortTermQuestDefinition? GetById(string id)
    {
        return _quests.FirstOrDefault(quest => quest.Id == id);
    }

    private static ShortTermQuestDefinition Quest(
        string id,
        string title,
        string npcName,
        string description,
        string requirement,
        string dungeonTypeId,
        int requiredAmount,
        int rewardGold,
        string iconType)
    {
        return new ShortTermQuestDefinition(id, title, npcName, description, requirement, dungeonTypeId, requiredAmount, rewardGold, iconType);
    }

    private static int StableRank(string seed)
    {
        var hash = 17;
        foreach (var character in seed)
        {
            hash = (hash * 31) + character;
        }

        return Math.Abs(hash);
    }
}
