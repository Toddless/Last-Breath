namespace Core.Narrative.Conditions
{
    using Data;
    using Entity;
    using Enums;
    using Newtonsoft.Json.Linq;

    public class FactionStandingCondition(IFactionRelationService relations, Fractions faction, RelationLevel atLeast) : INarrativeCondition
    {
        public bool IsPreviewSafe => true;

        public bool IsMet(NarrativeContext context) => relations.GetPlayerRelation(faction) >= atLeast;
    }

    public class FactionStandingConditionFactory(IFactionRelationService relations) : INarrativeConditionFactory
    {
        private const string TypeName = "FactionStanding";
        private const string FactionKey = "faction";
        private const string AtLeastKey = "atLeast";

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Enum<Fractions>(FactionKey),
            NarrativeParameterSchema.Enum<RelationLevel>(AtLeastKey));

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new FactionStandingCondition(relations,
                EnumParser.ParseEnum<Fractions>(json.Value<string>(FactionKey) ?? string.Empty),
                EnumParser.ParseEnum<RelationLevel>(json.Value<string>(AtLeastKey) ?? string.Empty));
    }
}
