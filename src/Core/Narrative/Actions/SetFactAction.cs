namespace Core.Narrative.Actions
{
    using Facts;
    using Newtonsoft.Json.Linq;

    /// <summary>Raises a free-form flag ("count" turns it into a counter increment).</summary>
    public class SetFactAction(IWorldFactsService facts, string key, int count) : INarrativeAction
    {
        public void Execute(NarrativeContext context)
        {
            if (count <= 1) facts.SetFact(key);
            else facts.Add(key, count);
        }
    }

    public class SetFactActionFactory(IWorldFactsService facts) : INarrativeActionFactory
    {
        private const string TypeName = "SetFact";
        private const string FactKey = "key";
        private const string CountKey = "count";
        private const int DefaultCount = 1;

        private static readonly NarrativeRecordSpec s_parameters = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Text(FactKey, required: true),
            NarrativeParameterSchema.Integer(CountKey, DefaultCount));

        public string Type => TypeName;

        /// <summary>The same free-form key the Fact condition reads back, declared the same way.</summary>
        public NarrativeRecordSpec Parameters => s_parameters;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string key = json.Value<string>(FactKey) ?? string.Empty;
            if (key.Length == 0)
            {
                Tracker.TrackError($"{TypeName} action: {FactKey} is missing");
                return null;
            }

            return new SetFactAction(facts, key, json.Value<int?>(CountKey) ?? DefaultCount);
        }
    }
}
