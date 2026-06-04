using System;
using System.Linq;
using DungeonFit.Core.Models;
using DungeonFit.Gameplay;
using Godot;

namespace DungeonFit.UI;

public partial class TavernView : Control
{
    public event Action? BackToTownRequested;
    public event Action? ManualSaveRequested;
    public event Action? DeleteSaveRequested;
    public event Action<string>? EquipRequested;
    public event Action<EquipmentSlot>? UnequipRequested;
    public event Action<string>? SellRequested;
    public event Action<string, bool>? LockChanged;
    public event Action? SellCommonRequested;
    public event Action? LockRareRequested;
    public event Action<EquipmentInventoryFilter, EquipmentInventorySort>? ViewChanged;

    private TavernEquipmentViewModel _model = null!;
    private EquipmentInventoryFilter _filter = EquipmentInventoryFilter.All;
    private EquipmentInventorySort _sort = EquipmentInventorySort.Rarity;
    private string? _selectedItemId;
    private Label _nameLevelLabel = null!;
    private Label _goldLabel = null!;
    private ProgressBar _expBar = null!;
    private VBoxContainer _equippedList = null!;
    private Label _bonusLinesLabel = null!;
    private GridContainer _inventoryGrid = null!;
    private Label _detailTitle = null!;
    private Label _detailMeta = null!;
    private Label _detailStats = null!;
    private Label _detailHint = null!;
    private PanelContainer _detailIconPanel = null!;
    private Button _equipButton = null!;
    private Button _sellButton = null!;
    private Button _lockButton = null!;
    private Button _sellCommonButton = null!;
    private Button _lockRareButton = null!;
    private OptionButton _filterOption = null!;
    private OptionButton _sortOption = null!;
    private SaveStatus? _saveStatus;
    private Label _saveStatusLabel = null!;
    private PanelContainer _settingsPanel = null!;

    public override void _Ready()
    {
        BindNodes();
        WireStaticControls();

        if (_model is not null)
        {
            Refresh();
        }
    }

    public void Initialize(TavernEquipmentViewModel model, SaveStatus? saveStatus = null)
    {
        _model = model;
        _saveStatus = saveStatus;
        _filter = model.Filter;
        _sort = model.Sort;

        if (_selectedItemId is null || model.AllInventoryItems.All(item => item.Id != _selectedItemId))
        {
            _selectedItemId = model.InventoryItems.FirstOrDefault()?.Id;
        }

        if (IsNodeReady())
        {
            Refresh();
        }
    }

    private void BindNodes()
    {
        DungeonFitUi.ApplyTheme(this);
        DungeonFitUi.AddBackground(this, UiThemePaths.CommonBackground);
        _nameLevelLabel = GetNode<Label>("%NameLevelLabel");
        _goldLabel = GetNode<Label>("%GoldLabel");
        _expBar = GetNode<ProgressBar>("%ExpBar");
        _equippedList = GetNode<VBoxContainer>("%EquippedList");
        _bonusLinesLabel = GetNode<Label>("%BonusLinesLabel");
        _inventoryGrid = GetNode<GridContainer>("%InventoryGrid");
        _detailTitle = GetNode<Label>("%DetailTitle");
        _detailMeta = GetNode<Label>("%DetailMeta");
        _detailStats = GetNode<Label>("%DetailStats");
        _detailHint = GetNode<Label>("%DetailHint");
        _detailIconPanel = GetNode<PanelContainer>("SafeMargin/Layout/DetailPanel/DetailMargin/DetailRow/ItemIcon");
        _equipButton = GetNode<Button>("%EquipButton");
        _sellButton = GetNode<Button>("%SellButton");
        _lockButton = GetNode<Button>("%LockButton");
        _filterOption = GetNode<OptionButton>("%FilterOption");
        _sortOption = GetNode<OptionButton>("%SortOption");
        _sellCommonButton = GetNode<Button>("%SellCommonButton");
        _lockRareButton = GetNode<Button>("%LockRareButton");
        ApplyArtStyles();
    }

