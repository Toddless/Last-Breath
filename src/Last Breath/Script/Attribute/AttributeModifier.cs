namespace LastBreath.Script.Attribute
{
    using Core.Enums;
    using Core.Interfaces;

    public class AttributeModifier(Parameter param, float value, ModifierType type, object source, int priority = 0) : IModifier
    {
        public Parameter Parameter { get; } = param;

        public ModifierType ModifierType { get; } = type;

        public int Priority { get; } = priority;
        public float BaseValue { get; } = value;
        public float Value { get; set; } = value;

        public object Source { get; } = source;

        public float ModifyValue(float value)
        {
            return ModifierType switch
            {
                ModifierType.Multiplicative => value * Value,
                _ => value + Value,
            };
        }
    }
}
