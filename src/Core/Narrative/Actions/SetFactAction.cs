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
        public string Type => "SetFact";

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string key = json.Value<string>("key") ?? string.Empty;
            if (key.Length == 0)
            {
                Tracker.TrackError("SetFact action: key is missing");
                return null;
            }

            return new SetFactAction(facts, key, json.Value<int?>("count") ?? 1);
        }
    }
}
