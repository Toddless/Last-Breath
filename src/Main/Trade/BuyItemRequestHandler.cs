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
            if (trader == null || offer == null) return Task.FromResult(0);

            int price = pricing.BuyPrice(offer.Item, trader.Fraction);
            if (price <= 0 || !wallet.TrySpend(price)) return Task.FromResult(0);

            var item = traderService.TakeOne(request.TraderId, request.OfferId);
            if (item == null || !inventory.TryAddItem(item))
            {
                wallet.Add(price); // shelf raced empty or the bag is full: the purchase never happened
                return Task.FromResult(0);
            }

            return Task.FromResult(price);
        }
    }
}
