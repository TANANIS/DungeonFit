using System;
using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;
using DungeonFit.Core.Rules;
using Godot;

namespace DungeonFit.UI;

public sealed class DungeonPortalSelectionView
{
    private const string CardScenePath = "res://Assets/Scenes/UI/DungeonTypeCard.tscn";

    private static readonly Dictionary<string, Vector2> CardAnchors = new()
    {
        ["chest"] = new(0.5f, 0.12f),
        ["shoulders"] = new(0.2f, 0.34f),
        ["arms"] = new(0.8f, 0.34f),
        ["back"] = new(0.5f, 0.54f),
        ["core"] = new(0.2f, 0.79f),
        ["legs"] = new(0.8f, 0.79f),
    };

    private readonly Control _stage;
    private readonly DungeonCategoryCatalog _categoryCatalog;
    private readonly PackedScene _cardScene;
    private readonly List<DungeonTypeCardView> _cards = new();

    public DungeonPortalSelectionView(Control stage, DungeonCategoryCatalog categoryCatalog)
    {
        _stage = stage;
        _categoryCatalog = categoryCatalog;
        _cardScene = GD.Load<PackedScene>(CardScenePath);
        _stage.Resized += LayoutCards;
    }

    public void Refresh(
        bool canEditPlan,
        int selectedCount,
        IReadOnlyList<DungeonRouteSlot> selectedRoute,
        Action<DungeonCategory> onSelected)
    {
        ClearCards();
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
            _stage.AddChild(card);
            _cards.Add(card);
        }

        LayoutCards();
    }

    private void LayoutCards()
    {
        if (_stage.Size.X <= 0 || _stage.Size.Y <= 0)
        {
            return;
        }

        var cardWidth = Mathf.Clamp(_stage.Size.X * 0.39f, 128f, 208f);
        var cardSize = new Vector2(cardWidth, cardWidth);
        foreach (var card in _cards)
        {
            if (!CardAnchors.TryGetValue(card.Name.ToString(), out var anchor))
            {
                anchor = new Vector2(0.5f, 0.5f);
            }

            var categoryId = card.GetMeta("dungeon_id", string.Empty).AsString();
            if (CardAnchors.TryGetValue(categoryId, out var categoryAnchor))
            {
                anchor = categoryAnchor;
            }

            card.Size = cardSize;
            card.Position = new Vector2(
                _stage.Size.X * anchor.X - cardSize.X * 0.5f,
                _stage.Size.Y * anchor.Y - cardSize.Y * 0.5f);
        }
    }

    private void ClearCards()
    {
        foreach (var card in _cards)
        {
            _stage.RemoveChild(card);
            card.QueueFree();
        }

        _cards.Clear();
    }
}
