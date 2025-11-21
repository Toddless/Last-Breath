namespace Core.Modifiers
{
    using Enums;
    using Interfaces;

    public class ModifierInstance(EntityParameter entityParameter, ModifierType type, float value, object source, int priority = 0) : IModifierInstance
    {
        public EntityParameter EntityParameter { get; } = entityParameter;
        public ModifierType ModifierType { get; } = type;
        public int Priority { get; } = priority;
        public float Value { get; set; } = value;
        public float BaseValue { get; } = value;
        public object Source { get; } = source;
        public float Weight { get; }

        public override bool Equals(object? obj)
        {
            if (obj is not IModifier other) return false;
            return EntityParameter == other.EntityParameter && ModifierType == other.ModifierType;
        }

        public override int GetHashCode() => System.HashCode.Combine(EntityParameter, ModifierType);
    }
}
