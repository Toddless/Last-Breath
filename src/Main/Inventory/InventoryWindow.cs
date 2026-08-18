namespace LastBreath.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Services;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The character inventory screen (Umbral layout "2a"): an equipment paperdoll and the key
    /// stats on the left, a filter bar (search, rarity chips, type) over the uniform bag grid on
    /// the right. The bag slots are owned by the Inventory service and only borrowed into this
    /// window's grid; filters dim non-matching slots instead of hiding them (the grid is the
    /// physical bag). Right-click an equippable item in the bag to wear it; right-click a filled
    /// paperdoll slot to take the piece off. Drag works both ways.
    /// </summary>
    public partial class InventoryWindow : Control, IWindow
    {
        private const string UID = "uid://byx7g1b2wlwfl";

        private static readonly Color s_filteredOut = new(1f, 1f, 1f, 0.28f);
        private static readonly Rarity[] s_chipOrder =
            [Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary, Rarity.Unique, Rarity.Mythic];

        private static readonly EntityParameter[] s_statParameters =
        [
            EntityParameter.PhysicalDamage,
            EntityParameter.CriticalChance,
            EntityParameter.CriticalDamage,
            EntityParameter.Armor,
            EntityParameter.Evade
        ];

        private static readonly EntityParameter[] s_resistParameters =
        [
            EntityParameter.FireResistance, EntityParameter.ColdResistance, EntityParameter.LightningResistance,
            EntityParameter.PoisonResistance
        ];

        [Export] private Button? _craftingButton, _allStatsButton, _sortButton, _destroyButton;
        [Export] private GridContainer? _inventoryGrid;
        [Export] private Control? _doll;
        [Export] private VBoxContainer? _stats;
        [Export] private Container? _resists;
        [Export] private LineEdit? _search;
        [Export] private OptionButton? _typeFilter;
        [Export] private HBoxContainer? _chips;
        [Export] private Label? _title;
        [Export] private Label? _equipHeader;
        [Export] private Label? _statsHeader;

        private IGameMessageBus? _messageBus;
        private IInventory? _inventory;
        private IPlayerAccessor? _playerAccessor;
        private IUiElementsManager? _uiElementsManager;
        private IParameterFormatProvider? _formats;
        private bool _destroyMode;

        private string _query = string.Empty;
        private readonly HashSet<Rarity> _rarityFilter = [];
        private readonly Dictionary<Rarity, Button> _chipButtons = [];
        private int _typeIndex;


        public override void _Ready()
        {
            _craftingButton?.Pressed += OnCraftingButtonPressed;
            _allStatsButton?.Pressed += () => _uiElementsManager?.ToggleWindow(typeof(UI.CharacterWindow));
            _sortButton?.Pressed += () => Bag?.SortBag();
            _destroyButton?.ToggleMode = true;
            _destroyButton?.Toggled += pressed => _destroyMode = pressed;

            _search?.TextChanged += text => { _query = text; ApplyFilters(); };
            _typeFilter?.ItemSelected += index => { _typeIndex = (int)index; ApplyFilters(); };

            LocalizeStaticLabels();
            BuildTypeOptions();
            BuildRarityChips();
        }

        public override void _ExitTree()
        {
            ResetSlotTints();
            if (Bag != null)
            {
                Bag.DetachSlots();
                Bag.ItemInteraction -= OnItemInteraction;
                Bag.EquipmentDroppedIntoBag -= OnEquipmentDroppedIntoBag;
                Bag.ItemAmountChanges -= OnBagChanged;
            }

            if (Equipment is { } equipment) equipment.EquipmentChanged -= OnEquipmentChanged;
            if (_playerAccessor?.Player != null) _playerAccessor.Player.Parameters.ParameterChanged -= OnParameterChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _inventory = provider.GetService<IInventory>();
            _messageBus = provider.GetService<IGameMessageBus>();
            _playerAccessor = provider.GetService<IPlayerAccessor>();
            _uiElementsManager = provider.GetService<IUiElementsManager>();
            _formats = provider.GetService<IParameterFormatProvider>();

            if (Bag != null && _inventoryGrid != null)
            {
                Bag.AttachSlots(_inventoryGrid);
                Bag.ItemInteraction += OnItemInteraction;
                Bag.EquipmentDroppedIntoBag += OnEquipmentDroppedIntoBag;
                Bag.ItemAmountChanges += OnBagChanged;
            }

            if (Equipment is { } equipment) equipment.EquipmentChanged += OnEquipmentChanged;
            if (_playerAccessor?.Player != null) _playerAccessor.Player.Parameters.ParameterChanged += OnParameterChanged;

            BindDollSlots();
            RenderDoll();
            RenderStats();
            ApplyFilters();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Close() => QueueFree();

        private Inventory? Bag => _inventory as Inventory;

        private IEquipmentComponent? Equipment => _playerAccessor?.Player?.Equipment;

        private IEnumerable<EquipmentSlot> DollSlots => _doll?.GetChildren().OfType<EquipmentSlot>() ?? [];

        private void LocalizeStaticLabels()
        {
            _title?.Text = Localization.Localize("UI_Inventory");
            _sortButton?.Text = Localization.Localize("UI_Inv_Sort");
            _destroyButton?.Text = Localization.Localize("UI_Inv_Destroy");
            _allStatsButton?.Text = Localization.Localize("UI_Character");
            _craftingButton?.Text = Localization.Localize("UI_Crafting");
            _equipHeader?.Text = Localization.Localize("UI_Inv_Equipment");
            _statsHeader?.Text = Localization.Localize("UI_Inv_Stats");
            _search?.PlaceholderText = Localization.Localize("UI_Inv_Search");
        }

        private void BuildTypeOptions()
        {
            if (_typeFilter == null) return;
            _typeFilter.Clear();
            foreach (string key in (string[])["UI_Inv_AllTypes", "UI_Inv_Type_Weapon", "UI_Inv_Type_Armor", "UI_Inv_Type_Jewellery", "UI_Inv_Type_Resources"])
                _typeFilter.AddItem(Localization.Localize(key));
            _typeFilter.Selected = 0;
        }

        /// <summary>An "all" pill plus one coloured dot per rarity. No pressed dot = no rarity filter.</summary>
        private void BuildRarityChips()
        {
            if (_chips == null) return;

            var all = new Button { Text = Localization.Localize("UI_Inv_All"), FocusMode = FocusModeEnum.None };
            all.Pressed += ClearRarityFilter;
            _chips.AddChild(all);

            foreach (var rarity in s_chipOrder)
            {
                var chip = MakeChip(Color.FromHtml(TextPalette.RarityColor(rarity)), rarity.ToString());
                chip.Toggled += pressed =>
                {
                    if (pressed) _rarityFilter.Add(rarity);
                    else _rarityFilter.Remove(rarity);
                    ApplyFilters();
                };
                _chipButtons[rarity] = chip;
                _chips.AddChild(chip);
            }
        }

        private static Button MakeChip(Color color, string tooltip)
        {
            var chip = new Button
            {
                ToggleMode = true,
                FocusMode = FocusModeEnum.None,
                CustomMinimumSize = new Vector2(26, 26),
                TooltipText = tooltip,
            };

            var normal = new StyleBoxFlat { BgColor = new Color(color.R, color.G, color.B, 0.35f) };
            normal.SetBorderWidthAll(1);
            normal.BorderColor = color;
            var pressed = new StyleBoxFlat { BgColor = color };
            pressed.SetBorderWidthAll(2);
            pressed.BorderColor = Colors.White;

            chip.AddThemeStyleboxOverride("normal", normal);
            chip.AddThemeStyleboxOverride("hover", pressed);
            chip.AddThemeStyleboxOverride("pressed", pressed);
            chip.AddThemeStyleboxOverride("hover_pressed", pressed);
            return chip;
        }

        private void ClearRarityFilter()
        {
            _rarityFilter.Clear();
            foreach (var chip in _chipButtons.Values)
                chip.SetPressedNoSignal(false);
            ApplyFilters();
        }

        private void OnCraftingButtonPressed() => _messageBus?.PublishMessageAsync(new OpenCraftingWindowMessage(string.Empty));

        private void OnEquipmentChanged(EquipmentPiece piece, IEquipItem? item)
        {
            RenderDoll();
            RenderStats();
        }

        private void OnBagChanged(string itemId, int total) => ApplyFilters();

        private void OnParameterChanged(EntityParameter parameter, float value) => RenderStats();

        /// <summary>Right-click in the bag wears the piece; whatever it replaced goes back in.
        /// Destroy mode intercepts the left click and disassembles instead (the crafting pipeline
        /// returns a share of the resources and grants shatter experience).</summary>
        private void OnItemInteraction(IItem item, MouseInteractions interaction)
        {
            if (_destroyMode && interaction == MouseInteractions.LeftClick)
            {
                _messageBus?.PublishMessageAsync(new DestroyItemMessage(item.InstanceId));
                return;
            }

            if (interaction != MouseInteractions.RightClick || item is not IEquipItem equipItem) return;
            EquipFromBag(equipItem);
        }

        private void EquipFromBag(IEquipItem equipItem, EquipmentPiece? targetSlot = null)
        {
            if (Equipment is not { } equipment || _inventory == null) return;

            if (!equipment.TryEquip(equipItem, out var replaced, targetSlot)) return;
            _inventory.RemoveItemByInstanceId(equipItem.InstanceId);
            if (replaced != null) _inventory.TryAddItem(replaced);
        }

        /// <summary>Drag of an equipped piece into a bag slot: unequip into that very slot.</summary>
        private void OnEquipmentDroppedIntoBag(EquipmentPiece piece, IInventorySlot slot)
        {
            if (Equipment is not { } equipment || Bag == null) return;
            if (slot.CurrentItem != null && Bag.GetAvailableCapacity() == 0) return;
            if (!equipment.TryUnequip(piece, out var removed) || removed == null) return;
            Bag.TryAddItemAt(removed, slot);
        }

        private bool CanEquipFromBag(EquipmentPiece slot, string instanceId) =>
            Bag?.GetItem<IItem>(instanceId) is IEquipItem item && item.EquipmentPiece == slot.AcceptedItemPiece();

        /// <summary>A drop on a concrete paperdoll slot equips into THAT slot (either ring accepts a ring).</summary>
        private void EquipInstanceFromBag(EquipmentPiece slot, string instanceId)
        {
            if (Bag?.GetItem<IItem>(instanceId) is IEquipItem item) EquipFromBag(item, slot);
        }

        private void OnUnequipPressed(EquipmentPiece piece)
        {
            if (Equipment is not { } equipment || _inventory == null) return;
            if (_inventory.GetAvailableCapacity() == 0) return;
            if (equipment.TryUnequip(piece, out var removed) && removed != null)
                _inventory.TryAddItem(removed);
        }

        private void BindDollSlots()
        {
            foreach (var slot in DollSlots)
            {
                var piece = slot.Piece;
                slot.Bind(instanceId => CanEquipFromBag(piece, instanceId), instanceId => EquipInstanceFromBag(piece, instanceId), OnUnequipPressed);
                HoverTooltip.Attach(slot, () => ShowEquippedTooltip(piece));
            }
        }

        private void RenderDoll()
        {
            foreach (var slot in DollSlots)
                slot.SetEquipped(Equipment?.GetEquipped(slot.Piece));
        }

        /// <summary>Resolved at hover time: the slot may outlive the piece it was built for.</summary>
        private IPopup? ShowEquippedTooltip(EquipmentPiece piece)
        {
            if (Equipment?.GetEquipped(piece) is not { } item) return null;
            var popup = _uiElementsManager?.ShowPopup(typeof(UI.ItemTooltipPopup)) as UI.ItemTooltipPopup;
            popup?.ShowItem(item);
            return popup;
        }

        private void RenderStats()
        {
            if (_stats == null || _playerAccessor?.Player is not { } player) return;

            _stats.QueueFreeChildren();

            AddStatRow(Localization.Localize("Health"), $"{Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(player.Parameters.MaxHealth)}");
            foreach (var parameter in s_statParameters)
                AddStatRow(Localization.Localize(parameter.ToString()), FormatValue(parameter, player));

            RenderResists(player);
        }

        private void AddStatRow(string name, string value)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = name, ThemeTypeVariation = "DimLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill });
            row.AddChild(new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Right });
            _stats?.AddChild(row);
        }

        private void RenderResists(IPlayer player)
        {
            if (_resists == null) return;
            _resists.QueueFreeChildren();

            foreach (var parameter in s_resistParameters)
            {
                var chip = new PanelContainer();
                chip.AddChild(new Label
                {
                    Text = $"{Localization.Localize(parameter.ToString())} {FormatValue(parameter, player)}",
                    ThemeTypeVariation = "DimLabel",
                });
                _resists.AddChild(chip);
            }
        }

        private string FormatValue(EntityParameter parameter, IPlayer player) =>
            ParameterValueText.Format(_formats, parameter, player.Parameters.GetValueForParameter(parameter));

        /// <summary>Dims the bag slots that fall out of the current search/rarity/type filter.
        /// The slots stay in place (the grid IS the bag) — filtered-out items just fade.</summary>
        private void ApplyFilters()
        {
            if (_inventoryGrid == null || _inventory == null) return;

            bool filterActive = _query.Length > 0 || _rarityFilter.Count > 0 || _typeIndex > 0;
            foreach (var child in _inventoryGrid.GetChildren())
            {
                if (child is not Slot slot) continue;
                var item = slot.CurrentItem == null ? null : _inventory.GetItem<IItem>(slot.CurrentItem.InstanceId);
                slot.Modulate = filterActive && item != null && !Matches(item) ? s_filteredOut : Colors.White;
            }
        }

        private bool Matches(IItem item)
        {
            if (_query.Length > 0 && !item.DisplayName.Contains(_query, StringComparison.OrdinalIgnoreCase)) return false;
            if (_rarityFilter.Count > 0 && !_rarityFilter.Contains(item.Rarity)) return false;
            return MatchesType(item);
        }

        private bool MatchesType(IItem item) => _typeIndex switch
        {
            1 => item is IEquipItem { EquipmentPiece: EquipmentPiece.Weapon },
            2 => item is IEquipItem
            {
                EquipmentPiece: EquipmentPiece.Body or EquipmentPiece.Belt or EquipmentPiece.Gloves
                or EquipmentPiece.Boots or EquipmentPiece.Helmet or EquipmentPiece.Cloak,
            },
            3 => item is IEquipItem { EquipmentPiece: EquipmentPiece.Amulet or EquipmentPiece.Ring },
            4 => item is not IEquipItem,
            _ => true,
        };

        /// <summary>The slot nodes survive this window (service-owned) — a dying window must not
        /// leave its filter tint behind for the next borrower.</summary>
        private void ResetSlotTints()
        {
            if (_inventoryGrid == null) return;
            foreach (var child in _inventoryGrid.GetChildren())
                if (child is Slot slot)
                    slot.Modulate = Colors.White;
        }
    }
}
