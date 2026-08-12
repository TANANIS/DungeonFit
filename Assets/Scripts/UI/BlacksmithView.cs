using System;
using System.Linq;
using DungeonFit.Gameplay;
using Godot;

namespace DungeonFit.UI;

public partial class BlacksmithView : Control
{
	private enum BlacksmithMode
	{
		Enhance,
		Extend,
		Dismantle,
	}

	public event Action? BackToTownRequested;
	public event Action<string>? EnhanceRequested;
	public event Action<string>? ExtendLevelRangeRequested;
	public event Action<string>? DismantleRequested;

	private BlacksmithViewModel _model = null!;
	private BlacksmithMode _mode = BlacksmithMode.Enhance;
	private string? _selectedItemId;
	private HubHeaderControls _header = null!;
	private GridContainer _inventoryGrid = null!;
	private TextureRect _detailIcon = null!;
	private Label _detailTitle = null!;
	private Label _detailMeta = null!;
	private Label _detailStats = null!;
	private Button _enhanceModeButton = null!;
	private Button _extendModeButton = null!;
	private Button _dismantleModeButton = null!;
	private TextureRect _operationIcon = null!;
	private Label _operationPreview = null!;
	private Label _operationHint = null!;
	private Button _actionButton = null!;

	public override void _Ready()
	{
		BuildUi();
		if (_model is not null)
		{
			Refresh();
		}
	}

	public void Initialize(BlacksmithViewModel model)
	{
		_model = model;
		_selectedItemId = model.SelectedItemId;
		if (IsNodeReady())
		{
			Refresh();
		}
	}

