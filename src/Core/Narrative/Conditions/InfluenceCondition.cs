namespace Core.Narrative.Conditions
{
    using Influence;
    using Newtonsoft.Json.Linq;
    using Tooling.Schema.Model;

    public class InfluenceCondition(IInfluenceMastery mastery, int atLeast) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => mastery.CurrentLevel >= atLeast;
    }

    public class InfluenceConditionFactory(IInfluenceMastery mastery) : INarrativeConditionFactory
    {
        private const string TypeName = "Influence";
        private const string AtLeastKey = "atLeast";
        private const int DefaultAtLeast = 0;

        private static readonly RecordSchema s_parameters = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Integer(AtLeastKey, DefaultAtLeast));

        public string Type => TypeName;

        public RecordSchema Parameters => s_parameters;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new InfluenceCondition(mastery, json.Value<int?>(AtLeastKey) ?? DefaultAtLeast);
    }
}
