namespace LastBreath.UI
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Services;
    using Core.Trade;
    using Core.Views;
    using Core.Views.UI;
    using Godot;
    using Bag = Inventory.Inventory;

    /// <summary>
    /// The trader's shop (Umbral trade prototype): the shelf on the left (authored stock, random
    /// equips, buyback section), the player's bag grid on the right, gold in the header. The
    /// window only MIRRORS availability — the Buy/Sell request handlers are the gates. Buying:
    /// the row's button (stackables carry a quantity spinner). Selling: right-click a bag slot
    /// sells one unit, Ctrl+right-click sells the whole stack; sold goods appear under Buyback.
    /// </summary>
    public partial class TradeWindow : Control, IWindow
    {
        private const string UID = "uid://cix0yesodqrxr";

        [Export] private SharedUi.WindowHeader? _header;
        [Export] private Label? _goldLabel;
        [Export] private VBoxContainer? _offersContainer;
        [Export] private GridContainer? _inventoryGrid;
        [Export] private Label? _goodsLabel;
        [Export] private Label? _bagLabel;

        private IGameMessageBus? _messageBus;
        private IInventory? _inventory;
        private ITraderService? _traderService;
        private TradePricing? _pricing;
        private IWalletService? _wallet;
        private IUiElementsManager? _uiElements;
        private string _traderId = string.Empty;

        private Bag? BagService => _inventory as Bag;

        /// <summary>The open shop, if any: the item tooltip asks it for a sell quote.
        /// Fresh-instance policy keeps it single.</summary>
        public static TradeWindow? Active { get; private set; }

        /// <summary>Shopping freezes walking exactly like a conversation (the window outlives
        /// the dialogue that opened it, so the Dialogue context can't cover it).</summary>
        public bool BlocksMovement => true;


        public override void _Ready()
        {
            _header?.Closed += Close;
            _goodsLabel?.Text = Localization.Localize("UI_Trade_Stock");
            _bagLabel?.Text = Localization.Localize("UI_Inventory");
            Active = this;
        }

        public override void _ExitTree()
        {
            if (Active == this) Active = null;
            if (_wallet != null) _wallet.GoldChanged -= OnGoldChanged;
            if (BagService is { } bag)
            {
                bag.DetachSlots();
                bag.ItemInteraction -= OnBagItemInteraction;
            }
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
            _traderService = provider.GetService<ITraderService>();
            _pricing = provider.GetService<TradePricing>();
            _wallet = provider.GetService<IWalletService>();
            _uiElements = provider.GetService<IUiElementsManager>();

            if (_wallet != null) _wallet.GoldChanged += OnGoldChanged;
            if (BagService is { } bag && _inventoryGrid != null)
            {
                bag.AttachSlots(_inventoryGrid);
                bag.ItemInteraction += OnBagItemInteraction;
            }

            OnGoldChanged(_wallet?.Gold ?? 0);
        }

        public void SetTrader(string traderId)
        {
            _traderId = traderId;
            _header?.SetTitle(Localization.Localize(traderId));
            RefreshStock();
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void RefreshStock()
        {
            if (_offersContainer == null || _traderService == null) return;
            _offersContainer.QueueFreeChildren();

            var trader = _traderService.GetTrader(_traderId);
            if (trader == null) return;

            // The shelf's zone header ("Goods") lives in the scene — the list itself opens with
            // the offers and only the buyback section announces itself.
            var stock = _traderService.GetStock(_traderId);
            foreach (var offer in stock.Where(entry => !entry.IsBuyback))
                _offersContainer.AddChild(BuildOfferRow(offer, trader.Fraction));

            var buyback = stock.Where(entry => entry.IsBuyback).ToList();
            if (buyback.Count == 0) return;
            AddSectionHeader(Localization.Localize("UI_Trade_Buyback"));
            foreach (var offer in buyback)
                _offersContainer.AddChild(BuildOfferRow(offer, trader.Fraction));
        }

        private void AddSectionHeader(string text)
        {
            // Same variation as the zone headers — one caption style across the window.
            _offersContainer?.AddChild(new Label { Text = text, ThemeTypeVariation = "SectionLabel" });
        }

        /// <summary>Row: icon · name (rarity-colored) · unit price · xRemaining · [qty] · Buy.
        /// The shared IconLabelRow carries the icon and name; price, counter, quantity spinner and
        /// the Buy button ride behind as trailing controls.</summary>
        private Control BuildOfferRow(TraderOffer offer, Fractions traderFaction)
        {
            var row = SharedUi.IconLabelRow.Initialize().Instantiate<SharedUi.IconLabelRow>();
            row.IconSize = 32;
            row.Set(offer.Item.Icon, offer.Item.DisplayName, Color.FromHtml(TextPalette.RarityColor(offer.Item.Rarity)));
            row.UseEllipsis();

            int unitPrice = offer.IsBuyback
                ? offer.BuybackUnitPrice
                : _pricing?.BuyPrice(offer.Item, traderFaction) ?? 0;
            row.SetTrailing(
                new Label { Text = Localization.Render("UI_Trade_Price", new System.Collections.Generic.Dictionary<string, object?> { ["Amount"] = unitPrice }) },
                new Label { Text = $"x{offer.Remaining}" });

            SpinBox? quantity = null;
            if (offer.Item.MaxStackSize > 1 && offer.Remaining > 1)
            {
                quantity = new SpinBox { MinValue = 1, MaxValue = offer.Remaining, Value = 1 };
                row.SetTrailing(quantity);
            }

            var buy = new Button { Text = Localization.Localize("UI_Trade_Buy"), Disabled = unitPrice <= 0 };
            buy.Pressed += () => _ = BuyAsync(offer.OfferId, quantity == null ? 1 : (int)quantity.Value);
            row.SetTrailing(buy);

            // The full item tooltip (rolled equips show their lines) — same pipeline as the bag slots.
            HoverTooltip.Attach(row, () => ShowOfferTooltip(offer.Item));
            return row;
        }

        private IPopup? ShowOfferTooltip(IItem item)
        {
            if (_uiElements?.ShowPopup(typeof(ItemTooltipPopup)) is not ItemTooltipPopup popup) return null;
            popup.ShowItem(item);
            return popup;
        }

        private async Task BuyAsync(string offerId, int amount)
        {
            if (_messageBus == null) return;
            await _messageBus.SendRequest<BuyItemRequest, int>(new(_traderId, offerId, amount));
            // 0 = refused (gold/bag/shelf) — the refreshed shelf and gold label tell the story.
            RefreshStock();
        }

        private async void OnBagItemInteraction(IItem item, MouseInteractions interaction)
        {
            if (_messageBus == null) return;
            int amount = interaction switch
            {
                MouseInteractions.RightClick => 1,
                MouseInteractions.CtrRmb => _inventory?.GetTotalItemAmount(item.Id) ?? 1,
                _ => 0,
            };
            if (amount < 1) return;

            await _messageBus.SendRequest<SellItemRequest, int>(new(_traderId, item.InstanceId, amount));
            RefreshStock(); // the buyback section just grew (or the sale was refused — same story)
        }

        private void OnGoldChanged(int gold) => _goldLabel?.Text = Localization.Render("UI_Trade_Gold", new System.Collections.Generic.Dictionary<string, object?> { ["Amount"] = gold });
    }
}
