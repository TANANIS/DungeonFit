using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
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
        session.UpdatePlayerProfile(172, FitnessGoal.FatLoss);
        session.RecordTodayWeight(72.4);
        session.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90, "chest_push_up"),
            new DungeonRouteSlot("shoulders", 4, 12, "chest_quest_01", 90),
        });

        var town = Load<TownView>(TownScenePath);
        town.Initialize(
            session.Player,
            session.SelectedPlan,
            session.LastRunSummary,
            session.BuildIdleRewardViewModel(),
            session.GetSaveStatus(),
            session.BuildBodyProfileViewModel(),
            session.BuildTutorialGuideViewModel());
        lines.Add(await TryCapture(parent, town, "Town"));
        lines.Add(await TryCaptureTownPlayerVisual(parent, session));
        lines.Add(await TryCaptureTownProfileOnboarding(parent, session));
        lines.Add(await TryCaptureTownBodyMetricsDialog(parent, session));

        var plan = Load<DungeonPlanView>(DungeonPlanScenePath);
        plan.Initialize(
            session.SelectedPlan,
            session.ActiveRun,
            session.SelectedDungeonRoute,
            session.CanEditPlan,
            session.ActiveShortTermQuests);
        lines.Add(await TryCaptureDungeonPlanReference(parent, plan));

        lines.Add(await TryCaptureDungeonPlanDialog(parent, session));
        lines.Add(await TryCaptureDungeonPlanMusicDialog(parent, session));

        session.UpdateDungeonRoute(new[]
        {
            new DungeonRouteSlot("chest", 4, 12, "chest_quest_01", 90, "chest_push_up"),
            new DungeonRouteSlot("legs", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("core", 4, 12, "chest_quest_01", 90),
            new DungeonRouteSlot("arms", 4, 12, "chest_quest_01", 90),
        });

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
        lines.Add(await TryCaptureRoomVisual(parent, "core", 1, 4, isBossWave: false, "RoomChallengeSlime"));
        lines.Add(await TryCaptureRoomVisual(parent, "chest", 3, 4, isBossWave: false, "RoomChallengeElite"));
        lines.Add(await TryCaptureRoomVisual(parent, "chest", 1, 1, isBossWave: true, "RoomChallengeBoss"));
        lines.Add(await TryCaptureActorVisualGrid(parent));

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
        session.Player.Apply(new LootTable().RollDungeonChest(new DungeonChest(
            "ui_tavern_item_icon",
            "Boss",
            "ui_tavern_stage",
            "chest",
            "ui_tavern_stage_set_4",
            CompletionResult.Completed,
            4)));
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

    public static async Task<string> CaptureRoomBossVisual(Control parent)
    {
        parent.GetWindow().Size = new Vector2I(540, 960);
        EnsureOutputDirectory();
        return await TryCaptureRoomVisual(parent, "chest", 1, 1, isBossWave: true, "RoomChallengeBoss");
    }

    private static async Task<string> Capture<TView>(Control parent, TView view, string name)
        where TView : Control
    {
        parent.AddChild(view);
        await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
        await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);

        return CaptureMounted(parent, view, name);
    }

    private static async Task<string> TryCaptureTownPlayerVisual(Control parent, GameSession session)
    {
        var town = Load<TownView>(TownScenePath);
        try
        {
            town.Initialize(
                session.Player,
                session.SelectedPlan,
                session.LastRunSummary,
                session.BuildIdleRewardViewModel(),
                session.GetSaveStatus(),
                session.BuildBodyProfileViewModel(),
                session.BuildTutorialGuideViewModel());
            parent.AddChild(town);
            for (var frame = 0; frame < 12; frame++)
            {
                await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            if (!town.SmokeHideTutorial())
            {
                parent.RemoveChild(town);
                town.Free();
                return "UI_SCREENSHOT_SKIPPED TownPlayerVisual tutorial-not-built";
            }

            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            return CaptureMounted(parent, town, "TownPlayerVisual");
        }
        catch (System.Exception exception)
        {
            if (town.GetParent() == parent)
            {
                parent.RemoveChild(town);
            }

            town.Free();
            return $"UI_SCREENSHOT_FAILED TownPlayerVisual {exception.GetType().Name}";
        }
    }

    private static async Task<string> TryCaptureDungeonPlanReference(Control parent, DungeonPlanView plan)
    {
        try
        {
            parent.AddChild(plan);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);

            if (!plan.SmokeApplyReferencePresentation())
            {
                parent.RemoveChild(plan);
                plan.Free();
                return "UI_SCREENSHOT_SKIPPED DungeonPlan reference-presentation-not-ready";
            }

            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            return CaptureMounted(parent, plan, "DungeonPlan");
        }
        catch (System.Exception exception)
        {
            if (plan.GetParent() == parent)
            {
                parent.RemoveChild(plan);
            }

            plan.Free();
            return $"UI_SCREENSHOT_FAILED DungeonPlan {exception.GetType().Name}";
        }
    }

    private static string CaptureMounted<TView>(Control parent, TView view, string name)
        where TView : Control
    {
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

    private static async Task<string> TryCaptureDungeonPlanDialog(Control parent, GameSession session)
    {
        var plan = Load<DungeonPlanView>(DungeonPlanScenePath);
        try
        {
            plan.Initialize(
                session.SelectedPlan,
                session.ActiveRun,
                session.SelectedDungeonRoute,
                session.CanEditPlan,
                session.ActiveShortTermQuests);
            parent.AddChild(plan);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);

            if (!plan.SmokeOpenFirstDungeonDialog())
            {
                parent.RemoveChild(plan);
                plan.Free();
                return "UI_SCREENSHOT_SKIPPED DungeonPlanExerciseDialog not-opened";
            }

            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            return CaptureMounted(parent, plan, "DungeonPlanExerciseDialog");
        }
        catch (System.Exception exception)
        {
            if (plan.GetParent() == parent)
            {
                parent.RemoveChild(plan);
            }

            plan.Free();
            return $"UI_SCREENSHOT_FAILED DungeonPlanExerciseDialog {exception.GetType().Name}";
        }
    }

    private static async Task<string> TryCaptureRoomVisual(
        Control parent,
        string dungeonTypeId,
        int currentSet,
        int totalSets,
        bool isBossWave,
        string name)
    {
        var catalog = new TaskCatalog();
        var plan = catalog.CreateDungeonPlanFromRoute(new[]
        {
            new DungeonRouteSlot(dungeonTypeId, totalSets, 12, "chest_quest_01", 90),
        });

        var task = plan.Stages.FirstOrDefault();
        if (task is null)
        {
            return $"UI_SCREENSHOT_SKIPPED {name} no-stage";
        }

        var room = Load<RoomChallengeView>(RoomChallengeScenePath);
        try
        {
            var player = new PlayerState();
            room.Initialize(
                player,
                task,
                1,
                1,
                player.MaxHp,
                new RoomSupplyViewModel(0, 3, false));
            parent.AddChild(room);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            room.SmokeStartActiveWave();

            if (!room.SmokeShowEnemyVisual(dungeonTypeId, currentSet, totalSets, isBossWave))
            {
                parent.RemoveChild(room);
                room.Free();
                return $"UI_SCREENSHOT_SKIPPED {name} visual-not-shown";
            }

            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            if (isBossWave)
            {
                await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            return CaptureMounted(parent, room, name);
        }
        catch (System.Exception exception)
        {
            if (room.GetParent() == parent)
            {
                parent.RemoveChild(room);
            }

            room.Free();
            return $"UI_SCREENSHOT_FAILED {name} {exception.GetType().Name}";
        }
    }

    private static async Task<string> TryCaptureActorVisualGrid(Control parent)
    {
        var visualIds = new[]
        {
            ActorVisualIds.SlimeBasic,
            ActorVisualIds.SkeletonBasic,
            ActorVisualIds.SkeletonArcher,
            ActorVisualIds.SkeletonArmored,
            ActorVisualIds.SkeletonGreatsword,
            ActorVisualIds.OrcBasic,
            ActorVisualIds.OrcArmored,
            ActorVisualIds.OrcElite,
            ActorVisualIds.OrcRiderBoss,
            ActorVisualIds.AxemanArmored,
            ActorVisualIds.WerewolfBoss,
            ActorVisualIds.WerebearBoss,
        };
        var catalog = new ActorVisualCatalog();
        var grid = new Control
        {
            Name = "ActorVisualGrid",
            CustomMinimumSize = new Vector2(540, 960),
        };
        grid.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Color = new Color(0.05f, 0.04f, 0.08f, 1),
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        grid.AddChild(background);

        const int columns = 3;
        const float cellWidth = 180;
        const float cellHeight = 210;
        for (var index = 0; index < visualIds.Length; index++)
        {
            var visual = catalog.Get(visualIds[index]);
            var cell = new PanelContainer
            {
                Position = new Vector2((index % columns) * cellWidth + 8, (index / columns) * cellHeight + 22),
                Size = new Vector2(cellWidth - 16, cellHeight - 18),
                ClipContents = true,
            };
            DungeonFitUi.ApplyPanel(cell, UiPanelStyle.Card);
            grid.AddChild(cell);

            var sprite = new AnimatedSprite2D
            {
                SpriteFrames = SpriteSheetFramesBuilder.Build(visual.ToAnimationSet()),
                Animation = "idle",
                Position = new Vector2((cellWidth - 16) * 0.5f, 78),
                Scale = Vector2.One * 4.8f * visual.DisplayScale,
                Centered = true,
                FlipH = true,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            };
            sprite.Play();
            cell.AddChild(sprite);

            var label = new Label
            {
                Text = visual.Id,
                Position = new Vector2(8, 132),
                Size = new Vector2(cellWidth - 32, 54),
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            label.AddThemeFontSizeOverride("font_size", 14);
            cell.AddChild(label);
        }

        return await TryCapture(parent, grid, "RoomChallengeDungeonVisualGrid");
    }

    private static async Task<string> TryCaptureTownProfileOnboarding(Control parent, GameSession session)
    {
        var town = Load<TownView>(TownScenePath);
        try
        {
            town.Initialize(
                session.Player,
                session.SelectedPlan,
                session.LastRunSummary,
                session.BuildIdleRewardViewModel(),
                session.GetSaveStatus(),
                BodyProfileViewModel.Empty);
            parent.AddChild(town);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);

            if (!town.SmokeOpenProfileOnboarding())
            {
                parent.RemoveChild(town);
                town.Free();
                return "UI_SCREENSHOT_SKIPPED TownProfileOnboarding not-opened";
            }

            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            return CaptureMounted(parent, town, "TownProfileOnboarding");
        }
        catch (System.Exception exception)
        {
            if (town.GetParent() == parent)
            {
                parent.RemoveChild(town);
            }

            town.Free();
            return $"UI_SCREENSHOT_FAILED TownProfileOnboarding {exception.GetType().Name}";
        }
    }

    private static async Task<string> TryCaptureTownBodyMetricsDialog(Control parent, GameSession session)
    {
        var town = Load<TownView>(TownScenePath);
        try
        {
            town.Initialize(
                session.Player,
                session.SelectedPlan,
                session.LastRunSummary,
                session.BuildIdleRewardViewModel(),
                session.GetSaveStatus(),
                session.BuildBodyProfileViewModel());
            parent.AddChild(town);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);

            if (!town.SmokeOpenBodyMetricsDialog())
            {
                parent.RemoveChild(town);
                town.Free();
                return "UI_SCREENSHOT_SKIPPED TownBodyMetricsDialog not-opened";
            }

            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            return CaptureMounted(parent, town, "TownBodyMetricsDialog");
        }
        catch (System.Exception exception)
        {
            if (town.GetParent() == parent)
            {
                parent.RemoveChild(town);
            }

            town.Free();
            return $"UI_SCREENSHOT_FAILED TownBodyMetricsDialog {exception.GetType().Name}";
        }
    }

    private static async Task<string> TryCaptureDungeonPlanMusicDialog(Control parent, GameSession session)
    {
        var plan = Load<DungeonPlanView>(DungeonPlanScenePath);
        try
        {
            plan.Initialize(
                session.SelectedPlan,
                session.ActiveRun,
                session.SelectedDungeonRoute,
                session.CanEditPlan,
                session.ActiveShortTermQuests);
            parent.AddChild(plan);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);

            if (!plan.SmokeOpenFirstDungeonMusicDialog())
            {
                parent.RemoveChild(plan);
                plan.Free();
                return "UI_SCREENSHOT_SKIPPED DungeonPlanMusicDialog not-opened";
            }

            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            return CaptureMounted(parent, plan, "DungeonPlanMusicDialog");
        }
        catch (System.Exception exception)
        {
            if (plan.GetParent() == parent)
            {
                parent.RemoveChild(plan);
            }

            plan.Free();
            return $"UI_SCREENSHOT_FAILED DungeonPlanMusicDialog {exception.GetType().Name}";
        }
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
