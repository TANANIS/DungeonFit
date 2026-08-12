namespace DungeonFit.UI;

public static class UiThemePaths
{
    public const string Theme = "res://Assets/Art/UI/DungeonFitTheme.tres";

    public const string CommonBackground = "res://Assets/Art/UI/Common/bg_common.png";
    public const string MainPanel = "res://Assets/Art/UI/Common/panel_main.png";
    public const string CardPanel = "res://Assets/Art/UI/Common/panel_card.png";
    public const string PrimaryButton = "res://Assets/Art/UI/Common/button_primary.png";
    public const string SecondaryButton = "res://Assets/Art/UI/Common/button_secondary.png";
    public const string DangerButton = "res://Assets/Art/UI/Common/button_danger.png";

    public const string TownBackground = "res://Assets/Art/UI/Town/bg_town.png";
    public const string MoonlitTownMapBackground = "res://Assets/Art/Generated/Town/moonlit_town_map_v1.png";
    public const string MoonlitExpeditionForestBackground = "res://Assets/Art/Generated/Town/outdoor_exploration/moonlit_expedition_forest_v1.png";
    public const string IdleToken = "res://Assets/Art/UI/Town/idle_token.png";
    public const string TownPlayerPortrait = "res://Assets/Art/Generated/Town/player_knight/portrait/clean.png";

    public const string BlacksmithWorkshopBackground = "res://Assets/Art/Generated/Blacksmith/moonlit_blacksmith_workshop_v1.png";
    public const string BlacksmithEnhanceIcon = "res://Assets/Art/Generated/Blacksmith/forge_actions/single-1.png";
    public const string BlacksmithExtendIcon = "res://Assets/Art/Generated/Blacksmith/forge_actions/single-2.png";
    public const string BlacksmithDismantleIcon = "res://Assets/Art/Generated/Blacksmith/forge_actions/single-3.png";
    public const string BlacksmithForgeIcon = "res://Assets/Art/Generated/Blacksmith/forge_actions/single-4.png";

    public const string NoticeBoardBackground = "res://Assets/Art/Generated/NoticeBoard/moonlit_notice_board_v1.png";
    public const string NoticeBoardQuestParchment = "res://Assets/Art/Generated/NoticeBoard/quest_parchment/clean.png";
    public const string NoticeBoardSelectedQuestEmblem = "res://Assets/Art/Generated/NoticeBoard/ornaments/single-3.png";
    public const string NoticeBoardDetailDivider = "res://Assets/Art/Generated/NoticeBoard/ornaments/single-4.png";

    public static string NoticeBoardQuestGiver(int index)
    {
        var normalizedIndex = ((index % 6) + 6) % 6 + 1;
        return $"res://Assets/Art/Generated/NoticeBoard/quest_givers/single-{normalizedIndex}.png";
    }

    public const string DungeonPlanBackground = "res://Assets/Art/Generated/DungeonPlan/portal_hall/moonlit_dungeon_portal_hall_v2.png";
    public const string RouteSlot = "res://Assets/Art/UI/DungeonPlan/route_slot.png";

    public const string RoomBackground = "res://Assets/Art/UI/RoomChallenge/bg_room.png";
    public const string BattleStage = "res://Assets/Art/UI/RoomChallenge/battle_stage.png";
    public const string ActorToken = "res://Assets/Art/UI/RoomChallenge/actor_token.png";
    public const string RoomPotion = "res://Assets/Art/UI/RoomChallenge/potion.png";

    public const string SummaryBackground = "res://Assets/Art/UI/Summary/bg_summary.png";
    public const string RewardChest = "res://Assets/Art/UI/Summary/reward_chest.png";

    public const string GoldIcon = "res://Assets/Art/UI/Icons/gold.png";
    public const string ExperienceIcon = "res://Assets/Art/UI/Icons/exp.png";
    public const string PotionIcon = "res://Assets/Art/UI/Icons/potion.png";

    public static string TownFacilityIcon(string id)
    {
        return id switch
        {
            "herb_shop" => "res://Assets/Art/Generated/Town/facility_markers/props/herb-shop/prop.png",
            "tavern" => "res://Assets/Art/Generated/Town/facility_markers/props/tavern/prop.png",
            "blacksmith" => "res://Assets/Art/Generated/Town/facility_markers/props/blacksmith/prop.png",
            "notice_board" => "res://Assets/Art/Generated/Town/facility_markers/props/notice-board/prop.png",
            "fountain" => "res://Assets/Art/Generated/Town/facility_markers/props/fountain/prop.png",
            "church" => "res://Assets/Art/Generated/Town/facility_markers/props/church/prop.png",
            _ => $"res://Assets/Art/UI/Town/{id}.png",
        };
    }

    public static string TownPlayerWalkFrame(int frame)
    {
        return $"res://Assets/Art/Generated/Town/player_knight/walk/walk-{frame}.png";
    }

    public static string DungeonIcon(string id)
    {
        return $"res://Assets/Art/UI/DungeonPlan/dungeon_{id}.png";
    }

    public static string DungeonPlanEmblem(string id)
    {
        return id switch
        {
            "chest" => "res://Assets/Art/Generated/DungeonPlan/dungeon_emblems/props/chest/prop.png",
            "shoulders" => "res://Assets/Art/Generated/DungeonPlan/dungeon_emblems/props/shoulders/prop.png",
            "back" => "res://Assets/Art/Generated/DungeonPlan/dungeon_emblems/props/back/prop.png",
            "legs" => "res://Assets/Art/Generated/DungeonPlan/dungeon_emblems/props/legs/prop.png",
            "core" => "res://Assets/Art/Generated/DungeonPlan/dungeon_emblems/props/core/prop.png",
            "arms" => "res://Assets/Art/Generated/DungeonPlan/dungeon_emblems/props/arms/prop.png",
            _ => DungeonIcon(id),
        };
    }
}
