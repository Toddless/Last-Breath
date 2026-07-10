namespace Core.Narrative.Conditions
{
    using Data;
    using Entity;
    using Enums;
    using Newtonsoft.Json.Linq;

    public class FactionStandingCondition(IFactionRelationService relations, Fractions faction, RelationLevel atLeast) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => relations.GetPlayerRelation(faction) >= atLeast;
    }

    public class FactionStandingConditionFactory(IFactionRelationService relations) : INarrativeConditionFactory
    {
        public string Type => "FactionStanding";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new FactionStandingCondition(relations,
                EnumParser.ParseEnum<Fractions>(json.Value<string>("faction") ?? string.Empty),
                EnumParser.ParseEnum<RelationLevel>(json.Value<string>("atLeast") ?? string.Empty));
    }
}
