using System;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
using Godot;

namespace DungeonFit.UI;

public sealed class DungeonTypeGridView
{
    private readonly GridContainer _grid;
    private readonly DungeonCategoryCatalog _categoryCatalog;

    public DungeonTypeGridView(GridContainer grid, DungeonCategoryCatalog categoryCatalog)
    {
        _grid = grid;
        _categoryCatalog = categoryCatalog;
    }

    public void Refresh(bool canEditPlan, int selectedCount, Action<DungeonCategory> onSelected)
    {
        ClearChildren(_grid);

        foreach (var category in _categoryCatalog.GetAll())
        {
            var button = DungeonFitUi.CreateIconTextButton(
                UiThemePaths.DungeonIcon(category.Id),
                $"{category.ShortName}\n{Text.DungeonSuffix}",
                58,
                UiButtonStyle.Secondary,
                26);
            button.CustomMinimumSize = new Vector2(190, 130);
            button.Disabled = !canEditPlan || selectedCount >= DungeonRouteRules.MaxRouteSlots;
            button.Pressed += () => onSelected(category);
            _grid.AddChild(button);
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

    private static class Text
    {
        public const string DungeonSuffix = "\u5730\u57ce";
    }
}
