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

        [Export] private Label? _title;
        [Export] private Label? _goldLabel;
        [Export] private VBoxContainer? _offersContainer;
        [Export] private GridContainer? _inventoryGrid;
        [Export] private Button? _closeButton;

        private IGameMessageBus? _messageBus;
        private IInventory? _inventory;
        private ITraderService? _traderService;
        private TradePricing? _pricing;
        private IWalletService? _wallet;
        private IUiElementsManager? _uiElements;
        private string _traderId = string.Empty;

        private Bag? BagService => _inventory as Bag;

        /// <summary>The open shop, if any: the item tooltip asks it for a sell quote and the player's
        /// movement poll treats it like an open conversation. Fresh-instance policy keeps it single.</summary>
        public static TradeWindow? Active { get; private set; }

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            _closeButton?.Pressed += Close;
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
            _title?.Text = Localization.Localize(traderId);
            RefreshStock();
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void RefreshStock()
        {
            if (_offersContainer == null || _traderService == null) return;
            foreach (var child in _offersContainer.GetChildren()) child.QueueFree();

            var trader = _traderService.GetTrader(_traderId);
            if (trader == null) return;

            var stock = _traderService.GetStock(_traderId);
            AddSectionHeader(Localization.Localize("UI_Trade_Stock"));
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
            var header = new Label { Text = text };
            header.AddThemeColorOverride("font_color", new Color(0.72f, 0.62f, 0.4f));
            _offersContainer?.AddChild(header);
        }

        /// <summary>Row: icon · name (rarity-colored) · unit price · xRemaining · [qty] · Buy.
        /// Built in code like the crafting requirement lines — the scene owns only the containers.</summary>
        private Control BuildOfferRow(TraderOffer offer, Fractions traderFaction)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var icon = new TextureRect
            {
                Texture = offer.Item.Icon,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            row.AddChild(icon);

            var name = new Label
            {
                Text = offer.Item.DisplayName,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            name.AddThemeColorOverride("font_color", Color.FromHtml(TextPalette.RarityColor(offer.Item.Rarity)));
            row.AddChild(name);

            int unitPrice = offer.IsBuyback
                ? offer.BuybackUnitPrice
                : _pricing?.BuyPrice(offer.Item, traderFaction) ?? 0;
            row.AddChild(new Label { Text = Localization.Render("UI_Trade_Price", new System.Collections.Generic.Dictionary<string, object?> { ["Amount"] = unitPrice }) });
            row.AddChild(new Label { Text = $"x{offer.Remaining}" });

            SpinBox? quantity = null;
            if (offer.Item.MaxStackSize > 1 && offer.Remaining > 1)
            {
                quantity = new SpinBox { MinValue = 1, MaxValue = offer.Remaining, Value = 1 };
                row.AddChild(quantity);
            }

            var buy = new Button { Text = Localization.Localize("UI_Trade_Buy"), Disabled = unitPrice <= 0 };
            buy.Pressed += () => _ = BuyAsync(offer.OfferId, quantity == null ? 1 : (int)quantity.Value);
            row.AddChild(buy);

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
