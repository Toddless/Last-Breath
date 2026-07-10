namespace Core.Narrative.Conditions
{
    using Influence;
    using Newtonsoft.Json.Linq;

    public class InfluenceCondition(IInfluenceMastery mastery, int atLeast) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => mastery.CurrentLevel >= atLeast;
    }

    public class InfluenceConditionFactory(IInfluenceMastery mastery) : INarrativeConditionFactory
    {
        public string Type => "Influence";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new InfluenceCondition(mastery, json.Value<int?>("atLeast") ?? 0);
    }
}
