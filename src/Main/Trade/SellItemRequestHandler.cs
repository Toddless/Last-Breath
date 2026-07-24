namespace LastBreath.Trade
{
    using System.Threading.Tasks;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Trade;

    /// <summary>
    /// The one gate of a sale: the trader prices the instance (0 = refuses — unpriced items never
    /// sell for a guessed sum), the item leaves the bag, the gold lands. Sells ONE unit of a stack.
    /// </summary>
    public class SellItemRequestHandler(
        ITraderService traderService,
        TradePricing pricing,
        IWalletService wallet,
        IInventory inventory)
        : IRequestHandler<SellItemRequest, int>
    {
        public Task<int> HandleRequest(SellItemRequest request)
        {
            var trader = traderService.GetTrader(request.TraderId);
            var item = inventory.GetItem<IItem>(request.ItemInstanceId);
            if (trader == null || item == null) return Task.FromResult(0);

            int price = pricing.SellPrice(item, trader.Fraction);
            if (price <= 0) return Task.FromResult(0);

            // A stackable good sells one unit of its stack; a rolled instance (equip) leaves whole.
            if (item.MaxStackSize > 1) inventory.RemoveItemById(item.Id, 1);
            else inventory.RemoveItemByInstanceId(item.InstanceId);
            wallet.Add(price);
            return Task.FromResult(price);
        }
    }
}
