namespace Core.Modifiers
{
    using Core.Enums;

    public abstract class ModifierBase(Parameter parameter, ModifierType type, float value, object source, int priority = 0) : IModifier
    {
        public Parameter Parameter { get; } = parameter;

        public ModifierType Type { get; } = type;

        public int Priority { get; } = priority;

        public float Value { get; set; } = value;

        public object Source { get; } = source;
    }
}
