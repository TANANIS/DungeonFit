using System.Collections.Generic;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Gameplay;
using DungeonFit.UI;
using Godot;

namespace DungeonFit.Diagnostics;

public static class FlowSmokeUiLoader
{
    private const string TownScenePath = "res://Assets/Scenes/Town.tscn";
    private const string DungeonPlanScenePath = "res://Assets/Scenes/DungeonPlan.tscn";
    private const string RoomChallengeScenePath = "res://Assets/Scenes/RoomChallenge.tscn";
    private const string SetSummaryScenePath = "res://Assets/Scenes/SetSummary.tscn";
    private const string DailySummaryScenePath = "res://Assets/Scenes/DailySummary.tscn";
    private const string TavernScenePath = "res://Assets/Scenes/Tavern.tscn";
    private const string BlacksmithScenePath = "res://Assets/Scenes/Blacksmith.tscn";
    private const string ChurchScenePath = "res://Assets/Scenes/Church.tscn";
    private const string NoticeBoardScenePath = "res://Assets/Scenes/NoticeBoard.tscn";
    private const string MoonlightFountainScenePath = "res://Assets/Scenes/MoonlightFountain.tscn";
    private const string HerbShopScenePath = "res://Assets/Scenes/HerbShop.tscn";

    public static IEnumerable<string> Run(Node parent)
    {
        var session = new GameSession(persistenceEnabled: false);
        session.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90, "chest_push_up"),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });

        var town = Load<TownView>(TownScenePath);
        town.Initialize(
            session.Player,
            session.SelectedPlan,
            session.LastRunSummary,
            session.BuildIdleRewardViewModel(),
            session.GetSaveStatus());
        parent.AddChild(town);
        yield return "TOWN_UI_LOADED";
        Release(parent, town);

        var plan = Load<DungeonPlanView>(DungeonPlanScenePath);
        plan.Initialize(
            session.SelectedPlan,
            session.ActiveRun,
            session.SelectedDungeonRoute,
            session.CanEditPlan,
            session.ActiveShortTermQuests);
        parent.AddChild(plan);
        yield return "DUNGEON_PLAN_UI_LOADED";
        Release(parent, plan);

        var activeRun = session.StartOrGetActiveRun();
        if (activeRun is not null)
        {
            var room = Load<RoomChallengeView>(RoomChallengeScenePath);
            room.Initialize(
                session.Player,
                activeRun.CurrentStage,
                activeRun.CurrentStageIndex + 1,
                activeRun.Plan.Stages.Count,
                activeRun.CurrentPlayerHp,
                session.BuildRoomSupplyViewModel());
            parent.AddChild(room);
            yield return "ROOM_CHALLENGE_UI_LOADED";
            yield return $"ROOM_PAUSE_OPENED {room.SmokeOpenPauseMenu()}";
            yield return $"ROOM_PAUSE_RESUMED {room.SmokeResumePauseMenu()}";
            Release(parent, room);

            session.RecordStageResult(new RunSummary(
                "Smoke Cleared",
                activeRun.CurrentStage.RoomName,
                activeRun.CurrentStage.TotalSets,
                activeRun.CurrentStage.TotalSets,
                new RewardBundle(RewardSource.DungeonRoom, 40, null),
                CompletedResults(activeRun.CurrentStage.TotalSets),
                null,
                session.Player.CurrentHp,
                12));

            if (session.LastSetSummary is not null)
            {
                var setSummary = Load<SetSummaryView>(SetSummaryScenePath);
                setSummary.Initialize(session.LastSetSummary);
                parent.AddChild(setSummary);
                yield return "SET_SUMMARY_UI_LOADED";
                Release(parent, setSummary);
            }

            var dailySummaryModel = session.BuildDailySummary();
            if (dailySummaryModel is not null)
            {
                var dailySummary = Load<DailySummaryView>(DailySummaryScenePath);
                dailySummary.Initialize(dailySummaryModel, session.DailyRewardsClaimed);
                parent.AddChild(dailySummary);
                yield return "DAILY_SUMMARY_UI_LOADED";
                Release(parent, dailySummary);
            }
        }

        var tavern = Load<TavernView>(TavernScenePath);
        tavern.Initialize(session.BuildTavernEquipmentViewModel(), session.GetSaveStatus());
        parent.AddChild(tavern);
        yield return "TAVERN_UI_LOADED";
        yield return $"TAVERN_SETTINGS_OPENED {tavern.SmokeOpenSettingsPanel()}";
        Release(parent, tavern);

        var blacksmith = Load<BlacksmithView>(BlacksmithScenePath);
        blacksmith.Initialize(session.BuildBlacksmithViewModel());
        parent.AddChild(blacksmith);
        yield return "BLACKSMITH_UI_LOADED";
        Release(parent, blacksmith);

        var church = Load<ChurchView>(ChurchScenePath);
        church.Initialize(session.BuildChurchViewModel());
        parent.AddChild(church);
        yield return "CHURCH_UI_LOADED";
        Release(parent, church);

        var noticeBoard = Load<NoticeBoardView>(NoticeBoardScenePath);
        noticeBoard.Initialize(new ShortTermQuestCatalog().GetDailyBoard(), session.ActiveShortTermQuests, session.Player);
        parent.AddChild(noticeBoard);
        yield return "NOTICE_BOARD_UI_LOADED";
        Release(parent, noticeBoard);

        var moon = Load<MoonlightFountainView>(MoonlightFountainScenePath);
        moon.Initialize(session.BuildMoonlightFountainViewModel());
        parent.AddChild(moon);
        yield return "MOONLIGHT_UI_LOADED";
        Release(parent, moon);

        var herb = Load<HerbShopView>(HerbShopScenePath);
        herb.Initialize(session.BuildHerbShopViewModel());
        parent.AddChild(herb);
        yield return "HERB_UI_LOADED";
        Release(parent, herb);
    }

    private static TView Load<TView>(string scenePath)
        where TView : Control
    {
        var scene = GD.Load<PackedScene>(scenePath);
        return scene.Instantiate<TView>();
    }

    private static void Release(Node parent, Node child)
    {
        parent.RemoveChild(child);
        child.Free();
    }

    private static IReadOnlyList<CompletionResult> CompletedResults(int totalSets)
    {
        var results = new CompletionResult[totalSets];
        for (var index = 0; index < results.Length; index++)
        {
            results[index] = CompletionResult.Completed;
        }

        return results;
    }
}
