using System.Collections.Generic;
using DungeonFit.Gameplay;
using DungeonFit.UI;
using Godot;

namespace DungeonFit.Diagnostics;

public static class FlowSmokeUiLoader
{
    private const string TavernScenePath = "res://Assets/Scenes/Tavern.tscn";
    private const string BlacksmithScenePath = "res://Assets/Scenes/Blacksmith.tscn";
    private const string ChurchScenePath = "res://Assets/Scenes/Church.tscn";
    private const string MoonlightFountainScenePath = "res://Assets/Scenes/MoonlightFountain.tscn";
    private const string HerbShopScenePath = "res://Assets/Scenes/HerbShop.tscn";

    public static IEnumerable<string> Run(Node parent)
    {
        var session = new GameSession(persistenceEnabled: false);

        var tavern = Load<TavernView>(TavernScenePath);
        tavern.Initialize(session.BuildTavernEquipmentViewModel(), session.GetSaveStatus());
        parent.AddChild(tavern);
        yield return "TAVERN_UI_LOADED";
        yield return $"TAVERN_SETTINGS_OPENED {tavern.SmokeOpenSettingsPanel()}";

        var blacksmith = Load<BlacksmithView>(BlacksmithScenePath);
        blacksmith.Initialize(session.BuildBlacksmithViewModel());
        parent.AddChild(blacksmith);
        yield return "BLACKSMITH_UI_LOADED";

        var church = Load<ChurchView>(ChurchScenePath);
        church.Initialize(session.BuildChurchViewModel());
        parent.AddChild(church);
        yield return "CHURCH_UI_LOADED";

        var moon = Load<MoonlightFountainView>(MoonlightFountainScenePath);
        moon.Initialize(session.BuildMoonlightFountainViewModel());
        parent.AddChild(moon);
        yield return "MOONLIGHT_UI_LOADED";

        var herb = Load<HerbShopView>(HerbShopScenePath);
        herb.Initialize(session.BuildHerbShopViewModel());
        parent.AddChild(herb);
        yield return "HERB_UI_LOADED";
    }

    private static TView Load<TView>(string scenePath)
        where TView : Control
    {
        var scene = GD.Load<PackedScene>(scenePath);
        return scene.Instantiate<TView>();
    }
}
