using System;
using DungeonFit.Core.Models;
using Godot;

namespace DungeonFit.UI;

public partial class DungeonTypeCardView : Button
{
    public event Action<DungeonCategory>? Selected;

    private DungeonCategory _category = null!;
    private TextureRect _icon = null!;
    private Label _checkLabel = null!;
    private Label _nameLabel = null!;

    public override void _Ready()
    {
        BindNodes();
        Pressed += RequestSelected;
    }

    public void Initialize(DungeonCategory category, bool selected, bool disabled)
    {
        BindNodes();
        _category = category;
        SetMeta("dungeon_id", category.Id);
        _nameLabel.Text = category.ShortName;
        _checkLabel.Visible = selected;
        _checkLabel.Text = selected ? "\u2713" : string.Empty;
        _icon.Texture = GD.Load<Texture2D>(UiThemePaths.DungeonPlanEmblem(category.Id));
        _icon.Visible = _icon.Texture is not null;
        Disabled = disabled;
        DungeonFitUi.ApplyDungeonEmblemButton(this, selected);
    }

    private void RequestSelected()
    {
        if (!Disabled)
        {
            Selected?.Invoke(_category);
        }
    }

    private void BindNodes()
    {
        _icon ??= GetNode<TextureRect>("%Icon");
        _checkLabel ??= GetNode<Label>("%CheckLabel");
        _nameLabel ??= GetNode<Label>("%NameLabel");
    }

}
