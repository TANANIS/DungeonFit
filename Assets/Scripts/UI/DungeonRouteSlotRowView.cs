using System;
using Godot;

namespace DungeonFit.UI;

public partial class DungeonRouteSlotRowView : PanelContainer
{
    public event Action? RemoveRequested;

    private Label _indexLabel = null!;
    private Label _routeLabel = null!;
    private Button _removeButton = null!;

    public override void _Ready()
    {
        BindNodes();
        DungeonFitUi.ApplyPanel(this, UiPanelStyle.Card);
        DungeonFitUi.ApplyButton(_removeButton, UiButtonStyle.Danger);
        _removeButton.Pressed += () => RemoveRequested?.Invoke();
    }

    public void Initialize(int index, string text, bool canRemove)
    {
        BindNodes();
        _indexLabel.Text = index.ToString();
        _routeLabel.Text = text;
        _removeButton.Visible = canRemove;
    }

    private void BindNodes()
    {
        _indexLabel ??= GetNode<Label>("%IndexLabel");
        _routeLabel ??= GetNode<Label>("%RouteLabel");
        _removeButton ??= GetNode<Button>("%RemoveButton");
    }
}
