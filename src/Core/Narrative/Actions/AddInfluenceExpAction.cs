namespace Core.Narrative.Actions
{
    using Influence;
    using Newtonsoft.Json.Linq;

    public class AddInfluenceExpAction(IInfluenceMastery mastery, int amount) : INarrativeAction
    {
        public void Execute(NarrativeContext context) => mastery.AddExperience(amount);
    }

    public class AddInfluenceExpActionFactory(IInfluenceMastery mastery) : INarrativeActionFactory
    {
        public string Type => "AddInfluenceExp";

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            int amount = json.Value<int?>("amount") ?? 0;
            if (amount > 0) return new AddInfluenceExpAction(mastery, amount);

            Tracker.TrackError("AddInfluenceExp action: amount must be positive");
            return null;
        }
    }
}