	private void BuildUi()
	{
		DungeonFitUi.ApplyTheme(this);
		var background = DungeonFitUi.AddBackground(this, UiThemePaths.BlacksmithWorkshopBackground);
		background.OffsetTop = -250;
		background.OffsetBottom = -250;

		var shade = new ColorRect
		{
			Color = new Color(0.018f, 0.012f, 0.048f, 0.22f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(shade);

		var safe = CreateMargin(34, 32);
		safe.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(safe);

		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 14);
		safe.AddChild(layout);

		var header = HubHeaderBuilder.Build(Text.BackShort, out _header);
		ApplyForgePanel(header, new Color(0.018f, 0.014f, 0.055f, 0.94f), new Color(0.63f, 0.23f, 0.9f, 0.98f), 3);
		DungeonFitUi.ApplyButton(_header.ActionButton, UiButtonStyle.Primary);
		_header.ActionButton.Pressed += () => BackToTownRequested?.Invoke();
		layout.AddChild(header);

		var hero = new Control
		{
			CustomMinimumSize = new Vector2(0, 480),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		layout.AddChild(hero);
		var heroLayout = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		heroLayout.SetAnchorsPreset(LayoutPreset.TopWide);
		heroLayout.OffsetTop = 42;
		heroLayout.OffsetBottom = 184;
		heroLayout.AddThemeConstantOverride("separation", 4);
		hero.AddChild(heroLayout);
		var title = CreateNoWrapCenteredLabel(Text.Title, 80);
		title.AddThemeColorOverride("font_color", new Color(0.94f, 0.88f, 1f));
		title.AddThemeColorOverride("font_outline_color", new Color(0.16f, 0.045f, 0.28f));
		title.AddThemeConstantOverride("outline_size", 10);
		heroLayout.AddChild(title);
		var subtitle = CreateNoWrapCenteredLabel(Text.Subtitle, 38);
		subtitle.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.44f));
		heroLayout.AddChild(subtitle);
		heroLayout.AddChild(CreateNoWrapCenteredLabel(Text.Description, 26));

		var workstation = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		ApplyForgePanel(workstation, new Color(0.025f, 0.016f, 0.07f, 0.9f), new Color(0.67f, 0.24f, 0.92f, 0.95f), 4);
		layout.AddChild(workstation);
		var workstationMargin = CreateMargin(18, 16);
		workstation.AddChild(workstationMargin);
		var workstationLayout = new VBoxContainer();
		workstationLayout.AddThemeConstantOverride("separation", 12);
		workstationMargin.AddChild(workstationLayout);

		var itemRow = new HBoxContainer { CustomMinimumSize = new Vector2(0, 390) };
		itemRow.AddThemeConstantOverride("separation", 14);
		workstationLayout.AddChild(itemRow);

		var inventoryPanel = new PanelContainer { CustomMinimumSize = new Vector2(440, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
		ApplyForgePanel(inventoryPanel, new Color(0.035f, 0.022f, 0.085f, 0.9f), new Color(0.49f, 0.24f, 0.75f, 0.94f), 3);
		itemRow.AddChild(inventoryPanel);
		var inventoryMargin = CreateMargin(12, 12);
		inventoryPanel.AddChild(inventoryMargin);
		var inventoryLayout = new VBoxContainer();
		inventoryLayout.AddThemeConstantOverride("separation", 7);
		inventoryMargin.AddChild(inventoryLayout);
		inventoryLayout.AddChild(CreateSectionLabel(Text.Inventory, 22));
		_inventoryGrid = new GridContainer { Columns = 3, SizeFlagsVertical = SizeFlags.ExpandFill };
		_inventoryGrid.AddThemeConstantOverride("h_separation", 8);
		_inventoryGrid.AddThemeConstantOverride("v_separation", 8);
		inventoryLayout.AddChild(_inventoryGrid);

		var detailPanel = new PanelContainer { CustomMinimumSize = new Vector2(460, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
		ApplyForgePanel(detailPanel, new Color(0.26f, 0.16f, 0.095f, 0.96f), new Color(0.78f, 0.55f, 0.3f, 0.96f), 3);
		itemRow.AddChild(detailPanel);
		var detailMargin = CreateMargin(16, 14);
		detailPanel.AddChild(detailMargin);
		var detailRow = new HBoxContainer();
		detailRow.AddThemeConstantOverride("separation", 14);
		detailMargin.AddChild(detailRow);
		var detailIconFrame = new PanelContainer
		{
			CustomMinimumSize = new Vector2(150, 150),
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
		};
		ApplyForgePanel(detailIconFrame, new Color(0.035f, 0.025f, 0.055f, 0.95f), new Color(0.35f, 0.2f, 0.5f, 0.9f), 2);
		detailRow.AddChild(detailIconFrame);
		var detailIconCenter = new CenterContainer();
		detailIconFrame.AddChild(detailIconCenter);
		_detailIcon = DungeonFitUi.CreateIcon(UiThemePaths.BlacksmithEnhanceIcon, 124, "EquipmentIcon");
		detailIconCenter.AddChild(_detailIcon);
		var detailLayout = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		detailLayout.AddThemeConstantOverride("separation", 7);
		detailRow.AddChild(detailLayout);
		_detailTitle = CreateLabel(string.Empty, 28);
		_detailTitle.AddThemeColorOverride("font_color", new Color(0.92f, 0.68f, 1f));
		detailLayout.AddChild(_detailTitle);
		_detailMeta = CreateLabel(string.Empty, 19);
		_detailMeta.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.55f));
		detailLayout.AddChild(_detailMeta);
		_detailStats = CreateLabel(string.Empty, 18);
		_detailStats.SizeFlagsVertical = SizeFlags.ExpandFill;
		_detailStats.AddThemeColorOverride("font_color", new Color(0.96f, 0.87f, 0.72f));
		detailLayout.AddChild(_detailStats);

		var modeRow = new HBoxContainer { CustomMinimumSize = new Vector2(0, 82) };
		modeRow.AddThemeConstantOverride("separation", 10);
		workstationLayout.AddChild(modeRow);
		_enhanceModeButton = CreateModeButton(UiThemePaths.BlacksmithEnhanceIcon, Text.EnhanceMode);
		_enhanceModeButton.Pressed += () => SetMode(BlacksmithMode.Enhance);
		modeRow.AddChild(_enhanceModeButton);
		_extendModeButton = CreateModeButton(UiThemePaths.BlacksmithExtendIcon, Text.ExtendMode);
		_extendModeButton.Pressed += () => SetMode(BlacksmithMode.Extend);
		modeRow.AddChild(_extendModeButton);
		_dismantleModeButton = CreateModeButton(UiThemePaths.BlacksmithDismantleIcon, Text.DismantleMode);
		_dismantleModeButton.Pressed += () => SetMode(BlacksmithMode.Dismantle);
		modeRow.AddChild(_dismantleModeButton);

		var previewPanel = new PanelContainer { CustomMinimumSize = new Vector2(0, 290) };
		ApplyForgePanel(previewPanel, new Color(0.045f, 0.032f, 0.09f, 0.94f), new Color(0.62f, 0.42f, 0.23f, 0.94f), 3);
		workstationLayout.AddChild(previewPanel);
		var previewMargin = CreateMargin(16, 10);
		previewPanel.AddChild(previewMargin);
		var previewRow = new HBoxContainer();
		previewRow.AddThemeConstantOverride("separation", 18);
		previewMargin.AddChild(previewRow);
		_operationIcon = DungeonFitUi.CreateIcon(UiThemePaths.BlacksmithForgeIcon, 210, "ForgeActionIcon");
		previewRow.AddChild(_operationIcon);
		var previewLayout = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
		previewLayout.AddThemeConstantOverride("separation", 6);
		previewRow.AddChild(previewLayout);
		_operationPreview = CreateLabel(string.Empty, 26);
		_operationPreview.HorizontalAlignment = HorizontalAlignment.Center;
		previewLayout.AddChild(_operationPreview);
		_operationHint = CreateLabel(string.Empty, 20);
		_operationHint.HorizontalAlignment = HorizontalAlignment.Center;
		_operationHint.AddThemeColorOverride("font_color", new Color(1f, 0.78f, 0.42f));
		previewLayout.AddChild(_operationHint);

		_actionButton = CreateButton(string.Empty, 0, 150, 42, UiButtonStyle.Primary);
		_actionButton.Pressed += RunSelectedAction;
		workstationLayout.AddChild(_actionButton);

		var returnButton = CreateButton(Text.ReturnTown, 0, 110, 34, UiButtonStyle.Primary);
		returnButton.Pressed += () => BackToTownRequested?.Invoke();
		workstationLayout.AddChild(returnButton);
	}

	private void SetMode(BlacksmithMode mode)
	{
		_mode = mode;
		Refresh();
	}

	private void Refresh()
	{
		HubHeaderBuilder.Refresh(_header, _model.Character.Level, _model.Character.Experience, _model.Character.ExperienceToNextLevel, _model.Character.Gold);
		BuildInventoryGrid();
		RefreshDetail();
		RefreshOperation();
	}

	private void BuildInventoryGrid()
	{
		foreach (var child in _inventoryGrid.GetChildren())
		{
			child.QueueFree();
		}

		if (_model.Items.Count == 0)
		{
			_inventoryGrid.Columns = 1;
			var empty = CreateCenteredLabel(Text.EmptyInventory, 24);
			empty.SizeFlagsVertical = SizeFlags.ExpandFill;
			_inventoryGrid.AddChild(empty);
			return;
		}

		_inventoryGrid.Columns = 3;
		foreach (var item in _model.Items)
		{
			var selected = item.Id == _selectedItemId;
			var button = CreateInventoryButton(item, selected);
			var itemId = item.Id;
			button.Pressed += () =>
			{
				_selectedItemId = itemId;
				Refresh();
			};
			_inventoryGrid.AddChild(button);
		}
	}

	private void RefreshDetail()
	{
		var item = SelectedItem;
		if (item is null)
		{
			_detailIcon.Texture = LoadTexture(UiThemePaths.BlacksmithEnhanceIcon);
			_detailTitle.Text = Text.NoItemTitle;
			_detailMeta.Text = Text.NoItemMeta;
			_detailStats.Text = string.Empty;
			return;
		}

		_detailIcon.Texture = LoadTexture(item.IconPath);
		_detailTitle.Text = $"{item.DisplayName}  +{item.EnhancementLevel}";
		_detailMeta.Text = $"{item.Rarity} | {item.SlotLabel}\n{Text.Power} {item.EffectivePower}";
		var equipState = item.IsEquipped ? Text.Equipped : Text.NotEquipped;
		var lockState = item.IsLocked ? $" / {Text.Locked}" : string.Empty;
		var rangeState = item.IsWithinRecommendedLevel ? Text.LevelRangeOk : Text.LevelRangeDecayed;
		var modifiers = item.ModifierLines.Count == 0 ? Text.NoModifiers : string.Join("\n", item.ModifierLines);
		_detailStats.Text = $"{equipState}{lockState}\n{Text.EnhancementLevel} +{item.EnhancementLevel} / +{item.MaxEnhancementLevel}\n{Text.LevelRange} Lv.{item.RecommendedLevelMin}-{item.EffectiveRecommendedLevelMax}  {rangeState}\n{modifiers}";
	}

	private void RefreshOperation()
	{
		RefreshModeButton(_enhanceModeButton, UiThemePaths.BlacksmithEnhanceIcon, Text.EnhanceMode, _mode == BlacksmithMode.Enhance);
		RefreshModeButton(_extendModeButton, UiThemePaths.BlacksmithExtendIcon, Text.ExtendMode, _mode == BlacksmithMode.Extend);
		RefreshModeButton(_dismantleModeButton, UiThemePaths.BlacksmithDismantleIcon, Text.DismantleMode, _mode == BlacksmithMode.Dismantle);
		var item = SelectedItem;

		switch (_mode)
		{
			case BlacksmithMode.Enhance:
				RefreshEnhanceOperation(item);
				return;
			case BlacksmithMode.Extend:
				RefreshExtendOperation(item);
				return;
			default:
				RefreshDismantleOperation(item);
				return;
		}
	}

	private void RefreshEnhanceOperation(BlacksmithItemViewModel? item)
	{
		var cost = item is null ? 0 : BlacksmithRules.GetEnhancementCost(item.EnhancementLevel);
		var canRun = item is not null && item.EnhancementLevel < item.MaxEnhancementLevel && _model.Character.Gold >= cost;
		_operationIcon.Texture = LoadTexture(UiThemePaths.BlacksmithForgeIcon);
		_operationPreview.Text = item is null ? Text.SelectItemPrompt : string.Format(Text.EnhancePreviewFormat, item.EffectivePower, item.EffectivePower + 1);
		_operationHint.Text = item is null ? Text.EnhanceHint : canRun ? string.Format(Text.CostFormat, cost) : BuildEnhanceDisabledReason(item, cost);
		_actionButton.Text = Text.RunEnhance;
		_actionButton.Disabled = !canRun;
	}

	private void RefreshExtendOperation(BlacksmithItemViewModel? item)
	{
		var cost = item is null ? 0 : BlacksmithRules.GetLevelExtensionCost(item.LevelExtension);
		var canRun = item is not null && item.LevelExtension < item.MaxLevelExtension && _model.Character.Gold >= cost;
		_operationIcon.Texture = LoadTexture(UiThemePaths.BlacksmithExtendIcon);
		_operationPreview.Text = item is null ? Text.SelectItemPrompt : string.Format(Text.ExtendPreviewFormat, item.EffectiveRecommendedLevelMax, item.EffectiveRecommendedLevelMax + 1);
		_operationHint.Text = item is null ? Text.ExtendHint : canRun ? string.Format(Text.CostFormat, cost) : BuildExtendDisabledReason(item, cost);
		_actionButton.Text = Text.RunExtend;
		_actionButton.Disabled = !canRun;
	}

	private void RefreshDismantleOperation(BlacksmithItemViewModel? item)
	{
		var canRun = item is not null && item.EnhancementLevel > 0;
		var refund = item is null ? 0 : BlacksmithRules.GetDismantleRefund(item.EnhancementLevel);
		_operationIcon.Texture = LoadTexture(UiThemePaths.BlacksmithDismantleIcon);
		_operationPreview.Text = item is null ? Text.SelectItemPrompt : string.Format(Text.DismantlePreviewFormat, item.EnhancementLevel);
		_operationHint.Text = item is null ? Text.DismantleHint : canRun ? string.Format(Text.RefundFormat, refund) : Text.CannotDismantleZero;
		_actionButton.Text = Text.RunDismantle;
		_actionButton.Disabled = !canRun;
	}

	private BlacksmithItemViewModel? SelectedItem =>
		_model.Items.FirstOrDefault(item => item.Id == _selectedItemId) ?? _model.SelectedItem;

	private void RunSelectedAction()
	{
		var item = SelectedItem;
		if (item is null)
		{
			return;
		}

		switch (_mode)
		{
			case BlacksmithMode.Enhance:
				EnhanceRequested?.Invoke(item.Id);
				return;
			case BlacksmithMode.Extend:
				ExtendLevelRangeRequested?.Invoke(item.Id);
				return;
			default:
				DismantleRequested?.Invoke(item.Id);
				return;
		}
	}

	private string BuildEnhanceDisabledReason(BlacksmithItemViewModel item, int cost)
	{
		if (item.EnhancementLevel >= item.MaxEnhancementLevel)
		{
			return Text.MaxEnhancementReached;
		}

		return _model.Character.Gold < cost ? string.Format(Text.NotEnoughGoldFormat, cost) : string.Empty;
	}

	private string BuildExtendDisabledReason(BlacksmithItemViewModel item, int cost)
	{
		if (item.LevelExtension >= item.MaxLevelExtension)
		{
			return Text.MaxExtensionReached;
		}

		return _model.Character.Gold < cost ? string.Format(Text.NotEnoughGoldFormat, cost) : string.Empty;
	}

	private static Button CreateInventoryButton(BlacksmithItemViewModel item, bool selected)
	{
		var button = new Button { Text = string.Empty, CustomMinimumSize = new Vector2(0, 108) };
		DungeonFitUi.ApplyButton(button, selected ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		button.AddChild(center);
		var content = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
		content.AddThemeConstantOverride("separation", 1);
		center.AddChild(content);
		content.AddChild(DungeonFitUi.CreateIcon(item.IconPath, 62));
		var name = CreateNoWrapCenteredLabel(item.DisplayName, 15);
		name.CustomMinimumSize = new Vector2(104, 18);
		name.ClipText = true;
		content.AddChild(name);
		var level = CreateNoWrapCenteredLabel($"+{item.EnhancementLevel}  {item.Rarity}", 13);
		level.CustomMinimumSize = new Vector2(104, 16);
		level.ClipText = true;
		level.AddThemeColorOverride("font_color", selected ? new Color(1f, 0.86f, 0.52f) : new Color(0.85f, 0.72f, 1f));
		content.AddChild(level);
		return button;
	}

	private static Button CreateModeButton(string iconPath, string label)
	{
		var button = new Button { Text = string.Empty };
		DungeonFitUi.ApplyButton(button, UiButtonStyle.Secondary);
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		button.AddChild(center);
		var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 7);
		center.AddChild(row);
		row.AddChild(DungeonFitUi.CreateIcon(iconPath, 54));
		var text = new Label
		{
			Text = label,
			AutowrapMode = TextServer.AutowrapMode.Off,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		text.AddThemeFontSizeOverride("font_size", 22);
		row.AddChild(text);
		return button;
	}

	private static void RefreshModeButton(Button button, string iconPath, string label, bool selected)
	{
		DungeonFitUi.ApplyButton(button, selected ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
	}

	private static Texture2D? LoadTexture(string path)
	{
		return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : GD.Load<Texture2D>(UiThemePaths.BlacksmithForgeIcon);
	}

	private static void ApplyForgePanel(PanelContainer panel, Color background, Color border, int borderWidth)
	{
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = 7,
			CornerRadiusTopRight = 7,
			CornerRadiusBottomRight = 7,
			CornerRadiusBottomLeft = 7,
			ShadowColor = new Color(0.17f, 0.02f, 0.35f, 0.74f),
			ShadowSize = 5,
		});
	}

	private static MarginContainer CreateMargin(int horizontal, int vertical)
	{
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", horizontal);
		margin.AddThemeConstantOverride("margin_top", vertical);
		margin.AddThemeConstantOverride("margin_right", horizontal);
		margin.AddThemeConstantOverride("margin_bottom", vertical);
		return margin;
	}

	private static Label CreateLabel(string text, int fontSize)
	{
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		label.AddThemeFontSizeOverride("font_size", fontSize);
		return label;
	}

	private static Label CreateSectionLabel(string text, int fontSize)
	{
		var label = CreateLabel(text, fontSize);
		label.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.42f));
		return label;
	}

