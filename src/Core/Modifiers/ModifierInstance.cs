namespace Core.Modifiers
{
    using Core.Enums;
    using Core.Interfaces;

    public class ModifierInstance(Parameter parameter, ModifierType type, float value, object source, int priority = 0) : IModifierInstance
    {
        public Parameter Parameter { get; } = parameter;
        public ModifierType ModifierType { get; } = type;
        public int Priority { get; } = priority;
        public float Value { get; set; } = value;
        public float BaseValue { get; } = value;
        public object Source { get; } = source;
        public float Weight { get; }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is not IModifier other) return false;
            return Parameter == other.Parameter && ModifierType == other.ModifierType;
        }

        public override int GetHashCode() => System.HashCode.Combine(Parameter, ModifierType);
    }
}
