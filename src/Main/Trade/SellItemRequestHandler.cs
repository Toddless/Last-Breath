namespace LastBreath.Trade
{
    using System;
    using System.Threading.Tasks;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Trade;

    /// <summary>
    /// The one gate of a sale: the trader prices the instance (0 = refuses — unpriced items never
    /// sell for a guessed sum), the goods leave the bag, the gold lands, and the sold goods wait on
    /// the trader's buyback shelf at the earned price. A stack sells up to what is actually held.
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
            if (trader == null || item == null || request.Amount < 1) return Task.FromResult(0);

            int unitPrice = pricing.SellPrice(item, trader.Fraction);
            if (unitPrice <= 0) return Task.FromResult(0);

            if (item.MaxStackSize > 1)
            {
                int sold = Math.Min(request.Amount, inventory.GetTotalItemAmount(item.Id));
                if (sold < 1) return Task.FromResult(0);
                // The buyback snapshot is a copy: the bag's instance count keeps shrinking normally.
                traderService.AddBuyback(request.TraderId, item.Copy<IItem>(), sold, unitPrice);
                inventory.RemoveItemById(item.Id, sold);
                wallet.Add(unitPrice * sold);
                return Task.FromResult(unitPrice * sold);
            }

            // A rolled instance (equip) leaves whole and waits on the shelf as itself.
            inventory.RemoveItemByInstanceId(item.InstanceId);
            traderService.AddBuyback(request.TraderId, item, 1, unitPrice);
            wallet.Add(unitPrice);
            return Task.FromResult(unitPrice);
        }
    }
}