    private void WireStaticControls()
    {
        GetNode<Button>("%TopBackButton").Pressed += ShowSettings;
        GetNode<Button>("%BottomBackButton").Pressed += () => BackToTownRequested?.Invoke();
        _equipButton.Pressed += OnEquipPressed;
        _sellButton.Pressed += OnSellPressed;
        _lockButton.Pressed += OnLockPressed;
        _sellCommonButton.Pressed += () => SellCommonRequested?.Invoke();
        _lockRareButton.Pressed += () => LockRareRequested?.Invoke();

        AddFilterOption(Text.All, EquipmentInventoryFilter.All);
        AddFilterOption(Text.Weapon, EquipmentInventoryFilter.Weapon);
        AddFilterOption(Text.Armor, EquipmentInventoryFilter.Armor);
        AddFilterOption(Text.Accessory, EquipmentInventoryFilter.Accessory);
        _filterOption.ItemSelected += OnFilterSelected;

        AddSortOption(Text.SortRarity, EquipmentInventorySort.Rarity);
        AddSortOption(Text.SortPower, EquipmentInventorySort.Power);
        AddSortOption(Text.SortType, EquipmentInventorySort.Type);
        AddSortOption(Text.SortSellPrice, EquipmentInventorySort.SellPrice);
        _sortOption.ItemSelected += OnSortSelected;
        BuildSettingsPanel();
    }

    public void UpdateSaveStatus(SaveStatus saveStatus)
    {
        _saveStatus = saveStatus;
        RefreshSaveStatus();
    }

    public bool SmokeOpenSettingsPanel()
    {
        ShowSettings();
        return _settingsPanel.Visible;
    }

    private void Refresh()
    {
        _nameLevelLabel.Text = $"{_model.Character.Name}     Lv.{_model.Character.Level}";
        _goldLabel.Text = $"Gold {_model.Character.Gold}";
        _bonusLinesLabel.Text = string.Join("\n", _model.CurrentBonusLines);
        _expBar.MaxValue = Math.Max(1, _model.Character.ExperienceToNextLevel);
        _expBar.Value = _model.Character.Experience;

        SelectOption(_filterOption, (int)_filter);
        SelectOption(_sortOption, (int)_sort);
        RefreshEquipped();
        RefreshInventory();
        RefreshBulkActions();
        RefreshDetail();
    }

    private void RefreshEquipped()
    {
        ClearChildren(_equippedList);
        foreach (var slot in _model.EquippedSlots)
        {
            var button = new Button
            {
                Text = slot.Item is null
                    ? $"{slot.Label}\n{Text.EmptySlot}"
                    : $"{slot.Label}\n{slot.Item.DisplayName}\n{slot.Item.Rarity} / {Text.Power} {slot.Item.EffectivePower}",
                CustomMinimumSize = new Vector2(0, 110),
            };
            button.AddThemeFontSizeOverride("font_size", 24);
            button.Disabled = slot.Item is null;
            var itemId = slot.Item?.Id;
            button.Pressed += () =>
            {
                _selectedItemId = itemId;
                RefreshDetail();
            };
            _equippedList.AddChild(button);
        }
    }

    private void RefreshInventory()
    {
        ClearChildren(_inventoryGrid);

        if (_model.InventoryItems.Count == 0)
        {
            _inventoryGrid.Columns = 1;
            _inventoryGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var empty = CreateLabel(Text.EmptyInventory, 26, HorizontalAlignment.Center);
            empty.CustomMinimumSize = new Vector2(0, 220);
            empty.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            empty.SizeFlagsVertical = SizeFlags.ExpandFill;
            empty.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            empty.VerticalAlignment = VerticalAlignment.Center;
            _inventoryGrid.AddChild(empty);
            return;
        }

        _inventoryGrid.Columns = 4;
        _inventoryGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        foreach (var item in _model.InventoryItems)
        {
            var marker = item.IsEquipped ? Text.EquippedMarker : item.IsLocked ? Text.LockedMarker : string.Empty;
            var button = new Button
            {
                Text = $"{marker}{item.DisplayName}\n{item.SlotLabel} / {item.Rarity}\n{Text.Power} {item.EffectivePower}  {item.LevelRangeText}",
                CustomMinimumSize = new Vector2(142, 120),
                ToggleMode = true,
                ButtonPressed = item.Id == _selectedItemId,
            };
            button.AddThemeFontSizeOverride("font_size", 20);
            var itemId = item.Id;
            button.Pressed += () =>
            {
                _selectedItemId = itemId;
                RefreshInventory();
                RefreshDetail();
            };
            _inventoryGrid.AddChild(button);
        }
    }

