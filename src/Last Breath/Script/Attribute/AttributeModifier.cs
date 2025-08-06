namespace LastBreath.Script.Attribute
{
    using Core.Enums;
    using Core.Modifiers;

    public class AttributeModifier(Parameter param, float value, ModifierType type, object source, int priority = 0) : IModifier
    {
        public Parameter Parameter { get; } = param;

        public ModifierType Type { get; } = type;

        public int Priority { get; } = priority;

        public float Value { get; set; } = value;

        public object Source { get; } = source;

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
