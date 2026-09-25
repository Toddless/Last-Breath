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
    using Core.Views;
    using Core.Views.UI;
    using Modules;
    using Godot;
    using SharedUi;

    /// <summary>
    /// The one crafting screen, a thin composer over module scenes: recipe tree on the left; mastery
    /// rail, mode tabs, the bench (preview / requirement slots / pool column) and the action bar on
    /// the right. The window owns the mode, the picked recipe/item and the slot choices, turns child
    /// events into pickers and bus commands, and pushes fresh view data back down. Reads go straight
    /// to the services; only the crafting COMMANDS travel the bus.
    /// </summary>
    public partial class CraftingWindow : Control, IWindow
    {
        private const string UID = "uid://betq124kfglyy";
        private const int AdditiveSlots = 3;
        private const string EssenceCategoryId = "Category_Essence";

        private const string TitleKey = "UI_Crafting";
        private const string PickItemKey = "UI_Craft_PickItem";
        private const string ChooseCategoryKey = "UI_Craft_Choose";
        private const string ExtraLevelForecastKey = "UI_Forecast_Extra_Level";
        private const string MinRarityForecastKey = "UI_Forecast_Min_Rarity";
        private const string UpgradeActionKey = "UI_Craft_Upgrade";
        private const string RerollActionKey = "UI_Craft_Reroll";
        private const string AscendActionKey = "UI_Craft_Ascend";
        private const string CreateActionKey = "UI_Craft_Create";

        [Export] private WindowHeader? _header;
        [Export] private RecipeTreePanel? _recipeTree;
        [Export] private MasteryRail? _masteryRail;
        [Export] private ModeTabs? _modeTabs;
        [Export] private ItemPreviewPane? _preview;
        [Export] private RequirementsPanel? _requirements;
        [Export] private PoolColumn? _pool;
        [Export] private ActionBar? _actionBar;

        private readonly Dictionary<string, string> _categoryChoices = [];
        private readonly string?[] _additiveChoices = new string?[AdditiveSlots];
        private IPickerPopup? _pickerPopup;

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
            _header?.Closed += Close;
            _header?.SetTitle(Localization.Localize(TitleKey));

            _recipeTree?.RecipeSelected += OnRecipeSelected;
            _modeTabs?.ModeChanged += SwitchMode;
            _preview?.ChangeItemRequested += () => OpenEquipPicker(_mode);
            _preview?.ModifierPicked += OnModifierPicked;
            _requirements?.CategorySlotClicked += OnCategorySlotClicked;
            _requirements?.AdditiveSlotClicked += OnAdditiveSlotClicked;
            _actionBar?.ActionPressed += OnActionPressed;
        }

        public override void _ExitTree()
        {
            // The picker lives in the Overlay layer — a window closed by hotkey must not orphan it.
            ClosePicker();
            _inventory?.ItemAmountChanges -= OnItemAmountChanged;
            _knowledge?.RecipeLearned -= OnRecipeLearned;
            _mastery?.CurrentLevelChange -= OnMasteryLevelChanged;
            _mastery?.BonusLevelChange -= OnMasteryLevelChanged;
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
            _knowledge.RecipeLearned += OnRecipeLearned;
            _mastery.CurrentLevelChange += OnMasteryLevelChanged;
            _mastery.BonusLevelChange += OnMasteryLevelChanged;

            _preview?.SetFormatter(_modifierFormatter);
            _pool?.SetFormatter(_modifierFormatter);
            _recipeTree?.SetCatalog(_dataProvider, _inventory, _knowledge);
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
            _recipeTree?.Deselect();
            RefreshDetails();
        }

        private void OnRecipeLearned(string recipeId) => _recipeTree?.Rebuild();

        private void OnMasteryLevelChanged(int level) => _recipeTree?.Rebuild();

        // ---------------------------------------------------------------- child events

        /// <summary>Create always leads back to recipe browsing; an item mode entered with an empty
        /// bench opens the equipment picker instead of refusing — the mode flips once a piece is picked.</summary>
        private void SwitchMode(CraftingMode mode)
        {
            if (_mode == mode)
            {
                _modeTabs?.SetMode(_mode);
                return;
            }

            if (mode != CraftingMode.Create && _item == null)
            {
                _modeTabs?.SetMode(_mode);
                OpenEquipPicker(mode);
                return;
            }

            _mode = mode;
            if (mode == CraftingMode.Create) _item = null;
            _selectedModifierInstanceId = null;
            ResetChoices();
            RefreshDetails();
        }

        private void OnRecipeSelected(string recipeId)
        {
            if (recipeId == _recipeId) return;

            _recipeId = recipeId;
            _item = null;
            _mode = CraftingMode.Create;
            _selectedModifierInstanceId = null;
            ResetChoices();
            RefreshDetails();
        }

        private void OnModifierPicked(string instanceId)
        {
            _selectedModifierInstanceId = instanceId;
            RefreshDetails();
        }

        private void OnCategorySlotClicked(string categoryId) => OpenPicker(
            Localization.Localize(categoryId),
            CategoryResourceIds(categoryId),
            picked =>
            {
                _categoryChoices[categoryId] = picked;
                RefreshDetails();
            });

        private void OnAdditiveSlotClicked(int slot)
        {
            if (_additiveChoices[slot] != null)
            {
                _additiveChoices[slot] = null; // click on a filled slot empties it
                RefreshDetails();
                return;
            }

            OpenPicker(Localization.Localize(RequirementsPanel.AdditivesKey), AvailableAdditiveIds(), picked =>
            {
                _additiveChoices[slot] = picked;
                RefreshDetails();
            });
        }

        // ---------------------------------------------------------------- equipment picker

        /// <summary>Equipment picker for an item mode: bag pieces the mode can actually work on.
        /// Hovering a row opens the SAME framed item card the bag slots show (through the Core
        /// contract — this module cannot name the game's popup class); a project that never
        /// registered that card (the standalone crafting sandbox) falls back to the plain text
        /// tooltip. The label carries the piece's rarity colour.</summary>
        private void OpenEquipPicker(CraftingMode mode)
        {
            bool framedCard = _uiElements?.HasPopupFactory(typeof(IItemTooltipPopup)) == true;
            var entries = (_inventory?.GetContents() ?? [])
                .Select(entry => entry.Item)
                .OfType<IEquipItem>()
                .Where(item => ModeAccepts(item, mode))
                .Select(item => new PickerEntry(
                    item.InstanceId,
                    item.UpdateLevel > 0 ? $"{item.DisplayName} +{item.UpdateLevel}" : item.DisplayName,
                    item.Icon,
                    framedCard ? null : EquipPickerTooltip(item),
                    Color.FromHtml(TextPalette.RarityColor(item.Rarity)),
                    framedCard ? () => ShowItemCard(item) : null))
                .ToList();

            _pickerPopup = _uiElements?.ShowPopup(typeof(IPickerPopup)) as IPickerPopup;
            _pickerPopup?.Present(Localization.Localize(PickItemKey), entries, instanceId =>
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

        /// <summary>The full framed item tooltip for a hovered picker row — the same card the bag
        /// slots open, resolved through the Core contract.</summary>
        private IPopup? ShowItemCard(IEquipItem item)
        {
            if (_uiElements?.ShowPopup(typeof(IItemTooltipPopup)) is not IItemTooltipPopup popup) return null;
            popup.ShowItem(item);
            return popup;
        }

        /// <summary>Plain-text fallback of the picker row hover, kept for projects without the framed
        /// item card (the standalone crafting sandbox).</summary>
        private string EquipPickerTooltip(IEquipItem item)
        {
            var lines = new List<string> { $"{Localization.Localize(item.Rarity.ToString())} · {Localization.Localize(item.EquipmentPiece.ToString())}" };
            foreach (var parameter in item.BaseStats.Keys)
            {
                (float baseValue, float localBonus) = item.GetBaseStatBreakdown(parameter);
                string value = _modifierFormatter?.FormatValue(ModifierValueType.Flat, parameter, baseValue + localBonus)
                               ?? CraftingFormat.PlainNumber(baseValue + localBonus);
                lines.Add($"{Localization.Localize(parameter.ToString())}: {value}");
            }

            lines.AddRange(EquipItemLines.ComposeImplicits(item).Select(line => line.Text));
            lines.AddRange(EquipItemLines.ComposeRolled(item).Select(line => line.Text));
            lines.AddRange(item.Grants.Select(grant => Localization.Localize(grant.Id)));
            return string.Join("\n", lines);
        }

        // ---------------------------------------------------------------- refresh

        private void RefreshDetails()
        {
            ClosePicker();
            _modeTabs?.SetMode(_mode);
            if (_mastery != null) _masteryRail?.Refresh(_mastery);
            RefreshPreview();
            RefreshRequirements();
            RefreshAdditives();
            RefreshPool();
            RefreshForecast();
            RefreshAction();
        }

        private void RefreshPreview()
        {
            if (_preview == null) return;
            if (_item != null)
            {
                _preview.ShowItem(_item, _mode, _selectedModifierInstanceId);
                return;
            }

            if (_recipeId == null)
            {
                _preview.ShowEmpty();
                return;
            }

            _preview.ShowRecipe(Localization.Localize(_recipeId), RecipeResultIcon(), SelectedBlueprint());
        }

        private Texture2D? RecipeResultIcon()
        {
            string resultId = _recipeId == null ? string.Empty : _dataProvider?.GetRecipeResultItemId(_recipeId) ?? string.Empty;
            return resultId.Length > 0 ? _dataProvider?.GetItemIcon(resultId) : null;
        }

        private EquipItemBlueprint? SelectedBlueprint()
        {
            if (_recipeId == null || _dataProvider == null) return null;
            string resultId = _dataProvider.GetRecipeResultItemId(_recipeId);
            return resultId.Length == 0 ? null : _dataProvider.GetBlueprint(resultId);
        }

        // ---------------------------------------------------------------- requirement / additive cards

        private void RefreshRequirements()
        {
            var cards = CurrentRequirements()
                .Select(requirement => requirement.Type == RequirementType.ResourceCategory
                    ? CategoryCardView(requirement)
                    : StaticCardView(requirement))
                .ToList();
            _requirements?.SetRequirements(cards, ascendWarningVisible: _mode == CraftingMode.Ascend && _item != null);
        }

        /// <summary>Fixed requirement row: mastery reads the level, resources read the bag.</summary>
        private RequirementCardView StaticCardView(Core.Interfaces.IRequirement requirement)
        {
            int have = requirement.Type == RequirementType.MasteryLevel
                ? _mastery?.CurrentLevel ?? 0
                : _inventory?.GetTotalItemAmount(requirement.Id) ?? 0;
            var icon = requirement.Type == RequirementType.Resource ? _dataProvider?.GetItemIcon(requirement.Id) : null;
            return new RequirementCardView(icon, Localization.Localize(requirement.Id), $"{have} / {requirement.Amount}", have >= requirement.Amount);
        }

        /// <summary>A category requirement is a slot: unfilled it invites a pick, filled it shows the choice.</summary>
        private RequirementCardView CategoryCardView(Core.Interfaces.IRequirement requirement)
        {
            if (!_categoryChoices.TryGetValue(requirement.Id, out string? resourceId))
                return new RequirementCardView(null, $"{Localization.Localize(ChooseCategoryKey)}: {Localization.Localize(requirement.Id)}", null, CountMet: true, requirement.Id);

            int have = _inventory?.GetTotalItemAmount(resourceId) ?? 0;
            return new RequirementCardView(_dataProvider?.GetItemIcon(resourceId), Localization.Localize(resourceId), $"{have} / {requirement.Amount}", have >= requirement.Amount,
                requirement.Id);
        }

        /// <summary>Owned candidates of one category slot, minus resources already occupying another slot.</summary>
        private List<string> CategoryResourceIds(string categoryId) =>
            CategoryResources.CandidateIds(_dataProvider, _inventory, categoryId)
                .Where(id => (_inventory?.GetTotalItemAmount(id) ?? 0) > 0)
                .Distinct()
                .Where(id => !_categoryChoices.ContainsValue(id))
                .ToList();

        private void RefreshAdditives()
        {
            // Nothing to offer while the CraftingAdditives catalog is empty — the block hides entirely.
            bool visible = (_additiveProvider?.KnownAdditiveIds.Count ?? 0) > 0 && (_item != null || _recipeId != null);
            List<AdditiveSlotView> slots = visible
                ? _additiveChoices
                    .Select(choice => choice == null
                        ? new AdditiveSlotView(null, null)
                        : new AdditiveSlotView(_dataProvider?.GetItemIcon(choice), Localization.Localize(choice)))
                    .ToList()
                : [];
            _requirements?.SetAdditives(slots);
        }

        /// <summary>Only additives that give an effect in the current mode are offered, plus plain
        /// essences (resources with own descriptors) where the operation's pool consumes them (creation and reroll).</summary>
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

        /// <summary>Essences: resources of the essence category carrying their own descriptors.
        /// Ores/hides also feed pools, but they live in the RECIPE slots — offering them here too
        /// would flood the picker without adding a choice the recipe doesn't already give.</summary>
        private IEnumerable<string> EssenceResourceIds() =>
            (_dataProvider?.GetAllResources() ?? [])
            .OfType<ICraftingResource>()
            .Where(resource => resource.Material?.MaterialCategory?.Id == EssenceCategoryId && resource.Material.Modifiers.Count > 0)
            .Select(resource => resource.Id);

        // ---------------------------------------------------------------- pool and forecast

        /// <summary>Which descriptors the pool column shows per mode: creation and reroll split into
        /// "kind + parameter" rows, the ascension gift pool keeps full sentence rows with the Mythic
        /// tint, Upgrade keeps the column hidden.</summary>
        private void RefreshPool()
        {
            switch (_mode)
            {
                case CraftingMode.Create:
                    _pool?.SetPool(CreationPoolDescriptors(), tint: null, splitRows: true, ineligible: null);
                    break;
                case CraftingMode.Recraft:
                    _pool?.SetPool(RecraftPoolDescriptors(), tint: null, splitRows: true, RecraftIneligible());
                    break;
                case CraftingMode.Ascend:
                    _pool?.SetPool(AscensionPoolDescriptors(),
                        Color.FromHtml(TextPalette.RarityColor(Rarity.Mythic)), splitRows: false, ineligible: null);
                    break;
                default:
                    _pool?.SetPool([], tint: null, splitRows: false, ineligible: null);
                    break;
            }
        }

        private IEnumerable<IModifierDescriptor> CreationPoolDescriptors()
        {
            if (_dataProvider == null || _mastery == null || _recipeId == null || SelectedBlueprint() is not { } blueprint)
                return [];

            string resultId = _dataProvider.GetRecipeResultItemId(_recipeId);
            return _dataProvider.GetGenerationPool(resultId)
                .Concat(BuildCost().Keys
                    .SelectMany(_dataProvider.GetResourceDescriptors)
                    .ForCategory(blueprint.Piece.ConvertEquipmentPartToCategory()))
                .Where(descriptor => descriptor.Affix != AffixKind.Mythic)
                .Select(descriptor => DescriptorOperations.Scale(descriptor, _mastery.GetCurrentValueMultiplier()));
        }

        private IEnumerable<IModifierDescriptor> AscensionPoolDescriptors() =>
            _item is { Rarity: Rarity.Legendary } item && _ascender != null
                ? _ascender.GetGiftPool(item.EquipmentPiece.ConvertEquipmentPartToCategory())
                : [];

        /// <summary>The item's live reroll pool with the chosen additives folded in — straight from
        /// the upgrader, so the preview and the actual roll share one composition.</summary>
        private IEnumerable<IModifierDescriptor> RecraftPoolDescriptors() =>
            _item is { IsSealed: false } item && _upgrader != null
                ? _upgrader.GetRerollPreviewPool(item, ChosenAdditiveIds())
                : [];

        private List<string> ChosenAdditiveIds() => _additiveChoices.OfType<string>().ToList();

        /// <summary>Mirror of TryRecraftModifier's refusal rules for the muted rendering: an entry with no
        /// line identity (grant/operation, a composite hiding one), an identity already occupying a line —
        /// atoms by key, composites by their whole part set (the picked line's own identity stays legal) —
        /// or, once a line is picked, the other slot family.</summary>
        private Func<IModifierDescriptor, bool>? RecraftIneligible()
        {
            if (_item == null) return null;

            var occupied = _item.OccupiedLineIdentities(_selectedModifierInstanceId == null ? [] : [_selectedModifierInstanceId]);
            var targetAffix = SelectedLineAffix();
            return descriptor =>
            {
                if (targetAffix is { } affix && affix != AffixKind.None && descriptor.Affix != affix) return true;
                return !LineIdentity.TryFrom(descriptor, out var identity) || occupied.Contains(identity);
            };
        }

        private AffixKind? SelectedLineAffix() =>
            _item == null || _selectedModifierInstanceId == null
                ? null
                : EquipItemLines.ComposeRolled(_item)
                    .FirstOrDefault(line => line.InstanceId == _selectedModifierInstanceId)?.Affix;

        private void RefreshForecast() =>
            _requirements?.Forecast?.SetForecast(ForecastLines()
                .Select(line => new ForecastLineView(Localization.Localize(line.Key), line.Value))
                .ToList());

        /// <summary>For the current operation the abstract mastery channels turn into concrete odds —
        /// data base struck through, the real chance (mastery + chosen additives, the exact numbers
        /// the handlers roll) next to it. Only lines the mode actually rolls are shown; a reroll is
        /// deterministic — no lines.</summary>
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
                (MasteryRail.UpgradeChannelKey,
                    CraftingFormat.WasNow(_upgrader!.GetBaseUpgradeChance(item), _upgrader.GetUpgradeChance(item, BuildCost().Keys))),
            };

            float extraLevel = _additiveChoices.Where(id => id != null)
                .Sum(id => _additiveProvider?.GetEffects(id!)?.ExtraUpgradeLevelChance ?? 0f);
            if (extraLevel > 0f)
                lines.Add((ExtraLevelForecastKey, CraftingFormat.Highlight(CraftingFormat.PercentText(extraLevel))));
            return lines;
        }

        private List<(string, string)> CreateForecast()
        {
            float effectNow = _mastery!.GetExtraEffectChance();
            float effectBase = effectNow / (1f + _mastery.GetExtraEffectChanceBonus());
            var lines = new List<(string, string)>
            {
                (MasteryRail.ValuesChannelKey, CraftingFormat.Highlight($"×{_mastery.GetCurrentValueMultiplier():0.00}")),
                (MasteryRail.EffectChannelKey, CraftingFormat.WasNow(effectBase, effectNow)),
            };

            var floorRarity = _additiveChoices.Where(id => id != null)
                .Select(id => _additiveProvider?.GetEffects(id!)?.MinRarity)
                .Where(rarity => rarity != null)
                .OrderBy(rarity => rarity!.Value) // lower enum value = rarer; the best floor wins
                .FirstOrDefault();
            if (floorRarity != null)
                lines.Add((MinRarityForecastKey,
                    $"[color={TextPalette.RarityColor(floorRarity.Value)}]{Localization.Localize(floorRarity.Value.ToString())}[/color]"));
            return lines;
        }

        private List<(string, string)> AscendForecast() =>
        [
            (MasteryRail.MythicChannelKey,
                CraftingFormat.WasNow(_mastery!.GetMythicGiftChance() / (1f + _mastery.GetMythicModifierChanceBonus()), _mastery.GetMythicGiftChance()))
        ];

        private void RefreshAction() => _actionBar?.SetAction(
            Localization.Localize(_mode switch
            {
                CraftingMode.Upgrade => UpgradeActionKey,
                CraftingMode.Recraft => RerollActionKey,
                CraftingMode.Ascend => AscendActionKey,
                _ => CreateActionKey,
            }),
            CanExecute());

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

            _pickerPopup = _uiElements?.ShowPopup(typeof(IPickerPopup)) as IPickerPopup;
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
            if (_pickerPopup is Node node && IsInstanceValid(node)) _pickerPopup.Close();
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
            _recipeTree?.Rebuild();
            RefreshDetails();
        }
    }
}
