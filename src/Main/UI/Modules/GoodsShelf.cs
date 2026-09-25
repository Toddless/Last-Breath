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
        private const string ConsumablesKey = "UI_Trade_Consumables";
        private const string EquipmentKey = "UI_Trade_Equipment";
        private const string SpecialsKey = "UI_Trade_Specials";
        private const string BuybackKey = "UI_Trade_Buyback";

        private const int BuybackSockets = 12;

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

        /// <summary>Card tile: the <see cref="ShelfTileCard"/> scene filled with the offer — the
        /// rarity color tints the name and the frame, dashed for the rotation slots.</summary>
        private Control BuildTile(ShelfTile tile, bool dashed, Action<string> report)
        {
            var card = ShelfTileCard.Initialize().Instantiate<ShelfTileCard>();
            card.SetOffer(tile.Item.Icon, tile.Item.DisplayName,
                Color.FromHtml(TextPalette.RarityColor(tile.Item.Rarity)), tile.UnitPrice, tile.Count);
            card.SetDashed(dashed);
            card.Clicked += () => report(tile.OfferId);
            HoverTooltip.Attach(card, () => ShowTooltip?.Invoke(tile.Item));
            return card;
        }

        /// <summary>An empty buyback socket: the dashed outline of a sale that has not happened.</summary>
        private static Control BuildPlaceholder()
        {
            var card = ShelfTileCard.Initialize().Instantiate<ShelfTileCard>();
            card.SetEmpty();
            return card;
        }
    }
}
