namespace LastBreath.UI
{
    using System.Collections.Generic;
    using Core;
    using Core.Constants;
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
    public partial class ItemTooltipPopup : Control, IHoverTooltipPopup, Core.Views.IItemTooltipPopup
    {
        private const string ScenePath = "uid://di3yh40oyvjhe";

        // An autowrap Label with no width floor measures its HEIGHT at its narrowest width
        // (≈ one word per line) during the container's min-size pass, which balloons the panel
        // to fill the screen. Pinning the width makes it wrap — and measure — at the real
        // content width. Beside the 168px icon the rows are narrower than the full-width ones.
        private const int HeaderLineWidth = 240;
        private const int FullLineWidth = 436;
        private const float CraftButtonsGap = 16f;

        // The mythic card: the frame eats the row's width (padding on both sides plus the mark and its gap),
        // so its label wraps narrower than a plain line.
        private const string MythicMark = "◆";
        private const int MythicCardPadding = 10;
        private const int MythicCardGap = 10;
        private const int MythicLineWidth = FullLineWidth - 60;

        private static readonly Color s_effectColor = new(0.9f, 0.81f, 0.58f);
        private static readonly Color s_baseStatColor = Color.FromHtml(TextPalette.BaseStat);

        // RichTextLabel ignores the DimLabel variation (it targets Label), so the grant description
        // gets its dim tone from a default_color override that mirrors DimLabel's colour.
        private static readonly Color s_effectDescColor = new(0.55f, 0.51f, 0.44f);

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
        // spreads, the pin toggle listens for its own key (Alt) inside HandlePinInput.
        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.RevealRanges)) SetRevealRanges(true);
            else if (@event.IsActionReleased(Settings.RevealRanges)) SetRevealRanges(false);

            IsPinned = HoverTooltipMotion.HandlePinInput(this, @event, IsPinned);
            if (IsPinned) AttachCraftButtons();
        }

        /// <summary>Pinned state grows the craft action row (upgrade/recraft/ascend/destroy for
        /// equips, the use button for anything an IItemUseBehavior claims) for bag items — the
        /// popup ignores the mouse while it follows the cursor, so the filters open up only here.
        /// Equipped items resolve through the bag inventory as null → no buttons (unequip first,
        /// by design). The row dies with the popup; its actions close the tooltip themselves.</summary>
        private void AttachCraftButtons()
        {
            if (_craftButtons != null || _item == null || _panel == null) return;

            var services = GameServiceProvider.Instance;
            var bagItem = services.GetService<IInventory>()?.GetItem<IItem>(_item.InstanceId);
            if (bagItem == null) return;
            if (bagItem is not IEquipItem && services.GetService<Core.Items.Use.IItemUseService>()?.BehaviorFor(bagItem) == null) return;

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

        /// <summary>The action strip prefers the space BELOW the panel; a tooltip pinned at the
        /// bottom of the screen (last bag rows) flips it above instead of pushing it off-screen.</summary>
        private void PlaceCraftButtons()
        {
            if (_craftButtons == null || _panel == null) return;
            float below = _panel.Position.Y + _panel.Size.Y + CraftButtonsGap;
            bool fitsBelow = below + _craftButtons.Size.Y <= GetViewportRect().Size.Y;
            _craftButtons.Position = fitsBelow
                ? _panel.Position + new Vector2(0, _panel.Size.Y + CraftButtonsGap)
                : _panel.Position - new Vector2(0, _craftButtons.Size.Y + CraftButtonsGap);
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

            _subtitle?.Text = Subtitle(item) + SellQuoteSuffix(item);
            RenderLines();
        }

        /// <summary>With a shop open the subtitle carries what THIS trader pays (perk included);
        /// closed shop or an untradable item add nothing.</summary>
        private static string SellQuoteSuffix(IItem item) =>
            TradeWindow.Active?.QuoteSellPrice(item) is { } price
                ? $" · {Localization.Render("UI_Trade_SellQuote", new Dictionary<string, object?> { ["Amount"] = price })}"
                : string.Empty;

        /// <summary>The weapon subtitle names the actual weapon (type + grip) instead of the generic
        /// "Weapon" piece; everything else keeps its equipment piece or the bare rarity.</summary>
        private static string Subtitle(IItem item) => item switch
        {
            IWeaponItem weapon => EquipItemText.WeaponSubtitle(item.Rarity.ToString(), weapon.WeaponType, weapon.Handedness),
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

            RenderItemStats(_item as IEquipItem);
            if (_item is IEquipItem equipItem) RenderEquipLines(equipItem);
            else HideEquipSections();
            RenderDescription(_item);
        }

        /// <summary>The item's own base stats: effective values with the LOCAL lines folded in.
        /// Weapons open with their combat triple; every equip then adds one row per typed base stat
        /// (armor's evade, a ring's health). A locally modified stat glows brighter, and the Ctrl
        /// reveal splits it into "base + local contribution" ("6% + 4%") instead of the folded
        /// total. Implicits stay a separate section for the SPECIAL authored lines only.</summary>
        private void RenderItemStats(IEquipItem? item)
        {
            _baseStats?.Visible = item is IWeaponItem || item?.BaseStats.Count > 0;
            if (item == null) return;

            if (item is IWeaponItem weapon)
            {
                AddBaseStatRow(EntityParameter.PhysicalDamage, weapon);
                AddBaseStatRow(EntityParameter.CriticalChance, weapon);
                AddBaseStatRow(EntityParameter.CriticalDamage, weapon);
            }

            foreach (var parameter in item.BaseStats.Keys)
                AddBaseStatRow(parameter, item.GetBaseStatBreakdown(parameter));
        }

        private void AddBaseStatRow(EntityParameter parameter, IWeaponItem weapon) =>
            AddBaseStatRow(parameter, weapon.GetStatBreakdown(parameter));

        private void AddBaseStatRow(EntityParameter parameter, (float Base, float LocalBonus) breakdown)
        {
            (float baseValue, float localBonus) = breakdown;
            bool locallyModified = System.MathF.Abs(localBonus) > 0.0001f;

            string text = locallyModified && _revealRanges
                ? $"{FormatParameter(parameter, baseValue)} {(localBonus >= 0 ? "+" : "−")} {FormatParameter(parameter, System.MathF.Abs(localBonus))}"
                : FormatParameter(parameter, baseValue + localBonus);

            var row = SharedUi.KeyValueRow.Initialize().Instantiate<SharedUi.KeyValueRow>();
            row.Set(Localization.Localize(parameter.ToString()), text,
                locallyModified ? Color.FromHtml(TextPalette.Number) : s_baseStatColor);
            _baseStats?.AddChild(row);
        }

        /// <summary>Same source of truth the character sheet uses: ParameterFormats.json decides
        /// which parameters are fractions-as-percent.</summary>
        private string FormatParameter(EntityParameter parameter, float value)
        {
            _formats ??= GameServiceProvider.Instance.GetService<IParameterFormatProvider>();
            return ParameterValueText.Format(_formats, parameter, value);
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

            RenderRolledLines(item);
            RenderGrants(item);
        }

        /// <summary>The rolled block, split by slot family: a caption opens each family (the rows arrive
        /// already ordered — prefixes, suffixes, leftovers, gift), and the ascension gift closes the section
        /// as a framed card instead of a plain row. The card needs no caption: the frame IS the label.</summary>
        private void RenderRolledLines(IEquipItem item)
        {
            var rolled = EquipItemLines.ComposeRolled(item);
            AffixKind? block = null;
            foreach (var line in rolled)
            {
                if (line.Affix != block)
                {
                    block = line.Affix;
                    var header = line.Affix == AffixKind.Mythic ? null : ItemLineRows.AffixHeader(line.Affix);
                    if (header != null) _mods?.AddChild(header);
                }

                if (line.Affix == AffixKind.Mythic) _mods?.AddChild(MythicCard(Pick(line)));
                else AddLine(_mods, Pick(line), FullLineWidth);
            }

            _modsSection?.Visible = rolled.Count > 0;
        }

        /// <summary>Each grant is a titled block in the Effect section: the name in the effect accent, then
        /// its rendered description beneath (rich text — numbers coloured, {@keyword} links). Modifier grants
        /// carry no description, so the name stands alone exactly as before.</summary>
        private void RenderGrants(IEquipItem item)
        {
            foreach (var grant in item.Grants) AddGrant(grant);
            _effectSection?.Visible = item.Grants.Count > 0;
        }

        private void AddGrant(IItemGrant grant)
        {
            var block = new VBoxContainer();
            block.AddThemeConstantOverride("separation", 2);
            AddLine(block, Localization.Localize(grant.Id), FullLineWidth, color: s_effectColor);
            if (!string.IsNullOrEmpty(grant.Description)) AddGrantDescription(block, grant.Description);
            _effects?.AddChild(block);
        }

        /// <summary>A grant description is BBCode, so it needs a RichTextLabel — a plain Label would print the
        /// raw tags. The width is pinned like every other line so the autowrap measures at the real content
        /// width instead of ballooning the panel; keyword links open the reference card when the popup is pinned.</summary>
        private static void AddGrantDescription(VBoxContainer container, string description)
        {
            var label = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = description,
                FitContent = true,
                ScrollActive = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(FullLineWidth, 0),
            };
            label.AddThemeColorOverride("default_color", s_effectDescColor);
            KeywordLinks.Attach(label);
            container.AddChild(label);
        }

        /// <summary>The ascension gift wears the item's own mythic rarity colour as a framed card — the one
        /// line that is neither prefix nor suffix reads as a thing apart at a glance.</summary>
        private static Control MythicCard(string text)
        {
            var accent = Color.FromHtml(TextPalette.RarityColor(Rarity.Mythic));
            var frame = new StyleBoxFlat
            {
                BgColor = accent with { A = 0.09f },
                BorderColor = accent with { A = 0.32f },
                ContentMarginLeft = MythicCardPadding,
                ContentMarginRight = MythicCardPadding,
                ContentMarginTop = MythicCardPadding,
                ContentMarginBottom = MythicCardPadding,
            };
            frame.SetBorderWidthAll(1);
            frame.SetCornerRadiusAll(2);

            var card = new PanelContainer();
            card.AddThemeStyleboxOverride("panel", frame);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            var mark = new Label { Text = MythicMark };
            mark.AddThemeColorOverride("font_color", accent);
            row.AddChild(mark);
            row.AddChild(new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(MythicLineWidth, 0),
            });
            card.AddChild(row);

            // The section's rows sit 2px apart — the card is a block, not a row, and needs air above it.
            var spaced = new MarginContainer();
            spaced.AddThemeConstantOverride("margin_top", MythicCardGap);
            spaced.AddChild(card);
            return spaced;
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
