namespace Core.Narrative.Conditions
{
    using Data;
    using Entity;
    using Entity.Components;
    using Enums;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>A hard attribute gate, no roll: muscles don't gamble, words do (speech checks
    /// against Influence are the rolled kind).</summary>
    public class AttributeCondition(IPlayerAccessor playerAccessor, AttributeType attribute, int atLeast) : INarrativeCondition
    {
        public bool IsPreviewSafe => true;

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
        private const string TypeName = "Attribute";
        private const string AttributeKey = "attribute";
        private const string AtLeastKey = "atLeast";
        private const int DefaultAtLeast = 0;

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Choice(AttributeKey, required: true,
                nameof(AttributeType.Strength), nameof(AttributeType.Dexterity), nameof(AttributeType.Intelligence)),
            NarrativeParameterSchema.Integer(AtLeastKey, DefaultAtLeast));

        public string Type => TypeName;

        /// <summary>Only the three attributes an entity carries are offered: the condition resolves any
        /// other member of the enum to nothing and is then never met.</summary>
        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            new AttributeCondition(playerAccessor,
                EnumParser.ParseEnum<AttributeType>(json.Value<string>(AttributeKey) ?? string.Empty),
                json.Value<int?>(AtLeastKey) ?? DefaultAtLeast);
    }
}
