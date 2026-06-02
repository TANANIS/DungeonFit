using System;
using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
using Godot;

namespace DungeonFit.UI;

public sealed class DungeonTypeGridView
{
    private const string CardScenePath = "res://Assets/Scenes/UI/DungeonTypeCard.tscn";

    private readonly GridContainer _grid;
    private readonly DungeonCategoryCatalog _categoryCatalog;
    private readonly PackedScene _cardScene;

    public DungeonTypeGridView(GridContainer grid, DungeonCategoryCatalog categoryCatalog)
    {
        _grid = grid;
        _categoryCatalog = categoryCatalog;
        _cardScene = GD.Load<PackedScene>(CardScenePath);
    }

    public void Refresh(bool canEditPlan, int selectedCount, Action<DungeonCategory> onSelected)
    {
        Refresh(canEditPlan, selectedCount, Array.Empty<DungeonRouteSlot>(), onSelected);
    }

    public void Refresh(
        bool canEditPlan,
        int selectedCount,
        IReadOnlyList<DungeonRouteSlot> selectedRoute,
        Action<DungeonCategory> onSelected)
    {
        ClearChildren(_grid);
        var selectedIds = selectedRoute.Select(slot => slot.DungeonTypeId).ToHashSet();

        foreach (var category in _categoryCatalog.GetAll())
        {
            var selected = selectedIds.Contains(category.Id);
            var card = _cardScene.Instantiate<DungeonTypeCardView>();
            card.Initialize(
                category,
                selected,
                !canEditPlan || selectedCount >= DungeonRouteRules.MaxRouteSlots);
            card.Selected += onSelected;
            _grid.AddChild(card);
        }
    }

    private static void ClearChildren(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

}
