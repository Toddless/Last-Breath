namespace Core.Narrative.Conditions
{
    using Data;
    using Enums;
    using Newtonsoft.Json.Linq;
    using Reputation;

    /// <summary>The interlocutor's EFFECTIVE relation: faction standing shifted by personal
    /// opinion. Outside a conversation (empty context) the condition is never met.</summary>
    public class NpcRelationCondition(IPersonalReputationService personal, RelationLevel atLeast) : INarrativeCondition
    {
        public bool IsPreviewSafe => true;

        public bool IsMet(NarrativeContext context) =>
            context is { NpcInstanceId: { } instanceId, NpcFaction: { } faction }
            && personal.GetEffectiveRelation(instanceId, faction) >= atLeast;
    }

    public class NpcRelationConditionFactory(IPersonalReputationService personal) : INarrativeConditionFactory
    {
        private const string TypeName = "NpcRelation";
        private const string AtLeastKey = "atLeast";

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Enum<RelationLevel>(AtLeastKey));

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new NpcRelationCondition(personal, EnumParser.ParseEnum<RelationLevel>(json.Value<string>(AtLeastKey) ?? string.Empty));
    }
}
