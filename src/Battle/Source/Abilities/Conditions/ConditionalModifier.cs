namespace Battle.Source.Abilities.Conditions
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class ConditionalModifier(string id, ICondition condition, float value, ModifierType type, AbilityParameter parameter) : IConditionalModifier
    {
        public string Id { get; } = id;
        public AbilityParameter Parameter { get; } = parameter;

        public (float Value, ModifierType Type)? GetValue(EffectApplyingContext context)
        {
            if (IsApplicable(context))
                return (value, type);
            return null;
        }

        private bool IsApplicable(EffectApplyingContext context) => condition.IsMet(context);
    }
}
