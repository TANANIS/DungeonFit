using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class ShortTermQuestCatalog
{
    private readonly ShortTermQuestDefinition[] _quests =
    {
        new(
            "herbal_chest",
            "\u85e5\u8349\u63a1\u96c6",
            "\u7fe0\u8449\u9577\u8005",
            "\u6708\u5f71\u68ee\u6797\u7684\u85e5\u8349\u53ea\u6703\u5728\u8a13\u7df4\u8005\u7684\u547c\u5438\u7bc0\u594f\u4e2d\u767c\u5149\u3002",
            "\u5b8c\u6210 1 \u500b\u80f8\u5730\u57ce\u623f\u9593\u3002",
            "chest",
            1,
            40,
            "herb"),
        new(
            "lost_necklace",
            "\u5931\u843d\u7684\u4e0a\u9996",
            "\u51f1\u723e",
            "\u6211\u5728\u91ce\u5916\u907a\u5931\u4e86\u7236\u89aa\u7559\u7d66\u6211\u7684\u4e0a\u9996\uff0c\u90a3\u662f\u6211\u6700\u91cd\u8981\u7684\u56de\u61b6\u4e4b\u4e00\u3002",
            "\u5728\u80f8\u5730\u57ce\u5c0b\u56de\u751f\u93fd\u7684\u4e0a\u9996\u3002",
            "chest",
            1,
            60,
            "sword"),
        new(
            "parcel_arms",
            "\u9001\u9054\u5305\u88f9",
            "\u7433\u5a1c",
            "\u5546\u968a\u7684\u5305\u88f9\u88ab\u9b54\u7269\u64cb\u5728\u8def\u4e0a\uff0c\u9700\u8981\u4e00\u4f4d\u53ef\u9760\u7684\u5192\u96aa\u8005\u5e6b\u5fd9\u9001\u9054\u3002",
            "\u5b8c\u6210 1 \u500b\u624b\u81c2\u5730\u57ce\u623f\u9593\u3002",
            "arms",
            1,
            45,
            "chest"),
        new(
            "monster_back",
            "\u602a\u7269\u8a0e\u4f10",
            "\u9ed1\u7ffc\u5b88\u671b\u8005",
            "\u80cc\u5730\u57ce\u7684\u9670\u5f71\u8b8a\u5f97\u9a37\u52d5\uff0c\u5b88\u671b\u8005\u9700\u8981\u4f60\u58d3\u4f4f\u9019\u5834\u6ce2\u52d5\u3002",
            "\u5b8c\u6210 1 \u500b\u80cc\u5730\u57ce\u623f\u9593\u3002",
            "back",
            1,
            50,
            "sword"),
        new(
            "ore_legs",
            "\u7926\u77f3\u6536\u96c6",
            "\u9435\u7827\u7926\u5de5",
            "\u6df1\u5c64\u7926\u77f3\u9700\u8981\u7a69\u5b9a\u7684\u4e0b\u76e4\u624d\u80fd\u642c\u56de\u57ce\u88e1\u3002",
            "\u5b8c\u6210 1 \u500b\u817f\u5730\u57ce\u623f\u9593\u3002",
            "legs",
            1,
            55,
            "pick"),
        new(
            "healing_core",
            "\u6cbb\u7652\u7684\u7948\u9858",
            "\u6708\u767d\u4fee\u5973",
            "\u6708\u5149\u6cc9\u7684\u5100\u5f0f\u9700\u8981\u6838\u5fc3\u7a69\u5b9a\u7684\u8a13\u7df4\u8005\u5b8c\u6210\u5f15\u5c0e\u3002",
            "\u5b8c\u6210 1 \u500b\u6838\u5fc3\u5730\u57ce\u623f\u9593\u3002",
            "core",
            1,
            50,
            "heal"),
    };

    public IReadOnlyList<ShortTermQuestDefinition> GetDailyBoard()
    {
        return _quests;
    }

    public ShortTermQuestDefinition? GetById(string id)
    {
        return _quests.FirstOrDefault(quest => quest.Id == id);
    }
}
