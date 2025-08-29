namespace Core.Modifiers
{
    using System;
    using Core.Enums;

    public abstract class ModifierBase(Parameter parameter, ModifierType type, float value, object source, int priority = 0) : IModifier
    {
        public Parameter Parameter { get; } = parameter;

        public ModifierType Type { get; } = type;

        public int Priority { get; } = priority;

        public float Value { get; set; } = value;

        public object Source { get; } = source;

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is not IModifier other) return false;
            return Parameter == other.Parameter && Type == other.Type && Value == other.Value && Source == other.Source;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Parameter.GetHashCode();
                hash = hash * 23 + Type.GetHashCode();
                hash = hash * 23 + BitConverter.SingleToInt32Bits(Value);
                return hash;
            }
        }
    }
}
