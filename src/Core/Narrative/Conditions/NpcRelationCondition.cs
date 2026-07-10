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
        public bool IsMet(NarrativeContext context) =>
            context is { NpcInstanceId: { } instanceId, NpcFaction: { } faction }
            && personal.GetEffectiveRelation(instanceId, faction) >= atLeast;
    }

    public class NpcRelationConditionFactory(IPersonalReputationService personal) : INarrativeConditionFactory
    {
        public string Type => "NpcRelation";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new NpcRelationCondition(personal, EnumParser.ParseEnum<RelationLevel>(json.Value<string>("atLeast") ?? string.Empty));
    }
}
