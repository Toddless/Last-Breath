namespace LastBreath.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Trade;
    using Core.Views.UI;
    using Godot;
    using Modules;
    using SharedUi;

    /// <summary>
    /// The trader's shop (Umbral three-column layout), a thin composer over module scenes: the
    /// showcase shelf on the left, the deal card in the middle, the borrowed bag grid on the
    /// right with the player's gold in its footer. The window maps offers into shelf sections,
    /// keeps the restock countdown ticking and decides the deal's direction — a shelf tile picks
    /// a purchase, a bag click picks a sale, the one confirm button fires the matching request.
    /// The window only MIRRORS availability: the Buy/Sell request handlers are the gates.
    /// </summary>
    public partial class TradeWindow : Control, IWindow
    {
        public const string ShowcaseKey = "UI_Trade_Showcase";
        public const string DealKey = "UI_Trade_Deal";
        public const string InventoryKey = "UI_Inventory";
        public const string ClickToSellKey = "UI_Trade_ClickToSell";
        public const string GoldSuffixKey = "UI_Trade_GoldSuffix";
        public const string RestockKey = "UI_Trade_Restock";
        public const string RotationKey = "UI_Trade_Rotation";

        private const string UID = "uid://cix0yesodqrxr";
        private const string NoDeadline = "--:--";
        private const double GameMinutesPerDay = 1440;

        [Export] private WindowHeader? _header;
        [Export] private Label? _restockLabel;
        [Export] private SectionHeader? _showcaseHeader, _dealHeader, _bagHeader;
        [Export] private GoodsShelf? _goodsShelf;
        [Export] private DealColumn? _dealColumn;
        [Export] private BagGrid? _bagGrid;
        [Export] private Label? _sellHint, _goldFooter;
        [Export] private Timer? _restockTimer;

        private IGameMessageBus? _messageBus;
        private IInventory? _inventory;
        private IItemInteractionSource? _bagClicks;
        private ISlotLender? _bagLender;
        private ITraderService? _traderService;
        private TradePricing? _pricing;
        private IWalletService? _wallet;
        private IUiElementsManager? _uiElements;
        private IWorldClock? _clock;

        private string _traderId = string.Empty;
        private string? _pendingOfferId;
        private string? _pendingSaleId;

        /// <summary>The open shop, if any: the item tooltip asks it for a sell quote.
        /// Fresh-instance policy keeps it single.</summary>
        public static TradeWindow? Active { get; private set; }

        /// <summary>Shopping freezes walking exactly like a conversation (the window outlives
        /// the dialogue that opened it, so the Dialogue context can't cover it).</summary>
        public bool BlocksMovement => true;

        public override void _Ready()
        {
            _header?.Closed += Close;
            _showcaseHeader?.SetTitle(Localization.Localize(ShowcaseKey));
            _dealHeader?.SetTitle(Localization.Localize(DealKey));
            _bagHeader?.SetTitle(Localization.Localize(InventoryKey));
            _sellHint?.Text = Localization.Localize(ClickToSellKey);
            _restockTimer?.Timeout += UpdateRestockTimer;
            Active = this;
        }

        public override void _ExitTree()
        {
            if (Active == this) Active = null;
            if (_wallet != null) _wallet.GoldChanged -= OnGoldChanged;
            if (_bagClicks != null) _bagClicks.ItemInteraction -= OnBagItemInteraction;
            _bagGrid?.Detach();
        }

        /// <summary>What THIS trader pays for the item right now (perk included); null when the item
        /// is untradable — the tooltip shows no price line then.</summary>
        public int? QuoteSellPrice(IItem item)
        {
            var trader = _traderService?.GetTrader(_traderId);
            if (trader == null || _pricing == null) return null;
            int price = _pricing.SellPrice(item, trader.Fraction);
            return price > 0 ? price : null;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _messageBus = provider.GetService<IGameMessageBus>();
            _inventory = provider.GetService<IInventory>();
            _bagClicks = provider.GetService<IItemInteractionSource>();
            _bagLender = provider.GetService<ISlotLender>();
            _traderService = provider.GetService<ITraderService>();
            _pricing = provider.GetService<TradePricing>();
            _wallet = provider.GetService<IWalletService>();
            _uiElements = provider.GetService<IUiElementsManager>();
            _clock = provider.GetService<IWorldClock>();

            _wallet?.GoldChanged += OnGoldChanged;
            _bagClicks?.ItemInteraction += OnBagItemInteraction;
            if (_bagLender is { } lender) _bagGrid?.Attach(lender);

            if (_goodsShelf is { } shelf)
            {
                shelf.ShowTooltip = ShowOfferTooltip;
                shelf.OfferClicked += OnOfferClicked;
                shelf.BuybackClicked += OnOfferClicked;
            }

            _dealColumn?.Confirmed += OnDealConfirmed;
            OnGoldChanged(_wallet?.Gold ?? 0);
        }

        public void SetTrader(string traderId)
        {
            _traderId = traderId;
            _header?.SetTitle(Localization.Localize(traderId));
            ClearDeal();
            RefreshStock();
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Reads the shelf and deals it onto the showcase: buyback to its sockets, the
        /// restock-minted equips to the specials, the authored rest split equip / consumable.</summary>
        private void RefreshStock()
        {
            if (_goodsShelf == null || _traderService == null) return;

            var trader = _traderService.GetTrader(_traderId);
            if (trader == null) return;

            var stock = _traderService.GetStock(_traderId);
            List<ShelfTile> consumables = [], equipment = [], specials = [], buyback = [];
            foreach (var offer in stock)
            {
                int unitPrice = offer.IsBuyback
                    ? offer.BuybackUnitPrice
                    : _pricing?.BuyPrice(offer.Item, trader.Fraction) ?? 0;
                var section = offer.IsBuyback ? buyback
                    : offer.IsRandomEquip ? specials
                    : offer.Item is IEquipItem ? equipment
                    : consumables;
                section.Add(new ShelfTile(offer.OfferId, offer.Item, unitPrice, offer.Remaining, offer.IsBuyback));
            }

            _goodsShelf.SetOffers(new ShelfSections(consumables, equipment, specials, buyback));

            // A refresh can carry the picked offer away (restock, sold out) — the deal must not
            // keep promising an id the shelf no longer holds.
            if (_pendingOfferId is { } pending && stock.All(offer => offer.OfferId != pending))
                ClearDeal();

            UpdateRestockTimer();
        }

        /// <summary>A shelf tile was picked (buyback included): the deal turns into a purchase.</summary>
        private void OnOfferClicked(string offerId)
        {
            var trader = _traderService?.GetTrader(_traderId);
            var offer = _traderService?.GetStock(_traderId).FirstOrDefault(entry => entry.OfferId == offerId);
            if (trader == null || offer == null || _dealColumn == null) return;

            _pendingOfferId = offerId;
            _pendingSaleId = null;
            int unitPrice = offer.IsBuyback ? offer.BuybackUnitPrice : _pricing?.BuyPrice(offer.Item, trader.Fraction) ?? 0;
            int maxAmount = offer.Item.MaxStackSize > 1 ? offer.Remaining : 1;
            _dealColumn.ShowPurchase(new DealItemView(offer.Item.Icon, offer.Item.DisplayName, RarityColorOf(offer.Item), unitPrice, maxAmount));
        }

        /// <summary>A click on a held bag item puts it into the deal as a sale (the old instant
        /// right-click sale moved here — the confirm button is the only thing that sells).</summary>
        private void OnBagItemInteraction(IItem item, MouseInteractions interaction)
        {
            // The old Ctrl+RMB "sell the stack" gesture folds into the same selection.
            if (interaction is not (MouseInteractions.LeftClick or MouseInteractions.RightClick or MouseInteractions.CtrRmb)) return;
            if (_dealColumn == null) return;

            _pendingSaleId = item.InstanceId;
            _pendingOfferId = null;
            int unitPrice = QuoteSellPrice(item) ?? 0;
            int maxAmount = item.MaxStackSize > 1 ? Math.Max(1, _inventory?.GetTotalItemAmount(item.Id) ?? 1) : 1;
            _dealColumn.ShowSale(new DealItemView(item.Icon, item.DisplayName, RarityColorOf(item), unitPrice, maxAmount));
        }

        private void OnDealConfirmed(int amount)
        {
            if (_pendingOfferId is { } offerId) _ = BuyAsync(offerId, amount);
            else if (_pendingSaleId is { } instanceId) _ = SellAsync(instanceId, amount);
        }

        private async Task BuyAsync(string offerId, int amount)
        {
            if (_messageBus == null) return;
            await _messageBus.SendRequest<BuyItemRequest, int>(new(_traderId, offerId, amount));
            // 0 = refused (gold/bag/shelf) — the refreshed shelf and gold footer tell the story.
            ClearDeal();
            RefreshStock();
        }

        private async Task SellAsync(string instanceId, int amount)
        {
            if (_messageBus == null) return;
            await _messageBus.SendRequest<SellItemRequest, int>(new(_traderId, instanceId, amount));
            ClearDeal();
            RefreshStock(); // the buyback sockets just grew (or the sale was refused — same story)
        }

        private void ClearDeal()
        {
            _pendingOfferId = null;
            _pendingSaleId = null;
            _dealColumn?.ShowEmpty();
        }

        /// <summary>Once a second (and on every refresh): the restock deadline rendered as a real
        /// wall-clock countdown. A deadline already passed refreshes the shelf — the lazy restock
        /// materializes there and reschedules itself.</summary>
        private void UpdateRestockTimer()
        {
            string countdown = NoDeadline;
            if (_clock != null && _traderService?.GetNextRestockMinutes(_traderId) is { } nextMinutes)
            {
                double remainingGameMinutes = nextMinutes - (_clock.Day * GameMinutesPerDay + _clock.MinuteOfDay);
                if (remainingGameMinutes <= 0)
                {
                    RefreshStock(); // re-enters with the fresh deadline
                    return;
                }

                countdown = FormatCountdown(remainingGameMinutes * _clock.RealSecondsPerGameMinute);
            }

            var values = new Dictionary<string, object?> { ["Time"] = countdown };
            _restockLabel?.Text = Localization.Render(RestockKey, values);
            _goodsShelf?.SetRotationText(Localization.Render(RotationKey, values));
        }

        private static string FormatCountdown(double realSeconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, realSeconds));
            return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}";
        }

        private static Color RarityColorOf(IItem item) => Color.FromHtml(TextPalette.RarityColor(item.Rarity));

        private IPopup? ShowOfferTooltip(IItem item)
        {
            if (_uiElements?.ShowPopup(typeof(ItemTooltipPopup)) is not ItemTooltipPopup popup) return null;
            popup.ShowItem(item);
            return popup;
        }

        private void OnGoldChanged(int gold)
        {
            _goldFooter?.Text = Localization.Render(GoldSuffixKey, new Dictionary<string, object?> { ["Amount"] = gold });
            _dealColumn?.SetAvailableGold(gold);
        }
    }
}