	private static Label CreateCenteredLabel(string text, int fontSize)
	{
		var label = CreateLabel(text, fontSize);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		return label;
	}

	private static Label CreateNoWrapCenteredLabel(string text, int fontSize)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.Off,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		return label;
	}

	private static Button CreateButton(string text, int width, int height, int fontSize, UiButtonStyle style)
	{
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(width, height) };
		button.AddThemeFontSizeOverride("font_size", fontSize);
		DungeonFitUi.ApplyButton(button, style);
		return button;
	}

	private static class Text
	{
		public const string BackShort = "\u8fd4\u56de";
		public const string Title = "\u9435\u5320\u92ea";
		public const string Subtitle = "\u6708\u9435\u4e4b\u7210  \u00b7  \u5f37\u5316  \u00b7  \u5ef6\u9577  \u00b7  \u5206\u89e3";
		public const string Description = "\u9078\u64c7\u88dd\u5099\uff0c\u8b93\u6bcf\u4e00\u6b21\u9304\u64ca\u90fd\u6210\u70ba\u66f4\u5f37\u7684\u6230\u529b\u3002";
		public const string Inventory = "\u88dd\u5099\u7bb1";
		public const string EnhanceMode = "\u5f37\u5316";
		public const string ExtendMode = "\u5ef6\u9577";
		public const string DismantleMode = "\u5206\u89e3";
		public const string EmptyInventory = "\u76ee\u524d\u6c92\u6709\u53ef\u4ee5\u6253\u9020\u7684\u88dd\u5099\u3002";
		public const string NoItemTitle = "\u9078\u64c7\u4e00\u4ef6\u88dd\u5099";
		public const string NoItemMeta = "\u5de6\u5074\u88dd\u5099\u683c\u6703\u986f\u793a\u7576\u524d\u64c1\u6709\u7684\u7269\u54c1\u3002";
		public const string Equipped = "\u88dd\u5099\u4e2d";
		public const string NotEquipped = "\u80cc\u5305\u4e2d";
		public const string Locked = "\u5df2\u4e0a\u9396";
		public const string Power = "\u653b\u64ca";
		public const string NoModifiers = "\u7121\u984d\u5916\u5c6c\u6027";
		public const string EnhancementLevel = "\u5f37\u5316";
		public const string LevelRange = "\u9069\u7528\u7b49\u7d1a";
		public const string LevelRangeOk = "\u9069\u7528";
		public const string LevelRangeDecayed = "\u7b49\u7d1a\u4e0d\u8db3";
		public const string SelectItemPrompt = "\u9078\u64c7\u4e00\u4ef6\u88dd\u5099\u4f86\u958b\u59cb\u934a\u9020\u3002";
		public const string EnhancePreviewFormat = "\u76ee\u524d  \u653b\u64ca +{0}    \u2192    \u5f37\u5316\u5f8c  \u653b\u64ca +{1}";
		public const string EnhanceHint = "\u5f37\u5316\u6703\u63d0\u9ad8\u6b66\u5668\u6216\u9632\u5177\u7684\u6230\u529b\u3002";
		public const string ExtendPreviewFormat = "\u76ee\u524d\u9069\u7528 Lv.{0}    \u2192    \u5ef6\u9577\u81f3 Lv.{1}";
		public const string ExtendHint = "\u5ef6\u9577\u88dd\u5099\u7684\u9069\u7528\u7b49\u7d1a\u3002";
		public const string CostFormat = "\u6d88\u8017\uff1a{0} Gold";
		public const string RunEnhance = "\u9032\u884c\u5f37\u5316";
		public const string RunExtend = "\u5ef6\u9577\u7b49\u7d1a";
		public const string DismantlePreviewFormat = "\u5f37\u5316 +{0}    \u2192    \u5206\u89e3\u56de\u5f37\u5316 +0";
		public const string DismantleHint = "\u5206\u89e3\u6703\u8fd4\u9084\u4e00\u90e8\u5206\u5f37\u5316\u8cbb\u7528\u3002";
		public const string RefundFormat = "\u8fd4\u9084\uff1a{0} Gold";
		public const string CannotDismantleZero = "\u9019\u4ef6\u88dd\u5099\u5c1a\u672a\u5f37\u5316\u3002";
		public const string RunDismantle = "\u9032\u884c\u5206\u89e3";
		public const string ReturnTown = "\u8fd4\u56de\u57ce\u93ae";
		public const string MaxEnhancementReached = "\u5df2\u9054\u5f37\u5316\u4e0a\u9650\u3002";
		public const string MaxExtensionReached = "\u5df2\u9054\u5ef6\u9577\u4e0a\u9650\u3002";
		public const string NotEnoughGoldFormat = "Gold \u4e0d\u8db3\uff0c\u9700\u8981 {0} Gold\u3002";
	}
}
