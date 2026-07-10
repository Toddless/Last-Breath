namespace Core.Narrative.Conditions
{
    using Facts;
    using Newtonsoft.Json.Linq;

    /// <summary>One entry covers flags and counters: a flag is a count of at least 1.</summary>
    public class FactCondition(IWorldFactsService facts, string key, int count) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => facts.GetCount(key) >= count;
    }

    public class FactConditionFactory(IWorldFactsService facts) : INarrativeConditionFactory
    {
        public string Type => "Fact";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            string key = json.Value<string>("key") ?? string.Empty;
            if (key.Length == 0)
            {
                Tracker.TrackError("Fact condition: key is missing");
                return null;
            }

            return new FactCondition(facts, key, json.Value<int?>("count") ?? 1);
        }
    }
}
