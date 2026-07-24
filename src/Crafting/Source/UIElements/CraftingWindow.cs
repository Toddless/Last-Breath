namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Results;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The one crafting screen, composed after the bench layout of the crafting UI kit:
    /// recipe tree with search on the left; on the right a mode tab row (Create / Upgrade /
    /// Reroll / Ascend), a bench of result preview (icon, name, modifier lines — recipe results
    /// render their blueprint descriptors with value spreads) next to requirement/additive slot
    /// cards, and an action bar at the bottom. Reads go straight to the services; only the
    /// crafting COMMANDS travel the bus. Cards and rows are code-built — the scene holds
    /// containers, not slots.
    /// </summary>
    public partial class CraftingWindow : Control, IWindow
    {
        private const string UID = "uid://betq124kfglyy";
        private const int AdditiveSlots = 3;
        private static readonly Vector2 s_cardIconSize = new(46, 46);
        private static readonly Vector2 s_cardMinSize = new(0, 130);

        [Export] private Tree? _tree;
        [Export] private LineEdit? _search;
        [Export] private Button? _close, _actionButton;
        [Export] private Button? _modeCreate, _modeUpgrade, _modeRecraft, _modeAscend;
        [Export] private TextureRect? _itemIcon;
        [Export] private Label? _title, _listTitle, _itemName, _itemSubtitle, _previewTag, _requirementsHeader, _additivesHeader, _ascendWarning;
        [Export] private VBoxContainer? _mods;
        [Export] private GridContainer? _requirements, _additives;
        [Export] private Control? _chanceBox;
        [Export] private ProgressBar? _chanceBar;
        [Export] private Label? _chanceLabel;
        [Export] private Label? _masteryLevel, _masteryXpLabel, _masteryTitle, _forecastHeader, _forecastHint;
        [Export] private ProgressBar? _masteryXpBar;
        [Export] private GridContainer? _masteryChips;
        [Export] private Control? _forecastBox;
        [Export] private VBoxContainer? _forecastRows;

        private readonly Dictionary<string, string> _categoryChoices = [];
        private readonly string?[] _additiveChoices = new string?[AdditiveSlots];
        private ResourcePickerPopup? _pickerPopup;

        private IItemDataProvider? _dataProvider;
        private IInventory? _inventory;
        private ICraftingMastery? _mastery;
        private IItemUpgrader? _upgrader;
        private IItemAscender? _ascender;
        private ICraftingAdditiveProvider? _additiveProvider;
        private IGameMessageBus? _messageBus;
        private IUiElementsManager? _uiElements;

        private CraftingMode _mode = CraftingMode.Create;
        private string? _recipeId;
        private IEquipItem? _item;
        private string? _selectedModifierInstanceId;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            _tree?.ItemSelected += OnRecipeSelected;
            _search?.TextChanged += _ => BuildTree();
            _close?.Pressed += Close;
            _actionButton?.Pressed += OnActionPressed;

            _modeCreate?.Pressed += () => SwitchMode(CraftingMode.Create);
            _modeUpgrade?.Pressed += () => SwitchMode(CraftingMode.Upgrade);
            _modeRecraft?.Pressed += () => SwitchMode(CraftingMode.Recraft);
            _modeAscend?.Pressed += () => SwitchMode(CraftingMode.Ascend);

            LocalizeStaticLabels();
        }

        public override void _ExitTree()
        {
            // The picker lives in the Overlay layer — a window closed by hotkey must not orphan it.
            ClosePicker();
            if (_inventory != null) _inventory.ItemAmountChanges -= OnItemAmountChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _dataProvider = provider.GetService<IItemDataProvider>();
            _inventory = provider.GetService<IInventory>();
            _mastery = provider.GetService<ICraftingMastery>();
            _upgrader = provider.GetService<IItemUpgrader>();
            _ascender = provider.GetService<IItemAscender>();
            _additiveProvider = provider.GetService<ICraftingAdditiveProvider>();
            _messageBus = provider.GetService<IGameMessageBus>();
            _uiElements = provider.GetService<IUiElementsManager>();

            _inventory.ItemAmountChanges += OnItemAmountChanged;
            BuildTree();
            RefreshDetails();
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Entry point for item operations (inventory tooltip buttons and the like).</summary>
        public void SetItem(IEquipItem item, CraftingMode mode)
        {
            _item = item;
            _mode = mode;
            _recipeId = null;
            _selectedModifierInstanceId = null;
            ResetChoices();
            _tree?.DeselectAll();
            RefreshDetails();
        }

        private void LocalizeStaticLabels()
        {
            _title?.Text = Localization.Localize("UI_Crafting");
            _requirementsHeader?.Text = Localization.Localize("UI_Craft_Requirements");
            _additivesHeader?.Text = Localization.Localize("UI_Craft_Additives");
            _ascendWarning?.Text = Localization.Localize("UI_Craft_AscendWarning");
            _modeCreate?.Text = Localization.Localize("UI_Crafting_Create");
            _modeUpgrade?.Text = Localization.Localize("UI_Crafting_Upgrade");
            _modeRecraft?.Text = Localization.Localize("UI_Crafting_Recraft");
            _modeAscend?.Text = Localization.Localize("UI_Crafting_Ascend");
            _masteryTitle?.Text = Localization.Localize("UI_Craft_Mastery").ToUpper();
            _forecastHeader?.Text = Localization.Localize("UI_Craft_Forecast").ToUpper();
            _forecastHint?.Text = Localization.Localize("UI_Craft_Forecast_Hint");
        }

        // ---------------------------------------------------------------- mode tabs

        /// <summary>Create always leads back to recipe browsing; the item modes need an item on the
        /// bench (tabs are disabled otherwise, this is the guard for hotkey/race paths).</summary>
        private void SwitchMode(CraftingMode mode)
        {
            if (mode != CraftingMode.Create && _item == null || _mode == mode) { RefreshModeTabs(); return; }

            _mode = mode;
            if (mode == CraftingMode.Create) _item = null;
            _selectedModifierInstanceId = null;
            ResetChoices();
            RefreshDetails();
        }

        private void RefreshModeTabs()
        {
            SyncTab(_modeCreate, CraftingMode.Create, enabled: true);
            SyncTab(_modeUpgrade, CraftingMode.Upgrade, _item != null);
            SyncTab(_modeRecraft, CraftingMode.Recraft, _item != null);
            SyncTab(_modeAscend, CraftingMode.Ascend, _item != null);
        }

        private void SyncTab(Button? tab, CraftingMode mode, bool enabled)
        {
            if (tab == null) return;
            tab.Disabled = !enabled;
            tab.SetPressedNoSignal(_mode == mode);
        }

        // ---------------------------------------------------------------- tree

        private void BuildTree()
        {
            if (_tree == null || _dataProvider == null) return;

            string query = _search?.Text?.Trim() ?? string.Empty;
            _tree.Clear();
            _tree.HideRoot = true;
            var root = _tree.CreateItem();
            int totalShown = 0;

            foreach (var group in _dataProvider.GetCraftingRecipes()
                         .GroupBy(RecipeCategory)
                         .OrderBy(entry => entry.Key.ToString()))
            {
                var matching = group
                    .Where(recipe => query.Length == 0 || Localization.Localize(recipe.Id).Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matching.Count == 0) continue;

                var category = _tree.CreateItem(root);
                category.SetText(0, Localization.Localize(group.Key.ToString()));
                category.SetSelectable(0, false);
                totalShown += matching.Count;

                foreach (var recipe in matching)
                {
                    var entry = _tree.CreateItem(category);
                    int amount = CraftableAmount(recipe.Id);
                    entry.SetText(0, amount > 0 ? $"{Localization.Localize(recipe.Id)} ({amount})" : Localization.Localize(recipe.Id));
                    entry.SetMetadata(0, recipe.Id);
                    entry.SetSelectable(0, recipe.IsOpened);
                }
            }

            _listTitle?.Text = $"{Localization.Localize("UI_Craft_Recipes")} ({totalShown})";
        }

        private static RecipeCategories RecipeCategory(ICraftingRecipe recipe) =>
            Enum.GetValues<RecipeCategories>()
                .FirstOrDefault(category => recipe.Tags.Contains(category.ToString(), StringComparer.OrdinalIgnoreCase));

        private int CraftableAmount(string recipeId)
        {
            var requirements = _dataProvider?.GetRecipeRequirements(recipeId) ?? [];
            int amount = int.MaxValue;
            foreach (var requirement in requirements)
            {
                int owned = requirement.Type switch
                {
                    RequirementType.Resource => _inventory?.GetTotalItemAmount(requirement.Id) ?? 0,
                    // The optimistic ceiling: any owned member of the category counts toward the slot.
                    RequirementType.ResourceCategory => CategoryOwnedTotal(requirement.Id),
                    _ => -1,
                };
                if (owned < 0) continue;
                amount = Math.Min(amount, owned / requirement.Amount);
            }

            return amount == int.MaxValue ? 0 : amount;
        }

        private int CategoryOwnedTotal(string categoryId)
        {
            var members = _dataProvider?.GetResourceIdsInCategory(categoryId) ?? [];
            var ownedIds = members.Count > 0 ? members : _inventory?.GetAllItemIdsWithTag(categoryId) ?? [];
            return ownedIds.Distinct().Sum(id => _inventory?.GetTotalItemAmount(id) ?? 0);
        }

        private void OnRecipeSelected()
        {
            string recipeId = _tree?.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            if (recipeId.Length == 0 || recipeId == _recipeId) return;

            _recipeId = recipeId;
            _item = null;
            _mode = CraftingMode.Create;
            _selectedModifierInstanceId = null;
            ResetChoices();
            RefreshDetails();
        }

        // ---------------------------------------------------------------- details

        private void RefreshDetails()
        {
            ClosePicker();
            RefreshModeTabs();
            RenderMasteryRail();
            RenderItemHeader();
            RenderModifiers();
            RenderRequirements();
            RenderAdditives();
            RenderForecast();
            RenderUpgradeChance();
            RenderAction();
        }

        /// <summary>The kit's success-chance bar in the action bar, Upgrade mode only. Lives inside
        /// RefreshDetails, so it updates live with every additive pick and after every attempt (the
        /// level moved — the curve point moved). A capped item shows no bar at all, mirroring the
        /// disabled action button. Same resource set as the actual roll — one formula, one place.</summary>
        private void RenderUpgradeChance()
        {
            bool visible = _mode == CraftingMode.Upgrade && _upgrader != null
                && _item is { } item && item.UpdateLevel < item.MaxUpdateLevel;
            _chanceBox?.Visible = visible;
            if (!visible) return;

            float chance = _upgrader!.GetUpgradeChance(_item!, BuildCost().Keys);
            _chanceBar?.Value = chance;
            _chanceLabel?.Text = Localization.Render("UI_Craft_Success_Chance",
                new Dictionary<string, object?> { ["Chance"] = chance });
        }

        // ---------------------------------------------------------------- mastery rail and forecast

        /// <summary>The always-visible mastery strip over the mode tabs (kit's mastery rail):
        /// level with the XP progress to the next one, then the six bonus channels as chips.
        /// Every channel lerps on the same level factor, so each chip's mini bar IS that factor.</summary>
        private void RenderMasteryRail()
        {
            if (_mastery == null || _masteryChips == null) return;

            _masteryLevel?.Text = _mastery.BonusLevel > 0
                ? $"{_mastery.CurrentLevel}+{_mastery.BonusLevel} / {_mastery.MaximumLevel}"
                : $"{_mastery.CurrentLevel} / {_mastery.MaximumLevel}";

            int expTotal = _mastery.ExpToNextLevelTotal();
            _masteryXpBar?.Value = expTotal > 0 ? _mastery.CurrentExperience / (float)expTotal : 1f;
            _masteryXpLabel?.Text = expTotal > 0
                ? $"{_mastery.CurrentExperience} / {expTotal}"
                : Localization.Localize("UI_Mastery_Max");

            foreach (var child in _masteryChips.GetChildren())
                child.QueueFree();

            float progress = _mastery.MaximumLevel <= 0
                ? 0f
                : Mathf.Clamp((_mastery.CurrentLevel + _mastery.BonusLevel) / (float)_mastery.MaximumLevel, 0f, 1f);
            foreach ((string key, float bonus) in MasteryChannels())
                _masteryChips.AddChild(MasteryChip(Localization.Localize(key), bonus, progress));
        }

        private IEnumerable<(string Key, float Bonus)> MasteryChannels() =>
        [
            ("UI_Mastery_Channel_Upgrade", _mastery!.GetUpgradeChanceBonus()),
            ("UI_Mastery_Channel_Values", _mastery.GetCurrentValueMultiplier() - 1f),
            ("UI_Mastery_Channel_Rarity", _mastery.GetRarityChanceBonus()),
            ("UI_Mastery_Channel_Effect", _mastery.GetExtraEffectChanceBonus()),
            ("UI_Mastery_Channel_Mythic", _mastery.GetMythicModifierChanceBonus()),
            ("UI_Mastery_Channel_Salvage", _mastery.GetResourceReturnBonus()),
        ];

        private static Control MasteryChip(string name, float bonus, float progress)
        {
            var chip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 8);
            margin.AddThemeConstantOverride("margin_top", 6);
            margin.AddThemeConstantOverride("margin_right", 8);
            margin.AddThemeConstantOverride("margin_bottom", 6);
            chip.AddChild(margin);

            var content = new VBoxContainer();
            content.AddThemeConstantOverride("separation", 2);
            margin.AddChild(content);

            var title = new Label
            {
                Text = name,
                ThemeTypeVariation = "DimLabel",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            title.AddThemeFontSizeOverride("font_size", 10);
            content.AddChild(title);

            var value = new Label { Text = $"+{PercentText(bonus)}" };
            value.AddThemeFontSizeOverride("font_size", 16);
            content.AddChild(value);

            content.AddChild(new ProgressBar
            {
                CustomMinimumSize = new Vector2(0, 3),
                MaxValue = 1.0,
                Step = 0.001,
                Value = progress,
                ShowPercentage = false,
            });
            return chip;
        }

        /// <summary>The kit's craft forecast under the resource slots: for the current operation the
        /// abstract mastery channels turn into concrete odds — data base struck through, the real
        /// chance (mastery + chosen additives, the exact numbers the handlers roll) next to it.
        /// Only lines the mode actually rolls are shown; a reroll is deterministic — no box.</summary>
        private void RenderForecast()
        {
            if (_forecastBox == null || _forecastRows == null) return;
            foreach (var child in _forecastRows.GetChildren())
                child.QueueFree();

            var lines = ForecastLines();
            _forecastBox.Visible = lines.Count > 0;
            foreach ((string key, string value) in lines)
                _forecastRows.AddChild(ForecastRow(Localization.Localize(key), value));
        }

        private List<(string Key, string Value)> ForecastLines()
        {
            if (_mastery == null) return [];
            return _mode switch
            {
                CraftingMode.Upgrade when _item is { } item && item.UpdateLevel < item.MaxUpdateLevel && _upgrader != null =>
                    UpgradeForecast(item),
                CraftingMode.Create when _recipeId != null => CreateForecast(),
                CraftingMode.Ascend when _item != null && _ascender?.CanAscend(_item) == true => AscendForecast(),
                _ => [],
            };
        }

        private List<(string, string)> UpgradeForecast(IEquipItem item)
        {
            var lines = new List<(string, string)>
            {
                ("UI_Mastery_Channel_Upgrade",
                    WasNow(_upgrader!.GetBaseUpgradeChance(item), _upgrader.GetUpgradeChance(item, BuildCost().Keys))),
            };

            float extraLevel = _additiveChoices.Where(id => id != null)
                .Sum(id => _additiveProvider?.GetEffects(id!)?.ExtraUpgradeLevelChance ?? 0f);
            if (extraLevel > 0f)
                lines.Add(("UI_Forecast_Extra_Level", Highlight(PercentText(extraLevel))));
            return lines;
        }

        private List<(string, string)> CreateForecast()
        {
            float effectNow = _mastery!.GetExtraEffectChance();
            float effectBase = effectNow / (1f + _mastery.GetExtraEffectChanceBonus());
            var lines = new List<(string, string)>
            {
                ("UI_Mastery_Channel_Values", Highlight($"×{_mastery.GetCurrentValueMultiplier():0.00}")),
                ("UI_Mastery_Channel_Effect", WasNow(effectBase, effectNow)),
            };

            var floorRarity = _additiveChoices.Where(id => id != null)
                .Select(id => _additiveProvider?.GetEffects(id!)?.MinRarity)
                .Where(rarity => rarity != null)
                .OrderBy(rarity => rarity!.Value) // lower enum value = rarer; the best floor wins
                .FirstOrDefault();
            if (floorRarity != null)
                lines.Add(("UI_Forecast_Min_Rarity",
                    $"[color={TextPalette.RarityColor(floorRarity.Value)}]{Localization.Localize(floorRarity.Value.ToString())}[/color]"));
            return lines;
        }

        private List<(string, string)> AscendForecast() =>
            [("UI_Mastery_Channel_Mythic",
                WasNow(_mastery!.GetMythicGiftChance() / (1f + _mastery.GetMythicModifierChanceBonus()), _mastery.GetMythicGiftChance()))];

        private static Control ForecastRow(string name, string valueBbcode)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(new Label
            {
                Text = name,
                ThemeTypeVariation = "DimLabel",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
            row.AddChild(new RichTextLabel
            {
                Text = valueBbcode,
                BbcodeEnabled = true,
                FitContent = true,
                AutowrapMode = TextServer.AutowrapMode.Off,
                CustomMinimumSize = new Vector2(130, 0),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });
            return row;
        }

        private static string WasNow(float before, float now) =>
            $"[s][color={TextPalette.Muted}]{PercentText(before)}[/color][/s] → {Highlight(PercentText(now))}";

        private static string Highlight(string text) => $"[color={TextPalette.Number}]{text}[/color]";

        /// <summary>Sub-10% chances keep one decimal, so early mastery does not round to a flat lie.</summary>
        private static string PercentText(float value) =>
            (value * 100f).ToString(value < 0.095f ? "0.#" : "0", System.Globalization.CultureInfo.InvariantCulture) + "%";

        private EquipItemBlueprint? SelectedBlueprint()
        {
            if (_recipeId == null || _dataProvider == null) return null;
            string resultId = _dataProvider.GetRecipeResultItemId(_recipeId);
            return resultId.Length == 0 ? null : _dataProvider.GetBlueprint(resultId);
        }

        private void RenderItemHeader()
        {
            if (_item != null)
            {
                _itemIcon?.Texture = _item.Icon;
                SetItemName(_item.DisplayName, _item.Rarity);
                _itemSubtitle?.Text = $"{Localization.Localize(_item.Rarity.ToString())}   +{_item.UpdateLevel} / {_item.MaxUpdateLevel}";
                return;
            }

            string resultId = _recipeId == null ? string.Empty : _dataProvider?.GetRecipeResultItemId(_recipeId) ?? string.Empty;
            var blueprint = SelectedBlueprint();
            _itemIcon?.Texture = resultId.Length > 0 ? _dataProvider?.GetItemIcon(resultId) : null;
            SetItemName(
                _recipeId == null ? Localization.Localize("UI_Craft_PickRecipe") : Localization.Localize(_recipeId),
                blueprint?.Rarity);
            _itemSubtitle?.Text = blueprint == null ? string.Empty : Localization.Localize(blueprint.Rarity.ToString());
        }

        private void SetItemName(string text, Rarity? rarity)
        {
            if (_itemName == null) return;
            _itemName.Text = text;
            if (rarity == null) _itemName.RemoveThemeColorOverride("font_color");
            else _itemName.AddThemeColorOverride("font_color", Color.FromHtml(TextPalette.RarityColor(rarity.Value)));
        }

        private void RenderModifiers()
        {
            if (_mods == null) return;
            foreach (var child in _mods.GetChildren())
                child.QueueFree();

            var blueprint = _item == null ? SelectedBlueprint() : null;
            bool visible = _item != null || blueprint != null;
            _previewTag?.Visible = visible;
            _mods.Visible = visible;
            _previewTag?.Text = Localization.Localize(_item != null ? "UI_Craft_Modifiers" : "UI_Craft_Result");

            if (_item != null) RenderItemModifiers(_item);
            else if (blueprint != null) RenderBlueprintModifiers(blueprint);
        }

        /// <summary>Live item lines: parts of one composite roll present as a single row (the row's
        /// id is the first part — the group-reroll target). The rolled rows arrive grouped by slot family
        /// (prefixes, suffixes, leftovers, the ascension gift) and each family announces itself — the bench
        /// shows the same blocks as the tooltip, just without its framing.</summary>
        private void RenderItemModifiers(IEquipItem item)
        {
            foreach (var line in EquipItemLines.ComposeImplicits(item))
                _mods?.AddChild(new Label { Text = line.Text, ThemeTypeVariation = "DimLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart });

            AffixKind? block = null;
            foreach (var line in EquipItemLines.ComposeRolled(item))
            {
                if (line.Affix != block)
                {
                    block = line.Affix;
                    var header = ItemLineRows.AffixHeader(line.Affix);
                    if (header != null) _mods?.AddChild(header);
                }

                _mods?.AddChild(RerollableOrLabel(line.InstanceId, line.Text));
            }

            foreach (var grant in item.Grants)
                _mods?.AddChild(new Label { Text = Localization.Localize(grant.Id), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }

        /// <summary>Recipe result preview straight from the blueprint: descriptors with value spreads
        /// render through the range templates ("+40–60 Strength"), fixed lines render as usual.</summary>
        private void RenderBlueprintModifiers(EquipItemBlueprint blueprint)
        {
            foreach (var descriptor in blueprint.Implicits)
                _mods?.AddChild(new Label { Text = Localization.Format(descriptor), ThemeTypeVariation = "DimLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart });

            foreach (var descriptor in blueprint.Modifiers)
                _mods?.AddChild(new Label { Text = Localization.Format(descriptor), AutowrapMode = TextServer.AutowrapMode.WordSmart });

            foreach (var grant in blueprint.Grants)
                _mods?.AddChild(new Label { Text = Localization.Localize(grant.Id), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }

        /// <summary>In Recraft every additional row — entity, context or grouped — is pickable; the chosen one gets rerolled.</summary>
        private Control RerollableOrLabel(string instanceId, string text) =>
            _mode == CraftingMode.Recraft
                ? RerollableRow(instanceId, text)
                : new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };

        private Button RerollableRow(string instanceId, string text)
        {
            var row = new Button
            {
                Text = (_selectedModifierInstanceId == instanceId ? "» " : string.Empty) + text,
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,
                ClipText = true,
            };
            row.Pressed += () =>
            {
                _selectedModifierInstanceId = instanceId;
                RefreshDetails();
            };
            return row;
        }

        // ---------------------------------------------------------------- requirement / additive cards

        private void RenderRequirements()
        {
            if (_requirements == null) return;
            foreach (var child in _requirements.GetChildren())
                child.QueueFree();

            foreach (var requirement in CurrentRequirements())
            {
                if (requirement.Type == RequirementType.ResourceCategory)
                    _requirements.AddChild(CategoryCard(requirement));
                else
                    _requirements.AddChild(StaticCard(requirement));
            }

            _ascendWarning?.Visible = _mode == CraftingMode.Ascend && _item != null;
        }

        private Control StaticCard(Core.Interfaces.IRequirement requirement)
        {
            int have = requirement.Type == RequirementType.MasteryLevel
                ? _mastery?.CurrentLevel ?? 0
                : _inventory?.GetTotalItemAmount(requirement.Id) ?? 0;
            var icon = requirement.Type == RequirementType.Resource ? _dataProvider?.GetItemIcon(requirement.Id) : null;

            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = s_cardMinSize };
            card.AddChild(CardContent(icon, Localization.Localize(requirement.Id), $"{have} / {requirement.Amount}", have >= requirement.Amount));
            return card;
        }

        /// <summary>A category requirement is a slot: click → pick a concrete resource carrying the tag.</summary>
        private Control CategoryCard(Core.Interfaces.IRequirement requirement)
        {
            bool chosen = _categoryChoices.TryGetValue(requirement.Id, out string? resourceId);
            int have = chosen ? _inventory?.GetTotalItemAmount(resourceId!) ?? 0 : 0;

            var card = SlotButton();
            card.AddChild(chosen
                ? CardContent(_dataProvider?.GetItemIcon(resourceId!), Localization.Localize(resourceId!), $"{have} / {requirement.Amount}", have >= requirement.Amount)
                : CardContent(null, $"{Localization.Localize("UI_Craft_Choose")}: {Localization.Localize(requirement.Id)}", null, countMet: true));
            card.Pressed += () => OpenPicker(
                Localization.Localize(requirement.Id),
                CategoryResourceIds(requirement.Id),
                picked =>
                {
                    _categoryChoices[requirement.Id] = picked;
                    RefreshDetails();
                });
            return card;
        }

        /// <summary>Owned candidates of one category slot. Membership comes from the resources'
        /// material data when the id names a real category; legacy bare-tag requirements fall back
        /// to the inventory tag lookup.</summary>
        private List<string> CategoryResourceIds(string categoryId)
        {
            var members = _dataProvider?.GetResourceIdsInCategory(categoryId) ?? [];
            var owned = members.Count > 0
                ? members.Where(id => (_inventory?.GetTotalItemAmount(id) ?? 0) > 0)
                : _inventory?.GetAllItemIdsWithTag(categoryId) ?? [];
            return owned
                .Distinct()
                .Where(id => !_categoryChoices.ContainsValue(id))
                .ToList();
        }

        private void RenderAdditives()
        {
            if (_additives == null) return;
            foreach (var child in _additives.GetChildren())
                child.QueueFree();

            // Nothing to offer while the CraftingAdditives catalog is empty — hide the block entirely.
            bool visible = (_additiveProvider?.KnownAdditiveIds.Count ?? 0) > 0 && (_item != null || _recipeId != null);
            _additivesHeader?.Visible = visible;
            _additives.Visible = visible;
            if (!visible) return;

            for (int slot = 0; slot < AdditiveSlots; slot++)
            {
                int index = slot;
                string? choice = _additiveChoices[slot];
                var card = SlotButton();
                card.AddChild(choice == null
                    ? CardContent(null, "+", null, countMet: true)
                    : CardContent(_dataProvider?.GetItemIcon(choice), Localization.Localize(choice), null, countMet: true));
                card.Pressed += () =>
                {
                    if (_additiveChoices[index] != null)
                    {
                        _additiveChoices[index] = null; // click on a filled slot empties it
                        RefreshDetails();
                        return;
                    }

                    OpenPicker(Localization.Localize("UI_Craft_Additives"), AvailableAdditiveIds(), picked =>
                    {
                        _additiveChoices[index] = picked;
                        RefreshDetails();
                    });
                };
                _additives.AddChild(card);
            }
        }

        /// <summary>Clickable slot card: the button is the frame, the content overlays it mouse-transparently.</summary>
        private static Button SlotButton() => new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = s_cardMinSize,
            FocusMode = FocusModeEnum.None,
        };

        /// <summary>Vertical slot-card content after the kit's resource slot: icon, name, have/need.</summary>
        private static Control CardContent(Texture2D? icon, string name, string? count, bool countMet)
        {
            var content = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            content.SetAnchorsPreset(LayoutPreset.FullRect);
            content.AddThemeConstantOverride("separation", 4);

            if (icon != null)
            {
                content.AddChild(new TextureRect
                {
                    Texture = icon,
                    CustomMinimumSize = s_cardIconSize,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    MouseFilter = MouseFilterEnum.Ignore,
                });
            }

            content.AddChild(new Label
            {
                Text = name,
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            });

            if (count != null)
            {
                content.AddChild(new Label
                {
                    Text = count,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ThemeTypeVariation = countMet ? null : "DimLabel",
                    MouseFilter = MouseFilterEnum.Ignore,
                });
            }

            return content;
        }

        /// <summary>Catalog additives that actually DO something in the current mode, plus plain
        /// essences (resources with own descriptors) where the operation's pool consumes them
        /// (creation and reroll). A rarity-floor rune in a reroll used to show up and silently eat
        /// resources for nothing — the picker now mirrors the domain's honest scope.</summary>
        private List<string> AvailableAdditiveIds() =>
            (_additiveProvider?.KnownAdditiveIds ?? []).Where(AdditiveServesMode)
            .Concat(_mode is CraftingMode.Create or CraftingMode.Recraft ? EssenceResourceIds() : [])
            .Distinct()
            .Where(id => (_inventory?.GetTotalItemAmount(id) ?? 0) > 0 && !_additiveChoices.Contains(id))
            .ToList();

        /// <summary>Which catalog effects matter where: sharpening bonuses in Upgrade, rarity
        /// floors in Create, per-operation pools in Recraft. Ascension takes no additives.</summary>
        private bool AdditiveServesMode(string id)
        {
            var effects = _additiveProvider?.GetEffects(id);
            if (effects == null) return false;
            return _mode switch
            {
                CraftingMode.Upgrade => effects.UpgradeChanceBonus > 0f || effects.ExtraUpgradeLevelChance > 0f,
                CraftingMode.Create => effects.MinRarity != null,
                CraftingMode.Recraft => !string.IsNullOrEmpty(effects.RecraftPoolId),
                _ => false,
            };
        }

        private const string EssenceCategoryId = "Category_Essence";

        /// <summary>Essences: resources of the essence category carrying their own descriptors.
        /// Ores/hides also feed pools, but they live in the RECIPE slots — offering them here too
        /// would flood the picker without adding a choice the recipe doesn't already give.</summary>
        private IEnumerable<string> EssenceResourceIds() =>
            (_dataProvider?.GetAllResources() ?? [])
                .OfType<Core.Crafting.ICraftingResource>()
                .Where(resource => resource.Material?.MaterialCategory?.Id == EssenceCategoryId && resource.Material.Modifiers.Count > 0)
                .Select(resource => resource.Id);

        private void RenderAction()
        {
            if (_actionButton == null) return;

            _actionButton.Text = Localization.Localize(_mode switch
            {
                CraftingMode.Upgrade => "UI_Craft_Upgrade",
                CraftingMode.Recraft => "UI_Craft_Reroll",
                CraftingMode.Ascend => "UI_Craft_Ascend",
                _ => "UI_Craft_Create",
            });
            _actionButton.Disabled = !CanExecute();
        }

        // ---------------------------------------------------------------- costs and execution

        private List<Core.Interfaces.IRequirement> CurrentRequirements()
        {
            if (_mode == CraftingMode.Create)
                return _recipeId == null ? [] : (_dataProvider?.GetRecipeRequirements(_recipeId) ?? []).ToList();
            if (_item == null || _upgrader == null || _ascender == null) return [];

            var category = _item.EquipmentPiece.ConvertEquipmentPartToCategory();
            return _mode switch
            {
                CraftingMode.Upgrade => _upgrader.GetUpgradeResourceCost(_item.Rarity, category),
                // Item-aware price: includes the growing-reroll multiplier, so the cards show the
                // ACTUAL next-reroll cost (RefreshDetails after each reroll re-reads it).
                CraftingMode.Recraft => _upgrader.GetRecraftResourceCost(_item),
                CraftingMode.Ascend => _ascender.GetAscendResourceCost(category),
                _ => [],
            };
        }

        /// <summary>The mandatory part of the cost: fixed resources plus chosen category resources.</summary>
        private Dictionary<string, int> BuildRequiredCost()
        {
            var cost = new Dictionary<string, int>();
            foreach (var requirement in CurrentRequirements())
            {
                switch (requirement.Type)
                {
                    case RequirementType.Resource:
                        cost[requirement.Id] = cost.GetValueOrDefault(requirement.Id) + requirement.Amount;
                        break;
                    case RequirementType.ResourceCategory when _categoryChoices.TryGetValue(requirement.Id, out string? chosen):
                        cost[chosen] = cost.GetValueOrDefault(chosen) + requirement.Amount;
                        break;
                }
            }

            return cost;
        }

        /// <summary>The optional part: whatever sits in the additive slots.</summary>
        private Dictionary<string, int> BuildOptionalCost()
        {
            var cost = new Dictionary<string, int>();
            foreach (string? additive in _additiveChoices)
                if (additive != null)
                    cost[additive] = cost.GetValueOrDefault(additive) + 1;
            return cost;
        }

        /// <summary>Everything the operation consumes, merged — for availability checks and the
        /// single-map item operations (upgrade/recraft).</summary>
        private Dictionary<string, int> BuildCost()
        {
            var cost = BuildRequiredCost();
            foreach ((string id, int amount) in BuildOptionalCost())
                cost[id] = cost.GetValueOrDefault(id) + amount;
            return cost;
        }

        private bool CanExecute()
        {
            if (_inventory == null) return false;

            bool costCovered = BuildCost().All(pair => _inventory.GetTotalItemAmount(pair.Key) >= pair.Value);
            return _mode switch
            {
                CraftingMode.Create => _recipeId != null && costCovered && MasteryAllows() && AllCategoriesChosen(),
                CraftingMode.Upgrade => _item != null && _item.UpdateLevel < _item.MaxUpdateLevel && costCovered,
                CraftingMode.Recraft => _item != null && _selectedModifierInstanceId != null && costCovered,
                CraftingMode.Ascend => _item != null && _ascender?.CanAscend(_item) == true && costCovered,
                _ => false,
            };
        }

        private bool MasteryAllows() =>
            CurrentRequirements().Where(requirement => requirement.Type == RequirementType.MasteryLevel)
                .All(requirement => (_mastery?.CurrentLevel ?? 0) >= requirement.Amount);

        private bool AllCategoriesChosen() =>
            CurrentRequirements().Where(requirement => requirement.Type == RequirementType.ResourceCategory)
                .All(requirement => _categoryChoices.ContainsKey(requirement.Id));

        private async void OnActionPressed()
        {
            try
            {
                if (_messageBus == null || !CanExecute()) return;
                var cost = BuildCost();

                switch (_mode)
                {
                    case CraftingMode.Create:
                        // The split travels to the item: the window knows which resources came from
                        // requirement cards and which from additive slots.
                        await _messageBus.SendRequest<CreateEquipItemRequest, IEquipItem?>(new(_recipeId!, BuildRequiredCost(), BuildOptionalCost()));
                        break;
                    case CraftingMode.Upgrade:
                        await _messageBus.SendRequest<UpgradeEquipItemRequest, ItemUpgradeResult>(new(_item!.InstanceId, cost));
                        break;
                    case CraftingMode.Recraft:
                        // Only the additives travel: the handler recomputes and spends the mandatory
                        // price itself — the window's requirement cards are a mirror, not the source.
                        await _messageBus.SendRequest<RecraftEquipItemModifierRequest, RequestResult<string>>(
                            new(_item!.InstanceId, _selectedModifierInstanceId!, BuildOptionalCost()));
                        _selectedModifierInstanceId = null;
                        break;
                    case CraftingMode.Ascend:
                        await _messageBus.SendRequest<AscendEquipItemRequest, AscensionResult>(new(_item!.InstanceId));
                        break;
                }

                RefreshDetails();
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Crafting action failed", exception, this);
                GD.Print($"Crafting action failed: {exception.Message}");
            }
        }

        // ---------------------------------------------------------------- picker

        /// <summary>The candidate list opens as an Overlay popup at the cursor — the bench layout
        /// never moves. RefreshDetails (and a dying window) closes whatever picker is up.</summary>
        private void OpenPicker(string title, List<string> ids, Action<string> onPicked)
        {
            var entries = ids
                .Select(id => new ResourcePickerPopup.PickerEntry(
                    id,
                    $"{Localization.Localize(id)}   ({_inventory?.GetTotalItemAmount(id) ?? 0})",
                    _dataProvider?.GetItemIcon(id)))
                .ToList();

            _pickerPopup = _uiElements?.ShowPopup(typeof(ResourcePickerPopup)) as ResourcePickerPopup;
            _pickerPopup?.Present(title, entries, onPicked);
        }

        private void ClosePicker()
        {
            if (_pickerPopup != null && IsInstanceValid(_pickerPopup)) _pickerPopup.Close();
            _pickerPopup = null;
        }

        // ---------------------------------------------------------------- misc

        private void ResetChoices()
        {
            _categoryChoices.Clear();
            Array.Clear(_additiveChoices);
        }

        private void OnItemAmountChanged(string itemId, int amount)
        {
            BuildTree();
            RefreshDetails();
        }
    }
}
