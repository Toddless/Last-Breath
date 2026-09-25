namespace LastBreath.Trade
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Inventory;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Trade;

    /// <summary>
    /// The one gate of a purchase (the UI button is a mirror): price first, wallet check-then-spend,
    /// item into the bag LAST — a full bag refunds the spend, so the request is all-or-nothing.
    /// A buyback offer sells at exactly the price the trader paid — undoing a sale never costs extra.
    /// </summary>
    public class BuyItemRequestHandler(
        ITraderService traderService,
        TradePricing pricing,
        IWalletService wallet,
        IInventory inventory)
        : IRequestHandler<BuyItemRequest, int>
    {
        public Task<int> HandleRequest(BuyItemRequest request)
        {
            var trader = traderService.GetTrader(request.TraderId);
            var offer = traderService.GetStock(request.TraderId)
                .FirstOrDefault(entry => entry.OfferId == request.OfferId);
            if (trader == null || offer == null || request.Amount < 1 || request.Amount > offer.Remaining) return Task.FromResult(0);

            int unitPrice = offer.IsBuyback ? offer.BuybackUnitPrice : pricing.BuyPrice(offer.Item, trader.Fraction);
            int total = unitPrice * request.Amount;
            if (unitPrice <= 0 || !wallet.TrySpend(total)) return Task.FromResult(0);

            var item = traderService.TakeMany(request.TraderId, request.OfferId, request.Amount);
            if (item == null || !inventory.TryAddItem(item, request.Amount))
            {
                wallet.Add(total); // shelf raced empty or the bag is full: the purchase never happened
                return Task.FromResult(0);
            }

            return Task.FromResult(total);
        }
    }
}
