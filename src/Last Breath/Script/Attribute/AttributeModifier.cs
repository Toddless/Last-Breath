namespace Playground.Script.Attribute
{
    using Playground.Components.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class AttributeModifier(Parameter param, float value, ModifierType type, IAttribute source, int priority = 0) : IModifier
    {
        public Parameter Parameter { get; } = param;

        public ModifierType Type { get; } = type;

        public int Priority { get; } = priority;

        public float Value { get; } = value;

        public IAttribute SourceAttribute { get; } = source;

        public float ModifyValue(float value)
        {
            return Type switch
            {
                ModifierType.Multiplicative => value * Value,
                _ => value + Value,
            };
        }
    }
}
