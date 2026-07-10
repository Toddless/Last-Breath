namespace Core.Narrative.Conditions
{
    using Components;
    using Data;
    using Entity;
    using Enums;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>A hard attribute gate, no roll: muscles don't gamble, words do (speech checks
    /// against Influence are the rolled kind).</summary>
    public class AttributeCondition(IPlayerAccessor playerAccessor, AttributeType attribute, int atLeast) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) =>
            playerAccessor.Player is { } player && Resolve(player) is { } value && value.Total >= atLeast;

        private IEntityAttribute? Resolve(IPlayer player) => attribute switch
        {
            AttributeType.Strength => player.Strength,
            AttributeType.Dexterity => player.Dexterity,
            AttributeType.Intelligence => player.Intelligence,
            _ => null,
        };
    }

    public class AttributeConditionFactory(IPlayerAccessor playerAccessor) : INarrativeConditionFactory
    {
        public string Type => "Attribute";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new AttributeCondition(playerAccessor,
                EnumParser.ParseEnum<AttributeType>(json.Value<string>("attribute") ?? string.Empty),
                json.Value<int?>("atLeast") ?? 0);
    }
}
