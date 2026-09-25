namespace Core.Narrative.Conditions
{
    using Facts;
    using Newtonsoft.Json.Linq;

    /// <summary>One entry covers flags and counters: a flag is a count of at least 1.</summary>
    public class FactCondition(IWorldFactsService facts, string key, int count) : INarrativeCondition
    {
        public bool IsPreviewSafe => true;

        public bool IsMet(NarrativeContext context) => facts.GetCount(key) >= count;
    }

    public class FactConditionFactory(IWorldFactsService facts) : INarrativeConditionFactory
    {
        private const string TypeName = "Fact";
        private const string FactKey = "key";
        private const string CountKey = "count";
        private const int DefaultCount = 1;

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.FactKey(FactKey, written: false),
            NarrativeParameterSchema.Integer(CountKey, DefaultCount));

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            string key = json.Value<string>(FactKey) ?? string.Empty;
            if (key.Length == 0)
            {
                Tracker.TrackError($"{TypeName} condition: {FactKey} is missing");
                return null;
            }

            return new FactCondition(facts, key, json.Value<int?>(CountKey) ?? DefaultCount);
        }
    }
}
