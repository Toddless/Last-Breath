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
    using Core.Modifiers;
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
        private static readonly Vector2 s_cardIconSize = new(28, 28);
        private static readonly Vector2 s_cardMinSize = new(0, 44);
        private const int ForecastValueWidth = 130;
        private const int GrantDescriptionMinWidth = 320;

        [Export] private Tree? _tree;
        [Export] private LineEdit? _search;
        [Export] private Button? _close, _actionButton;
        [Export] private Button? _modeCreate, _modeUpgrade, _modeRecraft, _modeAscend;
        [Export] private TextureRect? _itemIcon;
        [Export] private Label? _title, _listTitle, _itemName, _itemSubtitle, _previewTag, _requirementsHeader, _additivesHeader, _ascendWarning;
        [Export] private VBoxContainer? _mods;
        [Export] private VBoxContainer? _requirements, _additives;
        [Export] private Label? _masteryLevel, _masteryXpLabel, _masteryTitle, _forecastHeader, _forecastHint;
        [Export] private ProgressBar? _masteryXpBar;
        [Export] private GridContainer? _masteryChips;
        [Export] private Control? _forecastBox;
        [Export] private VBoxContainer? _forecastRows;
        [Export] private Control? _itemHeader, _poolBox;
        [Export] private Label? _poolHeader, _poolCounter;
        [Export] private VBoxContainer? _poolList;

        private readonly Dictionary<string, string> _categoryChoices = [];
        private readonly string?[] _additiveChoices = new string?[AdditiveSlots];
        private ResourcePickerPopup? _pickerPopup;

        private IItemDataProvider? _dataProvider;
        private ModifierFormatter? _modifierFormatter;
        private IInventory? _inventory;
        private ICraftingMastery? _mastery;
        private IRecipeKnowledge? _knowledge;
        private IItemUpgrader? _upgrader;
        private IItemAscender? _ascender;
        private ICraftingAdditiveProvider? _additiveProvider;
        private IGameMessageBus? _messageBus;
        private IUiElementsManager? _uiElements;

        private CraftingMode _mode = CraftingMode.Create;
        private string? _recipeId;
        private IEquipItem? _item;
        private string? _selectedModifierInstanceId;


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
            _itemHeader?.GuiInput += OnItemHeaderInput;

            LocalizeStaticLabels();
        }

        public override void _ExitTree()
        {
            // The picker lives in the Overlay layer — a window closed by hotkey must not orphan it.
            ClosePicker();
            if (_inventory != null) _inventory.ItemAmountChanges -= OnItemAmountChanged;
            if (_knowledge != null) _knowledge.RecipeLearned -= OnRecipeLearned;
            if (_mastery != null)
            {
                _mastery.CurrentLevelChange -= OnMasteryLevelChanged;
                _mastery.BonusLevelChange -= OnMasteryLevelChanged;
            }
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _dataProvider = provider.GetService<IItemDataProvider>();
            _modifierFormatter = provider.GetService<ModifierFormatter>();
            _inventory = provider.GetService<IInventory>();
            _mastery = provider.GetService<ICraftingMastery>();
            _upgrader = provider.GetService<IItemUpgrader>();
            _ascender = provider.GetService<IItemAscender>();
            _additiveProvider = provider.GetService<ICraftingAdditiveProvider>();
            _messageBus = provider.GetService<IGameMessageBus>();
            _uiElements = provider.GetService<IUiElementsManager>();
            _knowledge = provider.GetService<IRecipeKnowledge>();

            _inventory.ItemAmountChanges += OnItemAmountChanged;
            // Knowledge is live: scroll learns and mastery levels both unlock tree entries in place.
            if (_knowledge != null) _knowledge.RecipeLearned += OnRecipeLearned;
            if (_mastery != null)
            {
                _mastery.CurrentLevelChange += OnMasteryLevelChanged;
                _mastery.BonusLevelChange += OnMasteryLevelChanged;
            }

            BuildTree();
            RefreshDetails();
        }

        private void OnRecipeLearned(string recipeId) => BuildTree();

        private void OnMasteryLevelChanged(int level) => BuildTree();

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

        /// <summary>Create always leads back to recipe browsing; an item mode entered with an empty
        /// bench opens the equipment picker instead of refusing — the mode flips once a piece is picked.</summary>
        private void SwitchMode(CraftingMode mode)
        {
            if (_mode == mode) { RefreshModeTabs(); return; }
            if (mode != CraftingMode.Create && _item == null)
            {
                RefreshModeTabs();
                OpenEquipPicker(mode);
                return;
            }

            _mode = mode;
            if (mode == CraftingMode.Create) _item = null;
            _selectedModifierInstanceId = null;
            ResetChoices();
            RefreshDetails();
        }

        private void RefreshModeTabs()
        {
            SyncTab(_modeCreate, CraftingMode.Create);
            SyncTab(_modeUpgrade, CraftingMode.Upgrade);
            SyncTab(_modeRecraft, CraftingMode.Recraft);
            SyncTab(_modeAscend, CraftingMode.Ascend);
        }

        /// <summary>The bench item header doubles as a "change item" button in the item modes.</summary>
        private void OnItemHeaderInput(InputEvent @event)
        {
            if (_mode == CraftingMode.Create) return;
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            OpenEquipPicker(_mode);
        }

        /// <summary>Equipment picker for an item mode: bag pieces the mode can actually work on.
        /// The tooltip previews the piece's full line list, the label carries its rarity colour.</summary>
        private void OpenEquipPicker(CraftingMode mode)
        {
            var entries = (_inventory?.GetContents() ?? [])
                .Select(entry => entry.Item)
                .OfType<IEquipItem>()
                .Where(item => ModeAccepts(item, mode))
                .Select(item => new PickerEntry(
                    item.InstanceId,
                    item.UpdateLevel > 0 ? $"{item.DisplayName} +{item.UpdateLevel}" : item.DisplayName,
                    item.Icon,
                    EquipPickerTooltip(item),
                    Color.FromHtml(TextPalette.RarityColor(item.Rarity))))
                .ToList();

            _pickerPopup = _uiElements?.ShowPopup(typeof(IPickerPopup)) as ResourcePickerPopup;
            _pickerPopup?.Present(Localization.Localize("UI_Craft_PickItem"), entries, instanceId =>
            {
                if (_inventory?.GetItem<IEquipItem>(instanceId) is { } picked) SetItem(picked, mode);
            });
        }

        /// <summary>Which bag pieces an item mode offers: sealed items are untouchable everywhere,
        /// upgrade wants headroom, ascension wants Legendary (the deep gate stays with the handler).</summary>
        private static bool ModeAccepts(IEquipItem item, CraftingMode mode) => mode switch
        {
            CraftingMode.Upgrade => !item.IsSealed && item.UpdateLevel < item.MaxUpdateLevel,
            CraftingMode.Recraft => !item.IsSealed,
            CraftingMode.Ascend => !item.IsSealed && item.Rarity == Rarity.Legendary,
            _ => false,
        };

        private string EquipPickerTooltip(IEquipItem item)
        {
            var lines = new List<string> { $"{Localization.Localize(item.Rarity.ToString())} · {Localization.Localize(item.EquipmentPiece.ToString())}" };
            foreach (var parameter in item.BaseStats.Keys)
            {
                (float baseValue, float localBonus) = item.GetBaseStatBreakdown(parameter);
                string value = _modifierFormatter?.FormatValue(ModifierValueType.Flat, parameter, baseValue + localBonus)
                    ?? (baseValue + localBonus).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                lines.Add($"{Localization.Localize(parameter.ToString())}: {value}");
            }

            lines.AddRange(EquipItemLines.ComposeImplicits(item).Select(line => line.Text));
            lines.AddRange(EquipItemLines.ComposeRolled(item).Select(line => line.Text));
            lines.AddRange(item.Grants.Select(grant => Localization.Localize(grant.Id)));
            return string.Join("\n", lines);
        }

        /// <summary>Shared BBCode label defaults; callers add size flags/minimums on top.</summary>
        private static RichTextLabel RichText(string text, TextServer.AutowrapMode autowrap = TextServer.AutowrapMode.WordSmart) =>
            new() { Text = text, BbcodeEnabled = true, FitContent = true, AutowrapMode = autowrap };

        private void SyncTab(Button? tab, CraftingMode mode) => tab?.SetPressedNoSignal(_mode == mode);

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

                    // Unknown recipes stay visible but locked — the hint says how to get them.
                    bool known = _knowledge?.IsKnown(recipe.Id) ?? true;
                    entry.SetSelectable(0, known);
                    if (known) continue;
                    entry.SetCustomColor(0, Color.FromHtml(TextPalette.System));
                    entry.SetTooltipText(0, recipe.UnlockAtMastery is { } gate
                        ? Localization.Render("UI_Recipe_Locked_Mastery", new Dictionary<string, object?> { ["Level"] = gate })
                        : Localization.Localize("UI_Recipe_Locked_Scroll"));
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
            RenderPool();
            RenderForecast();
            RenderAction();
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

            _masteryChips.QueueFreeChildren();

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

        /// <summary>The "possible modifiers" column (Umbral mockup 2026-07-25): sections per slot
        /// family with entry counts. Create rows split into "kind + parameter" left and the roll
        /// interval right; the ascension gift pool keeps FULL sentence rows (owner: the split view
        /// broke the mythic entries) with the Mythic tint. Reroll shows the item's LIVE pool with
        /// additives folded in; entries the dedup/slot rules would refuse right now render muted.
        /// Upgrade keeps the column hidden.</summary>
        private void RenderPool()
        {
            if (_poolBox == null || _poolList == null) return;
            _poolList.QueueFreeChildren();

            int entryCount = 0;
            var rows = _mode switch
            {
                CraftingMode.Create => DescriptorSections(CreationPoolDescriptors(), tint: null, splitRows: true, out entryCount),
                CraftingMode.Recraft => DescriptorSections(RecraftPoolDescriptors(), tint: null, splitRows: true, out entryCount, RecraftIneligible()),
                CraftingMode.Ascend => DescriptorSections(AscensionPoolDescriptors(),
                    Color.FromHtml(TextPalette.RarityColor(Rarity.Mythic)), splitRows: false, out entryCount),
                _ => [],
            };

            _poolBox.Visible = rows.Count > 0;
            if (rows.Count == 0) return;

            _poolHeader?.Text = Localization.Localize("UI_Craft_Pool").ToUpper();
            _poolCounter?.Text = Localization.Render("UI_Pool_Counter", new Dictionary<string, object?> { ["Count"] = entryCount });
            foreach (var row in rows)
                _poolList.AddChild(row);
        }

        private IEnumerable<Core.Modifiers.IModifierDescriptor> CreationPoolDescriptors()
        {
            if (_dataProvider == null || _mastery == null || _recipeId == null || SelectedBlueprint() is not { } blueprint)
                return [];

            string resultId = _dataProvider.GetRecipeResultItemId(_recipeId);
            return _dataProvider.GetGenerationPool(resultId)
                .Concat(BuildCost().Keys
                    .SelectMany(_dataProvider.GetResourceDescriptors)
                    .ForCategory(blueprint.Piece.ConvertEquipmentPartToCategory()))
                .Where(descriptor => descriptor.Affix != AffixKind.Mythic)
                .Select(descriptor => Core.Modifiers.DescriptorOperations.Scale(descriptor, _mastery.GetCurrentValueMultiplier()));
        }

        private IEnumerable<Core.Modifiers.IModifierDescriptor> AscensionPoolDescriptors() =>
            _item is { Rarity: Rarity.Legendary } item && _ascender != null
                ? _ascender.GetGiftPool(item.EquipmentPiece.ConvertEquipmentPartToCategory())
                : [];

        /// <summary>The item's live reroll pool with the chosen additives folded in — straight from
        /// the upgrader, so the preview and the actual roll share one composition.</summary>
        private IEnumerable<Core.Modifiers.IModifierDescriptor> RecraftPoolDescriptors() =>
            _item is { IsSealed: false } item && _upgrader != null
                ? _upgrader.GetRerollPreviewPool(item, ChosenAdditiveIds())
                : [];

        private List<string> ChosenAdditiveIds() => _additiveChoices.OfType<string>().ToList();

        /// <summary>Mirror of TryRecraftModifier's refusal rules for the muted rendering: an entry with no
        /// line identity (grant/operation, a composite hiding one), an identity already occupying a line —
        /// atoms by key, composites by their whole part set (the picked line's own identity stays legal) —
        /// or, once a line is picked, the other slot family.</summary>
        private Func<Core.Modifiers.IModifierDescriptor, bool>? RecraftIneligible()
        {
            if (_item == null) return null;

            var occupied = _item.OccupiedLineIdentities(_selectedModifierInstanceId == null ? [] : [_selectedModifierInstanceId]);
            var targetAffix = SelectedLineAffix();
            return descriptor =>
            {
                if (targetAffix is { } affix && affix != AffixKind.None && descriptor.Affix != affix) return true;
                return !Core.Modifiers.LineIdentity.TryFrom(descriptor, out var identity) || occupied.Contains(identity);
            };
        }

        private AffixKind? SelectedLineAffix() =>
            _item == null || _selectedModifierInstanceId == null
                ? null
                : EquipItemLines.ComposeRolled(_item)
                    .FirstOrDefault(line => line.InstanceId == _selectedModifierInstanceId)?.Affix;

        /// <summary>Pool rows grouped by slot family (prefixes / suffixes / tail / mythic), each block
        /// opened by the shared affix caption with its entry count. A parameter descriptor splits into
        /// "kind + name" and the gold roll interval; sentence-shaped entries (context, composites) and
        /// grants keep their single-line form. Duplicates (same visible row) collapse.</summary>
        private List<Control> DescriptorSections(IEnumerable<Core.Modifiers.IModifierDescriptor> descriptors, Color? tint, bool splitRows, out int entryCount,
            Func<Core.Modifiers.IModifierDescriptor, bool>? ineligible = null)
        {
            var rows = new List<Control>();
            entryCount = 0;
            foreach (var family in descriptors
                         .GroupBy(descriptor => descriptor.Affix)
                         .OrderBy(group => FamilyRank(group.Key)))
            {
                var familyRows = new List<Control>();
                var seen = new HashSet<string>();
                foreach (var descriptor in OrderForDisplay(family))
                {
                    var row = DescriptorRow(descriptor, tint, splitRows, seen, ineligible?.Invoke(descriptor) == true);
                    if (row != null) familyRows.Add(row);
                }

                if (familyRows.Count == 0) continue;
                entryCount += familyRows.Count;
                var header = ItemLineRows.AffixHeader(family.Key, familyRows.Count);
                if (header != null) rows.Add(header);
                rows.AddRange(familyRows);
            }

            return rows;
        }

        /// <summary>Same block order the item line lists use: prefixes, suffixes, the tail, mythic last.</summary>
        private static int FamilyRank(AffixKind affix) => affix switch
        {
            AffixKind.Prefix => 0,
            AffixKind.Suffix => 1,
            AffixKind.Mythic => 3,
            _ => 2,
        };

        /// <summary>Inside a family block the rollable lines group by their stat (shown name), flats
        /// before increases before multipliers, each run high to low — "+ PhysicalDamage" entries sit
        /// together from the biggest roll down. Sentence-shaped entries follow in authored order.</summary>
        private static IEnumerable<Core.Modifiers.IModifierDescriptor> OrderForDisplay(IEnumerable<Core.Modifiers.IModifierDescriptor> family)
        {
            var parameters = new List<Core.Modifiers.ParameterDescriptor>();
            var sentences = new List<Core.Modifiers.IModifierDescriptor>();
            foreach (var descriptor in family)
            {
                if (descriptor is Core.Modifiers.ParameterDescriptor parameter) parameters.Add(parameter);
                else sentences.Add(descriptor);
            }

            return parameters
                .OrderBy(parameter => Localization.Localize(parameter.Parameter.ToString()))
                .ThenBy(parameter => KindRank(parameter.ValueType))
                .ThenByDescending(parameter => parameter.Value.Max)
                .ThenByDescending(parameter => parameter.Value.Min)
                .Cast<Core.Modifiers.IModifierDescriptor>()
                .Concat(sentences);
        }

        private static int KindRank(Core.Enums.ModifierValueType valueType) => valueType switch
        {
            Core.Enums.ModifierValueType.Increase => 1,
            Core.Enums.ModifierValueType.Multiplicative => 2,
            _ => 0,
        };

        private Control? DescriptorRow(Core.Modifiers.IModifierDescriptor descriptor, Color? tint, bool splitRows, HashSet<string> seen, bool muted = false)
        {
            var mutedColor = Color.FromHtml(TextPalette.System);
            if (splitRows && descriptor is Core.Modifiers.ParameterDescriptor parameter && _modifierFormatter != null)
            {
                string name = $"{Localization.Localize(KindKey(parameter.ValueType))} {Localization.Localize(parameter.Parameter.ToString())}";
                string range = _modifierFormatter.FormatDescriptorRange(parameter);
                return seen.Add($"{name}|{range}") ? SplitRow(name, range, muted ? mutedColor : tint, muted) : null;
            }

            string text = descriptor switch
            {
                Core.Modifiers.GrantDescriptor grant => Localization.Localize(grant.GrantId),
                Core.Modifiers.UpgradeLevelsDescriptor levels => Localization.Render("UI_Pool_Extra_Levels",
                    new Dictionary<string, object?> { ["Min"] = levels.Min, ["Max"] = levels.Max }),
                _ => Localization.Format(descriptor),
            };
            if (string.IsNullOrEmpty(text) || !seen.Add(text)) return null;

            var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            if (muted) label.AddThemeColorOverride("font_color", mutedColor);
            else if (tint is { } color) label.AddThemeColorOverride("font_color", color);
            return label;
        }

        /// <summary>How a rollable line announces its shape: "+ Health", "+% incr. Evade", "+% more Armor".</summary>
        private static string KindKey(Core.Enums.ModifierValueType valueType) => valueType switch
        {
            Core.Enums.ModifierValueType.Increase => "UI_Mod_Kind_Increase",
            Core.Enums.ModifierValueType.Multiplicative => "UI_Mod_Kind_Multiplicative",
            _ => "UI_Mod_Kind_Flat",
        };

        private static Control SplitRow(string name, string value, Color? tint, bool muted = false)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            var nameLabel = new Label
            {
                Text = name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            if (tint is { } color) nameLabel.AddThemeColorOverride("font_color", color);
            row.AddChild(nameLabel);

            var valueLabel = new Label { Text = value, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            valueLabel.AddThemeColorOverride("font_color", muted ? Color.FromHtml(TextPalette.System) : Color.FromHtml(TextPalette.Number));
            row.AddChild(valueLabel);
            return row;
        }

        /// <summary>The kit's craft forecast under the resource slots: for the current operation the
        /// abstract mastery channels turn into concrete odds — data base struck through, the real
        /// chance (mastery + chosen additives, the exact numbers the handlers roll) next to it.
        /// Only lines the mode actually rolls are shown; a reroll is deterministic — no box.</summary>
        private void RenderForecast()
        {
            if (_forecastBox == null || _forecastRows == null) return;
            _forecastRows.QueueFreeChildren();

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
            var value = RichText(valueBbcode, TextServer.AutowrapMode.Off);
            value.CustomMinimumSize = new Vector2(ForecastValueWidth, 0);
            value.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(value);
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
            _itemSubtitle?.Text = blueprint == null ? string.Empty : BlueprintSubtitle(blueprint);
        }

        /// <summary>A weapon recipe names the actual weapon (type + grip) next to the rarity,
        /// mirroring the item tooltip's subtitle; everything else shows the bare rarity.</summary>
        private static string BlueprintSubtitle(EquipItemBlueprint blueprint) =>
            blueprint.Weapon is { } weapon
                ? $"{Localization.Localize(blueprint.Rarity.ToString())} · {Localization.Localize($"WeaponType_{weapon.WeaponType}")} · {Localization.Localize($"Handedness_{weapon.Handedness}")}"
                : Localization.Localize(blueprint.Rarity.ToString());

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
            _mods.QueueFreeChildren();

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
        /// shows the same blocks as the tooltip, just without its framing. Grants show their full
        /// description below the name. Upgrade mode appends the sharpening preview to every scaling
        /// part — "+193.5 Evade → 203.2 (+9.7)", projected value gold, gain green.</summary>
        private void RenderItemModifiers(IEquipItem item)
        {
            if (item is IWeaponItem weapon)
            {
                AddWeaponStatRow(EntityParameter.PhysicalDamage, weapon);
                AddWeaponStatRow(EntityParameter.CriticalChance, weapon);
                AddWeaponStatRow(EntityParameter.CriticalDamage, weapon);
            }

            // The typed base channel, folded with its locals — same convention as the item tooltip.
            foreach (var parameter in item.BaseStats.Keys)
            {
                (float baseValue, float localBonus) = item.GetBaseStatBreakdown(parameter);
                AddBaseStatRow(parameter, baseValue + localBonus, highlighted: Mathf.Abs(localBonus) > 0.0001f);
            }


            float? previewScale = _mode == CraftingMode.Upgrade && !item.IsSealed && item.UpdateLevel < item.MaxUpdateLevel
                ? item.NextUpgradeValueScale
                : null;
            var format = previewScale == null ? TextFormat.Plain : TextFormat.Rich;

            foreach (var line in EquipItemLines.ComposeImplicits(item, format, previewScale))
                _mods?.AddChild(LineRow(line.Text, rich: previewScale != null, dim: true));

            AffixKind? block = null;
            foreach (var line in EquipItemLines.ComposeRolled(item, format, previewScale))
            {
                if (line.Affix != block)
                {
                    block = line.Affix;
                    var header = ItemLineRows.AffixHeader(line.Affix);
                    if (header != null) _mods?.AddChild(header);
                }

                _mods?.AddChild(previewScale != null
                    ? LineRow(line.Text, rich: true, dim: false)
                    : RerollableOrLabel(line.InstanceId, line.Text));
            }

            foreach (var grant in item.Grants)
                AddGrantBlock(grant);
        }

        /// <summary>An item line row: a plain Label normally, a RichTextLabel when the text carries
        /// the colored upgrade preview (BBCode). Dim rows keep their tone via a default_color override —
        /// theme variations only target Label.</summary>
        private static Control LineRow(string text, bool rich, bool dim)
        {
            if (!rich)
                return new Label { Text = text, ThemeTypeVariation = dim ? "DimLabel" : null, AutowrapMode = TextServer.AutowrapMode.WordSmart };

            var row = RichText(text);
            row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            if (dim) row.AddThemeColorOverride("default_color", Color.FromHtml(TextPalette.Muted));
            return row;
        }

        /// <summary>A grant on the bench mirrors the tooltip's Effect block: the name, then the rendered
        /// rich description below (a plain Label would print the raw BBCode tags).</summary>
        private void AddGrantBlock(IItemGrant grant)
        {
            _mods?.AddChild(new Label { Text = Localization.Localize(grant.Id), AutowrapMode = TextServer.AutowrapMode.WordSmart });
            if (string.IsNullOrEmpty(grant.Description)) return;
            var description = RichText(grant.Description);
            description.CustomMinimumSize = new Vector2(GrantDescriptionMinWidth, 0);
            description.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _mods?.AddChild(description);
        }

        /// <summary>Recipe result preview straight from the blueprint: a weapon opens with its base
        /// combat stats (the numbers the minted item is born with), then descriptors with value
        /// spreads render through the range templates ("+40–60 Strength"), fixed lines as usual.</summary>
        private void RenderBlueprintModifiers(EquipItemBlueprint blueprint)
        {
            if (blueprint.Weapon is { } weapon)
            {
                AddBaseStatRow(EntityParameter.PhysicalDamage, weapon.Damage);
                AddBaseStatRow(EntityParameter.CriticalChance, weapon.CriticalChance);
                AddBaseStatRow(EntityParameter.CriticalDamage, weapon.CriticalDamage);
            }

            // Unrolled base stats show their roll spread; a fixed base shows the single value.
            foreach (var stat in blueprint.BaseStats)
                AddBaseStatSpreadRow(stat);

            foreach (var descriptor in blueprint.Implicits)
                _mods?.AddChild(new Label { Text = Localization.Format(descriptor), ThemeTypeVariation = "DimLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart });

            foreach (var descriptor in blueprint.Modifiers)
                _mods?.AddChild(new Label { Text = Localization.Format(descriptor), AutowrapMode = TextServer.AutowrapMode.WordSmart });

            foreach (var grant in blueprint.Grants)
                _mods?.AddChild(new Label { Text = Localization.Localize(grant.Id), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }

        /// <summary>A bench-item weapon stat: the effective value with local lines folded in; a
        /// locally modified stat glows brighter — same convention as the item tooltip.</summary>
        private void AddWeaponStatRow(EntityParameter parameter, IWeaponItem weapon)
        {
            (float baseValue, float localBonus) = weapon.GetStatBreakdown(parameter);
            AddBaseStatRow(parameter, baseValue + localBonus, highlighted: Mathf.Abs(localBonus) > 0.0001f);
        }

        /// <summary>Blueprint base-stat row: the roll spread ("500–900") instead of a value — the
        /// bench previews what the mint can produce, not a concrete roll.</summary>
        private void AddBaseStatSpreadRow(BaseStatBlueprint stat)
        {
            string Bound(float value) =>
                _modifierFormatter?.FormatValue(ModifierValueType.Flat, stat.Parameter, value)
                ?? value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = Localization.Localize(stat.Parameter.ToString()),
                ThemeTypeVariation = "DimLabel",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            var valueLabel = new Label
            {
                Text = stat.Value.IsFixed ? Bound(stat.Value.Min) : $"{Bound(stat.Value.Min)}–{Bound(stat.Value.Max)}",
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            valueLabel.AddThemeColorOverride("font_color", Color.FromHtml(TextPalette.BaseStat));
            row.AddChild(valueLabel);
            _mods?.AddChild(row);
        }

        /// <summary>One base-stat row: dim parameter name, unit-aware value on the right.</summary>
        private void AddBaseStatRow(EntityParameter parameter, float value, bool highlighted = false)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = Localization.Localize(parameter.ToString()),
                ThemeTypeVariation = "DimLabel",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            var valueLabel = new Label
            {
                Text = _modifierFormatter?.FormatValue(ModifierValueType.Flat, parameter, value) ?? value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            valueLabel.AddThemeColorOverride("font_color", Color.FromHtml(highlighted ? TextPalette.Number : TextPalette.BaseStat));
            row.AddChild(valueLabel);
            _mods?.AddChild(row);
        }

        /// <summary>In Recraft every additional row — entity, context or grouped — is pickable; the chosen
        /// one gets rerolled. A sealed item (unique/mythic) offers no rows at all — nothing on it rerolls.</summary>
        private Control RerollableOrLabel(string instanceId, string text) =>
            _mode == CraftingMode.Recraft && _item is { IsSealed: false }
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
            _requirements.QueueFreeChildren();

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
            _additives.QueueFreeChildren();

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

        /// <summary>Compact horizontal slot-row content (owner request 2026-07-24 — the tall square
        /// cards ate the bench): icon on the left, name, have/need on the right.</summary>
        private static Control CardContent(Texture2D? icon, string name, string? count, bool countMet)
        {
            var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            margin.SetAnchorsPreset(LayoutPreset.FullRect);
            margin.AddThemeConstantOverride("margin_left", 8);
            margin.AddThemeConstantOverride("margin_right", 8);
            margin.AddThemeConstantOverride("margin_top", 4);
            margin.AddThemeConstantOverride("margin_bottom", 4);

            var content = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                // The empty additive slot is a lone "+" — center it; real rows read left to right.
                Alignment = icon == null && count == null ? BoxContainer.AlignmentMode.Center : BoxContainer.AlignmentMode.Begin,
            };
            content.AddThemeConstantOverride("separation", 8);
            margin.AddChild(content);

            if (icon != null)
            {
                content.AddChild(new TextureRect
                {
                    Texture = icon,
                    CustomMinimumSize = s_cardIconSize,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    MouseFilter = MouseFilterEnum.Ignore,
                });
            }

            content.AddChild(new Label
            {
                Text = name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            });

            if (count != null)
            {
                content.AddChild(new Label
                {
                    Text = count,
                    VerticalAlignment = VerticalAlignment.Center,
                    ThemeTypeVariation = countMet ? null : "DimLabel",
                    MouseFilter = MouseFilterEnum.Ignore,
                });
            }

            return margin;
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
                CraftingMode.Upgrade => _item is { IsSealed: false } && _item.UpdateLevel < _item.MaxUpdateLevel && costCovered,
                CraftingMode.Recraft => _item is { IsSealed: false } && _selectedModifierInstanceId != null && costCovered,
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
                        // The selection FOLLOWS the reroll (the fresh line keeps the slot): the button
                        // works as a toggle — pick once, reroll repeatedly. A refusal keeps the old pick.
                        var recraft = await _messageBus.SendRequest<RecraftEquipItemModifierRequest, RequestResult<string>>(
                            new(_item!.InstanceId, _selectedModifierInstanceId!, BuildOptionalCost()));
                        if (recraft is { IsSuccess: true, Param: { } freshLineId }) _selectedModifierInstanceId = freshLineId;
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
        /// never moves. RefreshDetails (and a dying window) closes whatever picker is up.
        /// Hovering a row shows the resource's description and what it feeds into the pool.</summary>
        private void OpenPicker(string title, List<string> ids, Action<string> onPicked)
        {
            var entries = ids
                .Select(id => new PickerEntry(
                    id,
                    $"{Localization.Localize(id)}   ({_inventory?.GetTotalItemAmount(id) ?? 0})",
                    _dataProvider?.GetItemIcon(id),
                    ResourceTooltip(id)))
                .ToList();

            _pickerPopup = _uiElements?.ShowPopup(typeof(IPickerPopup)) as ResourcePickerPopup;
            _pickerPopup?.Present(title, entries, onPicked);
        }

        /// <summary>Hover text of a picker row: the resource's own description plus the pool lines its
        /// descriptors contribute. A missing description key renders as the key — filtered out.</summary>
        private string ResourceTooltip(string id)
        {
            var lines = new List<string>();
            if (Localization.TryLocalizeDescription(id, out string description)) lines.Add(description);

            foreach (var descriptor in _dataProvider?.GetResourceDescriptors(id) ?? [])
            {
                // The tooltip describes the resource in general, across every byCategory section —
                // an essence authoring identical lines per category must not repeat them here.
                string text = Localization.Format(descriptor);
                if (!string.IsNullOrEmpty(text) && !lines.Contains(text)) lines.Add(text);
            }

            return string.Join("\n", lines);
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
