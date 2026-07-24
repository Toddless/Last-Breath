namespace LastBreath.Trade
{
    using System.Threading.Tasks;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Views.UI;
    using UI;

    /// <summary>The one entry into trading: dialogue StartTrade and the debug console both land
    /// here. A null window = trading is not available in the current context — silent no-op.</summary>
    public class OpenTradeWindowMessageHandler(IUiElementsManager uiElementsManager) : IMessageHandler<OpenTradeWindowMessage>
    {
        public Task HandleMessageAsync(OpenTradeWindowMessage message)
        {
            if (uiElementsManager.OpenWindow(typeof(TradeWindow)) is TradeWindow window)
                window.SetTrader(message.TraderId);
            return Task.CompletedTask;
        }
    }
}
