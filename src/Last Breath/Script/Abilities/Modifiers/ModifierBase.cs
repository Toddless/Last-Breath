namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class ModifierBase(Parameter parameter, ModifierType type, float value, object source, int priority = 0) : IModifier
    {
        public Parameter Parameter { get; } = parameter;

        public ModifierType Type { get; } = type;

        public int Priority { get; } = priority;

        public float Value { get; set; } = value;

        public object Source { get; } = source;

        public float ModifyValue(float value)
        {
            return Type switch
            {
                ModifierType.Multiplicative => value * Value,
                _ => value + Value
            };
        }
    }
}
