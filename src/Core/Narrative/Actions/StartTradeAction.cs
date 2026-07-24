namespace Core.Narrative.Actions
{
    using MessageBus;
    using MessageBus.Messages;
    using Newtonsoft.Json.Linq;

    /// <summary>Dialogue action "StartTrade": hands the conversation over to the trader's shop.
    /// Authors put it on a node that ends the dialogue — the trade window opens as it closes.</summary>
    public class StartTradeAction(IGameMessageBus messageBus, string traderId) : INarrativeAction
    {
        public void Execute(NarrativeContext context) => messageBus.PublishMessageAsync(new OpenTradeWindowMessage(traderId));
    }

    public class StartTradeActionFactory(IGameMessageBus messageBus) : INarrativeActionFactory
    {
        public string Type => "StartTrade";

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string traderId = json.Value<string>("traderId") ?? string.Empty;
            if (traderId.Length == 0)
            {
                Tracker.TrackError("StartTrade action: traderId is missing");
                return null;
            }

            return new StartTradeAction(messageBus, traderId);
        }
    }
}
