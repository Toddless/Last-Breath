namespace Core.Narrative.Actions
{
    using Data.GameData;
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
        private const string TypeName = "StartTrade";
        private const string TraderIdKey = "traderId";

        private static readonly NarrativeRecordSpec s_parameters = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Text(TraderIdKey, required: true, DataCatalog.Traders));

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => s_parameters;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string traderId = json.Value<string>(TraderIdKey) ?? string.Empty;
            if (traderId.Length == 0)
            {
                Tracker.TrackError($"{TypeName} action: {TraderIdKey} is missing");
                return null;
            }

            return new StartTradeAction(messageBus, traderId);
        }
    }
}
