namespace Core.Narrative.Conditions
{
    using Influence;
    using Newtonsoft.Json.Linq;

    public class InfluenceCondition(IInfluenceMastery mastery, int atLeast) : INarrativeCondition
    {
        public bool IsPreviewSafe => true;

        public bool IsMet(NarrativeContext context) => mastery.CurrentLevel >= atLeast;
    }

    public class InfluenceConditionFactory(IInfluenceMastery mastery) : INarrativeConditionFactory
    {
        private const string TypeName = "Influence";
        private const string AtLeastKey = "atLeast";
        private const int DefaultAtLeast = 0;

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Integer(AtLeastKey, DefaultAtLeast));

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new InfluenceCondition(mastery, json.Value<int?>(AtLeastKey) ?? DefaultAtLeast);
    }
}
