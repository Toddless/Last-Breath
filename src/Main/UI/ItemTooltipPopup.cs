namespace LastBreath.UI
{
    using Core.Constants;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.Services;
    using Core.Views.UI;
    using Crafting.Source.UIElements;
    using Godot;

    /// <summary>
    /// Hover tooltip of an item (bag slots, equipment rows), Umbral layout "1a": a large framed
    /// icon on the left with the implicits beside it, then the rolled modifiers, grants and lore
    /// in ruled sections below. The name and the top accent line carry the rarity colour. Holding
    /// the RevealRanges action (default Ctrl) appends each rolled line's spread ("+47 Strength
    /// (40–60)"). Plain items show name, icon and description only.
    /// </summary>
    [GlobalClass]
    public partial class ItemTooltipPopup : Control, IHoverTooltipPopup
    {
        private const string ScenePath = "uid://di3yh40oyvjhe";

        // An autowrap Label with no width floor measures its HEIGHT at its narrowest width
        // (≈ one word per line) during the container's min-size pass, which balloons the panel
        // to fill the screen. Pinning the width makes it wrap — and measure — at the real
        // content width. Beside the 168px icon the rows are narrower than the full-width ones.
        private const int HeaderLineWidth = 240;
        private const int FullLineWidth = 436;
        private const float CraftButtonsGap = 16f;

        private static readonly Color s_effectColor = new(0.9f, 0.81f, 0.58f);
        private static readonly Color s_baseStatColor = new(0.79f, 0.66f, 0.38f);

        [Export] private PanelContainer? _panel;
        [Export] private ColorRect? _accent;
        [Export] private TextureRect? _icon;
        [Export] private Label? _title;
        [Export] private Label? _subtitle;
        [Export] private VBoxContainer? _baseStats;
        [Export] private Control? _implicitsSection;
        [Export] private Label? _implicitsHeader;
        [Export] private VBoxContainer? _implicits;
        [Export] private Control? _modsSection;
        [Export] private Label? _modsHeader;
        [Export] private VBoxContainer? _mods;
        [Export] private Control? _effectSection;
        [Export] private Label? _effectHeader;
        [Export] private VBoxContainer? _effects;
        [Export] private Control? _loreSection;
        [Export] private Label? _lore;

        private IItem? _item;
        private bool _revealRanges;
        private InventorySlotTooltipButtons? _craftButtons;
        private IParameterFormatProvider? _formats;

        public PopupLifetime Lifetime => PopupLifetime.WhileHovered;

        public OverlayRegion Region => OverlayRegion.Cursor;

        public bool IsPinned { get; private set; }

        public override void _Ready()
        {
            _implicitsHeader?.Text = Localization.Localize("UI_Item_Implicits").ToUpper();
            _modsHeader?.Text = Localization.Localize("UI_Item_Modifiers").ToUpper();
            _effectHeader?.Text = Localization.Localize("UI_Item_Effect").ToUpper();
            HoverTooltipMotion.Setup(this, _panel);
        }

        public override void _Process(double delta)
        {
            if (!IsPinned) HoverTooltipMotion.Follow(this, _panel);
        }

        // Input arrives through overrides (no bus/event subscriptions), so the fresh-instance
        // policy needs no _ExitTree unhook — the callbacks die with the node.
        // Two independent keys share this handler: ui_reveal_ranges (default Ctrl) toggles the
        // spreads, the pin toggle listens for its own key (Alt) inside TogglePin.
        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.RevealRanges)) SetRevealRanges(true);
            else if (@event.IsActionReleased(Settings.RevealRanges)) SetRevealRanges(false);

            bool pinned = HoverTooltipMotion.TogglePin(@event, IsPinned);
            // Un-pinning dismisses the tooltip: once the pointer has left its source a pinned popup is
            // orphaned (HoverTooltip.Attach dropped its handle on MouseExited), so resuming cursor-follow
            // would make it chase the mouse forever with nothing left to close it.
            if (IsPinned && !pinned) { Close(); return; }
            IsPinned = pinned;
            if (IsPinned) AttachCraftButtons();
        }

        /// <summary>Pinned state grows the craft action row (upgrade/recraft/ascend/destroy) for bag
        /// items — the popup ignores the mouse while it follows the cursor, so the filters open up
        /// only here. Equipped items resolve through the bag inventory as null → no buttons (unequip
        /// first, by design). The row dies with the popup; its actions close the tooltip themselves.</summary>
        private void AttachCraftButtons()
        {
            if (_craftButtons != null || _item == null || _panel == null) return;

            var services = GameServiceProvider.Instance;
            if (services.GetService<IInventory>()?.GetItem<IEquipItem>(_item.InstanceId) == null) return;

            _craftButtons = InventorySlotTooltipButtons.Initialize().Instantiate<InventorySlotTooltipButtons>();
            _craftButtons.InjectServices(services);
            _craftButtons.SetItemInstanceId(_item.InstanceId);
            _craftButtons.Close += Close;
            // The row is a detached strip BELOW the panel, not part of it. Placement resolves
            // deferred: the row's size is unknown until its buttons enter the tree.
            AddChild(_craftButtons);
            Callable.From(PlaceCraftButtons).CallDeferred();

            MouseFilter = MouseFilterEnum.Pass;
            _panel.MouseFilter = MouseFilterEnum.Stop;
        }

        private void PlaceCraftButtons()
        {
            if (_craftButtons == null || _panel == null) return;
            _craftButtons.Position = _panel.Position + new Vector2(0, _panel.Size.Y + CraftButtonsGap);
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath);

        public void ShowItem(IItem item)
        {
            _item = item;
            // The key may already be held when the tooltip spawns mid-hover.
            _revealRanges = InputMap.HasAction(Settings.RevealRanges) && Input.IsActionPressed(Settings.RevealRanges);

            var rarityColor = Color.FromHtml(TextPalette.RarityColor(item.Rarity));
            _title?.Text = item is IEquipItem { UpdateLevel: > 0 } upgraded
                ? $"{item.DisplayName} +{upgraded.UpdateLevel}"
                : item.DisplayName;
            _title?.AddThemeColorOverride("font_color", rarityColor);
            _accent?.Color = rarityColor;
            _icon?.Texture = item.Icon;

            _subtitle?.Text = Subtitle(item);
            RenderLines();
        }

        /// <summary>Re-renders the line lists only — header and geometry stay put, so toggling the
        /// reveal mid-hover doesn't make the tooltip jump.</summary>
        /// <summary>The weapon subtitle names the actual weapon (type + grip) instead of the generic
        /// "Weapon" piece; everything else keeps its equipment piece or the bare rarity.</summary>
        private static string Subtitle(IItem item) => item switch
        {
            IWeaponItem weapon =>
                $"{item.Rarity} · {Localization.Localize($"WeaponType_{weapon.WeaponType}")} · {Localization.Localize($"Handedness_{weapon.Handedness}")}",
            IEquipItem equip => $"{item.Rarity} · {equip.EquipmentPiece}",
            _ => item.Rarity.ToString(),
        };

        private void RenderLines()
        {
            if (_item == null) return;
            ClearRows(_baseStats);
            ClearRows(_implicits);
            ClearRows(_mods);
            ClearRows(_effects);

            RenderWeaponStats(_item as IWeaponItem);
            if (_item is IEquipItem equipItem) RenderEquipLines(equipItem);
            else HideEquipSections();
            RenderDescription(_item);
        }

        /// <summary>The weapon's own base stats (effective damage — upgrade-scaled and locally
        /// modified — plus the crit pair that replaces the owner's base values while equipped).</summary>
        private void RenderWeaponStats(IWeaponItem? weapon)
        {
            _baseStats?.Visible = weapon != null;
            if (weapon == null) return;

            AddBaseStatRow(EntityParameter.Damage, weapon.Damage);
            AddBaseStatRow(EntityParameter.CriticalChance, weapon.CriticalChance);
            AddBaseStatRow(EntityParameter.CriticalDamage, weapon.CriticalDamage);
        }

        private void AddBaseStatRow(EntityParameter parameter, float value)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = Localization.Localize(parameter.ToString()),
                ThemeTypeVariation = "DimLabel",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            var valueLabel = new Label { Text = FormatParameter(parameter, value), HorizontalAlignment = HorizontalAlignment.Right };
            valueLabel.AddThemeColorOverride("font_color", s_baseStatColor);
            row.AddChild(valueLabel);
            _baseStats?.AddChild(row);
        }

        /// <summary>Same source of truth the character sheet uses: ParameterFormats.json decides
        /// which parameters are fractions-as-percent.</summary>
        private string FormatParameter(EntityParameter parameter, float value)
        {
            _formats ??= GameServiceProvider.Instance.GetService<IParameterFormatProvider>();
            return _formats?.GetUnit(parameter) == ParameterUnit.Percent
                ? $"{(value * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}%"
                : value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void ClearRows(VBoxContainer? container)
        {
            if (container == null) return;
            foreach (var child in container.GetChildren())
            {
                container.RemoveChild(child);
                child.QueueFree();
            }
        }

        private void HideEquipSections()
        {
            _implicitsSection?.Visible = false;
            _modsSection?.Visible = false;
            _effectSection?.Visible = false;
        }

        private void RenderEquipLines(IEquipItem item)
        {
            var implicits = EquipItemLines.ComposeImplicits(item);
            foreach (var line in implicits)
                AddLine(_implicits, Pick(line), HeaderLineWidth, dim: true);
            _implicitsSection?.Visible = implicits.Count > 0;

            var rolled = EquipItemLines.ComposeRolled(item);
            foreach (var line in rolled)
                AddLine(_mods, Pick(line), FullLineWidth);
            _modsSection?.Visible = rolled.Count > 0;

            foreach (var grant in item.Grants)
                AddLine(_effects, Localization.Localize(grant.Id), FullLineWidth, color: s_effectColor);
            _effectSection?.Visible = item.Grants.Count > 0;
        }

        private string Pick(EquipItemLine line) => _revealRanges ? line.RevealedText ?? line.Text : line.Text;

        private void SetRevealRanges(bool reveal)
        {
            if (_revealRanges == reveal) return;
            _revealRanges = reveal;
            RenderLines();
        }

        private void RenderDescription(IItem item)
        {
            bool hasLore = !string.IsNullOrEmpty(item.Description) && item.Description != $"{item.Id}_Description";
            _loreSection?.Visible = hasLore;
            if (hasLore) _lore?.Text = item.Description;
        }

        private static void AddLine(VBoxContainer? container, string text, int width, bool dim = false, Color? color = null)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(width, 0),
            };
            if (dim) label.ThemeTypeVariation = "DimLabel";
            if (color != null) label.AddThemeColorOverride("font_color", color.Value);
            container?.AddChild(label);
        }
    }
}
