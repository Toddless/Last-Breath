namespace Core.MessageBus.Requests
{
    /// <summary>Buys Amount units of the offer all-or-nothing. Response: the gold actually paid;
    /// 0 = refused (shelf can't cover the amount, not enough gold, or no bag space) — nothing changed.</summary>
    public record BuyItemRequest(string TraderId, string OfferId, int Amount = 1) : IRequest<int>;

    /// <summary>Sells Amount units of a bag item (by instance id; a stack sells up to what is held).
    /// The goods land on the trader's buyback shelf at the earned price. Response: the gold gained;
    /// 0 = refused (unknown item or the trader won't price it) — nothing changed.</summary>
    public record SellItemRequest(string TraderId, string ItemInstanceId, int Amount = 1) : IRequest<int>;
}
