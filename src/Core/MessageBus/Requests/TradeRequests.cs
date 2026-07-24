namespace Core.MessageBus.Requests
{
    /// <summary>Buys one unit of the offer. Response: the gold actually paid; 0 = refused
    /// (gone from the shelf, not enough gold, or no bag space) — nothing changed.</summary>
    public record BuyItemRequest(string TraderId, string OfferId) : IRequest<int>;

    /// <summary>Sells one bag item (by instance id) to the trader. Response: the gold gained;
    /// 0 = refused (unknown item or the trader won't price it) — nothing changed.</summary>
    public record SellItemRequest(string TraderId, string ItemInstanceId) : IRequest<int>;
}
