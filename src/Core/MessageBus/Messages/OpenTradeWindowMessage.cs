namespace Core.MessageBus.Messages
{
    /// <summary>Opens the trade window for one trader. Published by the dialogue StartTrade action
    /// and the debug console; the handler is Main-side (trade is a world-project feature).</summary>
    public record OpenTradeWindowMessage(string TraderId) : IMessage;
}