    private void RefreshDetail()
    {
        var item = SelectedItem;
        if (item is null)
        {
            _detailTitle.Text = Text.NoSelection;
            _detailMeta.Text = Text.NoSelectionMeta;
            _detailStats.Text = string.Empty;
            _detailHint.Text = Text.NoSelectionHint;
            RefreshDetailIcon(string.Empty);
            _equipButton.Disabled = true;
            _sellButton.Disabled = true;
            _lockButton.Disabled = true;
            _equipButton.Text = Text.Equip;
            _sellButton.Text = Text.Sell;
            _lockButton.Text = Text.Lock;
            return;
        }

        _detailTitle.Text = item.DisplayName;
        RefreshDetailIcon(item.IconPath);
        var powerText = item.EffectivePower == item.Power
            ? item.Power.ToString()
            : $"{item.EffectivePower}/{item.Power}";
        var levelState = item.IsWithinRecommendedLevel ? Text.LevelRangeOk : Text.LevelRangeDecayed;
        _detailMeta.Text = $"{item.Rarity} | {item.SlotLabel} | {Text.Power} {powerText} | {item.LevelRangeText} {levelState}";
        _detailStats.Text = string.Join("\n", item.ModifierLines) + "\n" +
            string.Format(Text.SellPriceFormat, item.SellPrice);
        _detailHint.Text = item.IsEquipped
            ? Text.EquippedHint
            : item.IsLocked
                ? Text.LockedHint
                : Text.ReadyHint;
        _equipButton.Disabled = !item.CanEquip && !item.CanUnequip;
        _equipButton.Text = item.CanUnequip ? Text.Unequip : Text.Equip;
        _sellButton.Disabled = !item.CanSell;
        _lockButton.Disabled = item.IsEquipped;
        _lockButton.Text = item.IsLocked ? Text.Unlock : Text.Lock;
    }

    private void RefreshBulkActions()
    {
        _sellCommonButton.Text = string.Format(
            Text.SellCommonFormat,
            _model.CommonSellableCount,
            _model.CommonSellableValue);
        _sellCommonButton.Disabled = _model.CommonSellableCount <= 0;
        _lockRareButton.Text = string.Format(Text.LockRareFormat, _model.RareUnlockedCount);
        _lockRareButton.Disabled = _model.RareUnlockedCount <= 0;
    }

    private void RefreshDetailIcon(string iconPath)
    {
        ClearChildren(_detailIconPanel);
        DungeonFitUi.ApplyPanel(_detailIconPanel, UiPanelStyle.Token);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _detailIconPanel.AddChild(center);

        if (string.IsNullOrWhiteSpace(iconPath))
        {
            var label = CreateLabel(Text.ItemIconFallback, 26, HorizontalAlignment.Center);
            label.VerticalAlignment = VerticalAlignment.Center;
            center.AddChild(label);
            return;
        }

        center.AddChild(DungeonFitUi.CreateIcon(iconPath, 128));
    }

    private TavernInventoryItemViewModel? SelectedItem =>
        _selectedItemId is null
            ? null
            : _model.AllInventoryItems.FirstOrDefault(item => item.Id == _selectedItemId);

    private void OnEquipPressed()
    {
        var item = SelectedItem;
        if (item is null)
        {
            return;
        }

        if (item.CanUnequip)
        {
            UnequipRequested?.Invoke(item.Slot);
            return;
        }

        EquipRequested?.Invoke(item.Id);
    }

    private void OnSellPressed()
    {
        var item = SelectedItem;
        if (item?.CanSell == true)
        {
            SellRequested?.Invoke(item.Id);
        }
    }

    private void OnLockPressed()
    {
        var item = SelectedItem;
        if (item is not null && !item.IsEquipped)
        {
            LockChanged?.Invoke(item.Id, !item.IsLocked);
        }
    }

    private void OnFilterSelected(long index)
    {
        _filter = (EquipmentInventoryFilter)_filterOption.GetItemId((int)index);
        _selectedItemId = null;
        ViewChanged?.Invoke(_filter, _sort);
    }

    private void OnSortSelected(long index)
    {
        _sort = (EquipmentInventorySort)_sortOption.GetItemId((int)index);
        ViewChanged?.Invoke(_filter, _sort);
    }

