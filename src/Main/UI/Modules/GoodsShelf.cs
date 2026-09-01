namespace LastBreath.UI.Modules
{
    using System;
    using System.Collections.Generic;
    using Core.Items;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;
    using SharedUi;

    /// <summary>One tile of the showcase: an offer as the shelf shows it (a buyback row carries
    /// the price the trader paid for it).</summary>
    public record ShelfTile(string OfferId, IItem Item, int UnitPrice, int Count, bool IsBuyback = false);

    /// <summary>The showcase split into its sections. The window does the sorting — the shelf only draws.</summary>
    public record ShelfSections(
        IReadOnlyList<ShelfTile> Consumables,
        IReadOnlyList<ShelfTile> Equipment,
        IReadOnlyList<ShelfTile> Specials,
        IReadOnlyList<ShelfTile> Buyback);

    /// <summary>
    /// The "showcase" column of the trade window: consumables, equipment, the restock-minted
    /// special deliveries (dashed frames — they rotate away with the restock) and the buyback row
    /// (12 sockets, the empty ones dashed placeholders). A tile click travels up as its offer id;
    /// prices, sections and the rotation caption are handed down ready-made.
    /// </summary>
    public partial class GoodsShelf : ScrollContainer
    {
        public const string ConsumablesKey = "UI_Trade_Consumables";
        public const string EquipmentKey = "UI_Trade_Equipment";
        public const string SpecialsKey = "UI_Trade_Specials";
        public const string BuybackKey = "UI_Trade_Buyback";
        private const string PriceKey = "UI_Trade_Price";

        private const int BuybackSockets = 12;
        private const float IconSide = 48f;
        private static readonly Vector2 s_tileSize = new(118, 124);
        private static readonly Color s_tileBg = new(0.055f, 0.05f, 0.042f); // ItemSlotPanel ground
        private static readonly Color s_neutralBorder = new(0.42f, 0.341f, 0.188f); // Umbral GoldBorder

        [Export] private SectionHeader? _consumablesHeader, _equipmentHeader, _specialsHeader, _buybackHeader;
        [Export] private Control? _specialsRow;
        [Export] private Label? _rotationLabel;
        [Export] private HFlowContainer? _consumablesFlow, _equipmentFlow, _specialsFlow, _buybackFlow;

        /// <summary>The composer's tooltip pipeline — the shelf itself never talks to services.</summary>
        public Func<IItem, IPopup?>? ShowTooltip;

        public event Action<string>? OfferClicked;
        public event Action<string>? BuybackClicked;

        public override void _Ready()
        {
            _consumablesHeader?.SetTitle(Localization.Localize(ConsumablesKey));
            _equipmentHeader?.SetTitle(Localization.Localize(EquipmentKey));
            _specialsHeader?.SetTitle(Localization.Localize(SpecialsKey));
            _buybackHeader?.SetTitle(Localization.Localize(BuybackKey));
        }

        /// <summary>The rotation countdown next to the specials header (the window's restock timer).</summary>
        public void SetRotationText(string text) => _rotationLabel?.Text = text;

        public void SetOffers(ShelfSections sections)
        {
            Rebuild(_consumablesFlow, sections.Consumables, dashed: false, id => OfferClicked?.Invoke(id));
            Rebuild(_equipmentFlow, sections.Equipment, dashed: false, id => OfferClicked?.Invoke(id));
            Rebuild(_specialsFlow, sections.Specials, dashed: true, id => OfferClicked?.Invoke(id));
            Rebuild(_buybackFlow, sections.Buyback, dashed: false, id => BuybackClicked?.Invoke(id));
            for (int i = sections.Buyback.Count; i < BuybackSockets; i++)
                _buybackFlow?.AddChild(BuildPlaceholder());

            // Empty authored sections fold away; buyback always shows its sockets.
            SetSectionVisible(_consumablesHeader, _consumablesFlow, sections.Consumables.Count > 0);
            SetSectionVisible(_equipmentHeader, _equipmentFlow, sections.Equipment.Count > 0);
            _specialsRow?.Visible = sections.Specials.Count > 0;
            _specialsFlow?.Visible = sections.Specials.Count > 0;
        }

        private static void SetSectionVisible(Control? header, Control? flow, bool visible)
        {
            header?.Visible = visible;
            flow?.Visible = visible;
        }

        private void Rebuild(Container? flow, IReadOnlyList<ShelfTile> tiles, bool dashed, Action<string> report)
        {
            if (flow == null) return;
            flow.QueueFreeChildren();
            foreach (var tile in tiles)
                flow.AddChild(BuildTile(tile, dashed, report));
        }

        /// <summary>Card tile: rarity-tinted ItemSlotPanel frame, icon centered, name in the rarity
        /// color, price at the bottom, the stack counter in the corner (only when it counts).</summary>
        private Control BuildTile(ShelfTile tile, bool dashed, Action<string> report)
        {
            var rarityColor = Color.FromHtml(TextPalette.RarityColor(tile.Item.Rarity));
            var panel = BuildFrame(dashed ? null : rarityColor);
            if (dashed) panel.AddChild(new DashedFrame { Line = rarityColor, MouseFilter = MouseFilterEnum.Ignore });

            var layout = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            layout.AddThemeConstantOverride("separation", 2);
            layout.AddChild(BuildIcon(tile.Item.Icon));
            layout.AddChild(BuildCaption(tile.Item.DisplayName, rarityColor));
            layout.AddChild(BuildPrice(tile.UnitPrice));
            panel.AddChild(layout);

            if (tile.Count > 1) panel.AddChild(BuildCountCorner(tile.Count));

            panel.GuiInput += @event =>
            {
                if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                report(tile.OfferId);
                panel.AcceptEvent();
            };
            HoverTooltip.Attach(panel, () => ShowTooltip?.Invoke(tile.Item));
            return panel;
        }

        /// <summary>An empty buyback socket: the dashed outline of a sale that has not happened.</summary>
        private static Control BuildPlaceholder()
        {
            var panel = BuildFrame(border: null);
            panel.MouseFilter = MouseFilterEnum.Ignore;
            panel.AddChild(new DashedFrame { Line = s_neutralBorder, MouseFilter = MouseFilterEnum.Ignore });
            return panel;
        }

        /// <summary>The ItemSlotPanel look built by hand — the border color is per-tile (rarity),
        /// which a shared theme stylebox cannot carry.</summary>
        private static PanelContainer BuildFrame(Color? border)
        {
            var style = new StyleBoxFlat { BgColor = s_tileBg };
            style.SetContentMarginAll(6);
            if (border is { } color)
            {
                style.SetBorderWidthAll(1);
                style.BorderColor = color;
            }

            var panel = new PanelContainer { CustomMinimumSize = s_tileSize };
            panel.AddThemeStyleboxOverride("panel", style);
            return panel;
        }

        private static Control BuildIcon(Texture2D? icon)
        {
            var center = new CenterContainer { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            center.AddChild(new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new Vector2(IconSide, IconSide),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            return center;
        }

        private static Label BuildCaption(string name, Color rarityColor)
        {
            var caption = new Label
            {
                Text = name,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            caption.AddThemeColorOverride("font_color", rarityColor);
            caption.AddThemeFontSizeOverride("font_size", 12);
            return caption;
        }

        private static Label BuildPrice(int unitPrice)
        {
            var price = new Label
            {
                Text = Localization.Render(PriceKey, new Dictionary<string, object?> { ["Amount"] = unitPrice }),
                HorizontalAlignment = HorizontalAlignment.Center,
                ThemeTypeVariation = "ValueLabel",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            price.AddThemeFontSizeOverride("font_size", 12);
            return price;
        }

        /// <summary>The xN counter riding the tile's top-right corner.</summary>
        private static Control BuildCountCorner(int count)
        {
            var corner = new Label
            {
                Text = $"x{count}",
                ThemeTypeVariation = "SlotCountLabel",
                GrowHorizontal = GrowDirection.Begin,
                GrowVertical = GrowDirection.End,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            var overlay = new Control { MouseFilter = MouseFilterEnum.Ignore };
            overlay.AddChild(corner);
            corner.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight, Control.LayoutPresetMode.KeepSize, 2);
            return overlay;
        }

        /// <summary>The hairline the theme cannot draw: a dashed border, drawn by hand for the
        /// rotation slots and the empty buyback sockets.</summary>
        private sealed partial class DashedFrame : Control
        {
            public Color Line { get; init; } = s_neutralBorder;

            public override void _Notification(int what)
            {
                if (what == NotificationResized) QueueRedraw();
            }

            public override void _Draw()
            {
                var end = Size - Vector2.One;
                DrawDashedLine(Vector2.One, new Vector2(end.X, 1), Line);
                DrawDashedLine(new Vector2(end.X, 1), end, Line);
                DrawDashedLine(end, new Vector2(1, end.Y), Line);
                DrawDashedLine(new Vector2(1, end.Y), Vector2.One, Line);
            }
        }
    }
}
