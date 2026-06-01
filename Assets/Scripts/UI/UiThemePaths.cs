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
    public const string IdleToken = "res://Assets/Art/UI/Town/idle_token.png";

    public const string DungeonPlanBackground = "res://Assets/Art/UI/DungeonPlan/bg_dungeon_plan.png";
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
        return $"res://Assets/Art/UI/Town/{id}.png";
    }

    public static string DungeonIcon(string id)
    {
        return $"res://Assets/Art/UI/DungeonPlan/dungeon_{id}.png";
    }
}