    private void BuildSettingsPanel()
    {
        _settingsPanel = new PanelContainer
        {
            Name = "SettingsPanel",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _settingsPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        DungeonFitUi.ApplyPanel(_settingsPanel, UiPanelStyle.Overlay);
        AddChild(_settingsPanel);
        _settingsPanel.MoveToFront();

        var outerMargin = new MarginContainer();
        outerMargin.AddThemeConstantOverride("margin_left", 44);
        outerMargin.AddThemeConstantOverride("margin_top", 120);
        outerMargin.AddThemeConstantOverride("margin_right", 44);
        outerMargin.AddThemeConstantOverride("margin_bottom", 120);
        _settingsPanel.AddChild(outerMargin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 20);
        outerMargin.AddChild(layout);

        var title = CreateLabel(Text.SettingsTitle, 42, HorizontalAlignment.Center);
        layout.AddChild(title);

        var description = CreateLabel(Text.SettingsDescription, 26, HorizontalAlignment.Center);
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        layout.AddChild(description);

        _saveStatusLabel = CreateLabel(string.Empty, 28, HorizontalAlignment.Center);
        _saveStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        layout.AddChild(_saveStatusLabel);

        var saveButton = CreateSettingsButton(Text.ManualSave);
        saveButton.Pressed += () => ManualSaveRequested?.Invoke();
        layout.AddChild(saveButton);

        var deleteButton = CreateSettingsButton(Text.DeleteSave);
        deleteButton.Pressed += () => DeleteSaveRequested?.Invoke();
        layout.AddChild(deleteButton);

        var closeButton = CreateSettingsButton(Text.Close);
        closeButton.Pressed += HideSettings;
        layout.AddChild(closeButton);
    }

    private void ShowSettings()
    {
        _settingsPanel.Visible = true;
        _settingsPanel.MoveToFront();
        RefreshSaveStatus();
    }

    private void HideSettings()
    {
        _settingsPanel.Visible = false;
    }

    private void RefreshSaveStatus()
    {
        if (_saveStatusLabel is null)
        {
            return;
        }

        if (_saveStatus is null)
        {
            _saveStatusLabel.Text = Text.SaveStatusUnknown;
            return;
        }

        if (!string.IsNullOrWhiteSpace(_saveStatus.WarningMessage))
        {
            _saveStatusLabel.Text = _saveStatus.WarningMessage;
            return;
        }

        _saveStatusLabel.Text = _saveStatus.HasSaveFile
            ? string.Format(
                Text.SaveStatusFormat,
                _saveStatus.Gold,
                _saveStatus.RouteSlotCount,
                _saveStatus.CompletedStageCount,
                _saveStatus.BankedRewardCount,
                _saveStatus.BankedChestCount,
                _saveStatus.DailyRewardsClaimed ? Text.Claimed : Text.Unclaimed)
            : Text.NoSaveFile;
    }

    private void AddFilterOption(string text, EquipmentInventoryFilter filter)
    {
        _filterOption.AddItem(text, (int)filter);
    }

    private void AddSortOption(string text, EquipmentInventorySort sort)
    {
        _sortOption.AddItem(text, (int)sort);
    }

    private static void SelectOption(OptionButton option, int id)
    {
        for (var index = 0; index < option.ItemCount; index++)
        {
            if (option.GetItemId(index) == id)
            {
                option.Select(index);
                return;
            }
        }
    }

    private static Label CreateLabel(string text, int fontSize, HorizontalAlignment alignment)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static Button CreateSettingsButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 86),
        };
        button.AddThemeFontSizeOverride("font_size", 30);
        DungeonFitUi.ApplyButton(button, text == Text.DeleteSave ? UiButtonStyle.Danger : UiButtonStyle.Secondary);
        return button;
    }

    private void ApplyArtStyles()
    {
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/Header"), UiPanelStyle.Main);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/Header/HeaderMargin/HeaderRow/Portrait"), UiPanelStyle.Token);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/TitlePanel"), UiPanelStyle.Main);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/MainSplit/CharacterPanel"), UiPanelStyle.Card);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/MainSplit/CharacterPanel/CharacterMargin/CharacterLayout/StandIn"), UiPanelStyle.Token);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/MainSplit/CharacterPanel/CharacterMargin/CharacterLayout/BonusPanel"), UiPanelStyle.Card);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/MainSplit/InventoryPanel"), UiPanelStyle.Card);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/DetailPanel"), UiPanelStyle.Card);
        DungeonFitUi.ApplyPanel(GetNode<PanelContainer>("SafeMargin/Layout/DetailPanel/DetailMargin/DetailRow/ItemIcon"), UiPanelStyle.Token);
        DungeonFitUi.ApplyButton(GetNode<Button>("%TopBackButton"), UiButtonStyle.Secondary);
        DungeonFitUi.ApplyButton(GetNode<Button>("%BottomBackButton"), UiButtonStyle.Secondary);
        DungeonFitUi.ApplyButton(_equipButton, UiButtonStyle.Primary);
        DungeonFitUi.ApplyButton(_sellButton, UiButtonStyle.Danger);
        DungeonFitUi.ApplyButton(_lockButton, UiButtonStyle.Secondary);
        DungeonFitUi.ApplyButton(_sellCommonButton, UiButtonStyle.Danger);
        DungeonFitUi.ApplyButton(_lockRareButton, UiButtonStyle.Secondary);
        DungeonFitUi.ApplyProgressBar(_expBar, new Color(0.48f, 0.82f, 0.58f));
    }

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static class Text
    {
        public const string All = "\u5168\u90e8";
        public const string Weapon = "\u6b66\u5668";
        public const string Armor = "\u8b77\u7532";
        public const string Accessory = "\u98fe\u54c1";
        public const string SortRarity = "\u6392\u5e8f\uff1a\u7a00\u6709\u5ea6";
        public const string SortPower = "\u6392\u5e8f\uff1a\u6230\u529b";
        public const string SortType = "\u6392\u5e8f\uff1a\u985e\u578b";
        public const string SortSellPrice = "\u6392\u5e8f\uff1a\u552e\u50f9";
        public const string EmptySlot = "\u672a\u88dd\u5099";
        public const string EmptyInventory = "\u5009\u5eab\u76ee\u524d\u6c92\u6709\u88dd\u5099\u3002";
        public const string EquippedMarker = "[E] ";
        public const string LockedMarker = "[L] ";
        public const string Power = "\u6230\u529b";
        public const string LevelRangeOk = "\u6548\u679c\u5b8c\u6574";
        public const string LevelRangeDecayed = "\u8d85\u51fa\u7b49\u7d1a\u8870\u6e1b";
        public const string Attack = "\u653b\u64ca";
        public const string NoSelection = "\u5c1a\u672a\u9078\u64c7\u88dd\u5099";
        public const string NoSelectionMeta = "\u5f9e\u5009\u5eab\u6216\u5df2\u88dd\u5099\u6b04\u9078\u4e00\u4ef6\u88dd\u5099\u3002";
        public const string NoSelectionHint = "\u9078\u4e2d\u5f8c\u53ef\u4ee5\u88dd\u5099\u3001\u51fa\u552e\u6216\u9396\u5b9a\u3002";
        public const string ItemIconFallback = "\u88dd\u5099";
        public const string SellPriceFormat = "\u552e\u50f9\uff1a{0} Gold";
        public const string EquippedHint = "\u76ee\u524d\u5df2\u88dd\u5099\u3002";
        public const string LockedHint = "\u5df2\u9396\u5b9a\uff0c\u4e0d\u6703\u88ab\u51fa\u552e\u3002";
        public const string ReadyHint = "\u53ef\u6574\u7406\u3001\u88dd\u5099\u6216\u51fa\u552e\u3002";
        public const string Equip = "\u88dd\u5099";
        public const string Unequip = "\u5378\u4e0b";
        public const string Sell = "\u51fa\u552e";
        public const string SellCommonFormat = "\u552e\u666e\u901a {0} (+{1}G)";
        public const string LockRareFormat = "\u9396\u7a00\u6709 {0}";
        public const string Lock = "\u9396\u5b9a";
        public const string Unlock = "\u89e3\u9396";
        public const string SettingsTitle = "\u8a2d\u5b9a";
        public const string SettingsDescription = "\u904a\u6232\u6703\u5728\u8def\u7dda\u3001\u623f\u9593\u7d50\u679c\u8207\u9818\u53d6\u734e\u52f5\u6642\u81ea\u52d5\u5132\u5b58\u3002";
        public const string SaveStatusUnknown = "\u5b58\u6a94\u72c0\u614b\uff1a\u672a\u77e5";
        public const string NoSaveFile = "\u5b58\u6a94\u72c0\u614b\uff1a\u76ee\u524d\u6c92\u6709\u5b58\u6a94";
        public const string SaveStatusFormat = "\u5b58\u6a94\u72c0\u614b\uff1a\u5df2\u5b58\u5728\n\u91d1\u5e63 {0} / \u8def\u7dda {1} / \u5df2\u5b8c\u6210 {2} / \u66ab\u5b58\u734e\u52f5 {3} / \u5bf6\u7bb1 {4}\n\u4eca\u65e5\u734e\u52f5\uff1a{5}";
        public const string Claimed = "\u5df2\u9818\u53d6";
        public const string Unclaimed = "\u672a\u9818\u53d6";
        public const string ManualSave = "\u624b\u52d5\u5132\u5b58";
        public const string DeleteSave = "\u522a\u9664\u7576\u524d\u5b58\u6a94";
        public const string Close = "\u95dc\u9589";
    }
}
