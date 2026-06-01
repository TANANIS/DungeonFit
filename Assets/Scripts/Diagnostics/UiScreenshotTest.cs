using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Gameplay;
using DungeonFit.UI;
using Godot;

namespace DungeonFit.Diagnostics;

public static class UiScreenshotTest
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
    private const string OutputDirectory = "user://ui-smoke";

    public static async Task<IReadOnlyList<string>> Run(Control parent)
    {
        parent.GetWindow().Size = new Vector2I(540, 960);

        var lines = new List<string>();
        EnsureOutputDirectory();

        var session = new GameSession(persistenceEnabled: false);
        session.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("legs", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("core", 4, 12, "Training Loop", 90),
            new DungeonRouteSlot("arms", 4, 12, "Training Loop", 90),
        });

        var town = Load<TownView>(TownScenePath);
        town.Initialize(
            session.Player,
            session.SelectedPlan,
            session.LastRunSummary,
            session.BuildIdleRewardViewModel(),
            session.GetSaveStatus());
        lines.Add(await TryCapture(parent, town, "Town"));

        var plan = Load<DungeonPlanView>(DungeonPlanScenePath);
        plan.Initialize(
            session.SelectedPlan,
            session.ActiveRun,
            session.SelectedDungeonRoute,
            session.CanEditPlan,
            session.ActiveShortTermQuests);
        lines.Add(await TryCapture(parent, plan, "DungeonPlan"));

        var activeRun = session.StartOrGetActiveRun();
        if (activeRun is null)
        {
            lines.Add("UI_SCREENSHOT_SKIPPED RoomChallenge no-active-run");
            return lines;
        }

        var room = Load<RoomChallengeView>(RoomChallengeScenePath);
        room.Initialize(
            session.Player,
            activeRun.CurrentStage,
            activeRun.CurrentStageIndex + 1,
            activeRun.Plan.Stages.Count,
            activeRun.CurrentPlayerHp,
            session.BuildRoomSupplyViewModel());
        lines.Add(await TryCapture(parent, room, "RoomChallenge"));

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
            lines.Add(await TryCapture(parent, setSummary, "SetSummary"));
        }

        var dailySummaryModel = session.BuildDailySummary();
        if (dailySummaryModel is not null)
        {
            var dailySummary = Load<DailySummaryView>(DailySummaryScenePath);
            dailySummary.Initialize(dailySummaryModel, session.DailyRewardsClaimed);
            lines.Add(await TryCapture(parent, dailySummary, "DailySummary"));
        }

        var tavern = Load<TavernView>(TavernScenePath);
        tavern.Initialize(session.BuildTavernEquipmentViewModel(), session.GetSaveStatus());
        lines.Add(await TryCapture(parent, tavern, "Tavern"));

        lines.Add(await TryBuildAndCapture(parent, "Blacksmith", () =>
        {
            var blacksmith = Load<BlacksmithView>(BlacksmithScenePath);
            blacksmith.Initialize(session.BuildBlacksmithViewModel());
            return blacksmith;
        }));

        lines.Add(await TryBuildAndCapture(parent, "Church", () =>
        {
            var church = Load<ChurchView>(ChurchScenePath);
            church.Initialize(session.BuildChurchViewModel());
            return church;
        }));

        lines.Add(await TryBuildAndCapture(parent, "NoticeBoard", () =>
        {
            var noticeBoard = Load<NoticeBoardView>(NoticeBoardScenePath);
            noticeBoard.Initialize(new ShortTermQuestCatalog().GetDailyBoard(), session.ActiveShortTermQuests, session.Player);
            return noticeBoard;
        }));

        lines.Add(await TryBuildAndCapture(parent, "MoonlightFountain", () =>
        {
            var moon = Load<MoonlightFountainView>(MoonlightFountainScenePath);
            moon.Initialize(session.BuildMoonlightFountainViewModel());
            return moon;
        }));

        lines.Add(await TryBuildAndCapture(parent, "HerbShop", () =>
        {
            var herb = Load<HerbShopView>(HerbShopScenePath);
            herb.Initialize(session.BuildHerbShopViewModel());
            return herb;
        }));

        lines.Add($"UI_SCREENSHOT_DIR {ProjectSettings.GlobalizePath(OutputDirectory)}");
        WriteResults(lines);
        return lines;
    }

    private static async Task<string> Capture<TView>(Control parent, TView view, string name)
        where TView : Control
    {
        parent.AddChild(view);
        await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
        await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);

        var path = $"{OutputDirectory}/{name}.png";
        var displayServerName = DisplayServer.GetName();
        if (displayServerName.Contains("headless", System.StringComparison.OrdinalIgnoreCase) ||
            displayServerName.Contains("dummy", System.StringComparison.OrdinalIgnoreCase) ||
            OS.GetCmdlineArgs().Any(argument => argument.Contains("headless", System.StringComparison.OrdinalIgnoreCase)))
        {
            parent.RemoveChild(view);
            view.Free();
            return $"UI_SCREENSHOT_SKIPPED {name} {displayServerName}-renderer";
        }

        var texture = parent.GetViewport().GetTexture();
        Image? image;
        try
        {
            image = texture?.GetImage();
        }
        catch (System.Exception exception)
        {
            parent.RemoveChild(view);
            view.Free();
            return $"UI_SCREENSHOT_SKIPPED {name} viewport-image-error {exception.GetType().Name}";
        }

        if (image is null)
        {
            parent.RemoveChild(view);
            view.Free();
            return $"UI_SCREENSHOT_SKIPPED {name} viewport-image-unavailable";
        }

        var error = image.SavePng(path);
        parent.RemoveChild(view);
        view.Free();

        return error == Error.Ok
            ? $"UI_SCREENSHOT_SAVED {name} {ProjectSettings.GlobalizePath(path)}"
            : $"UI_SCREENSHOT_FAILED {name} {error}";
    }

    private static async Task<string> TryCapture<TView>(Control parent, TView view, string name)
        where TView : Control
    {
        try
        {
            return await Capture(parent, view, name);
        }
        catch (System.Exception exception)
        {
            if (view.GetParent() == parent)
            {
                parent.RemoveChild(view);
            }

            view.Free();
            return $"UI_SCREENSHOT_FAILED {name} {exception.GetType().Name}";
        }
    }

    private static async Task<string> TryBuildAndCapture<TView>(Control parent, string name, System.Func<TView> build)
        where TView : Control
    {
        try
        {
            return await TryCapture(parent, build(), name);
        }
        catch (System.Exception exception)
        {
            return $"UI_SCREENSHOT_FAILED {name} build-{exception.GetType().Name}";
        }
    }

    private static TView Load<TView>(string scenePath)
        where TView : Control
    {
        var scene = GD.Load<PackedScene>(scenePath);
        return scene.Instantiate<TView>();
    }

    private static void EnsureOutputDirectory()
    {
        var userDir = DirAccess.Open("user://");
        userDir?.MakeDirRecursive("ui-smoke");
    }

    private static void WriteResults(IReadOnlyList<string> lines)
    {
        using var file = FileAccess.Open($"{OutputDirectory}/results.txt", FileAccess.ModeFlags.Write);
        if (file is null)
        {
            return;
        }

        foreach (var line in lines)
        {
            file.StoreLine(line);
        }
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
