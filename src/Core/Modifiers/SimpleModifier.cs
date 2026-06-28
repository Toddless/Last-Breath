namespace Core.Modifiers
{
    using System;
    using Enums;
    using Interfaces.Entity;

    public class SimpleModifier(EntityParameter entityParameter, ModifierValueType valueType, float value, object source, float weight = 1) : IModifierInstance
    {
        public EntityParameter EntityParameter { get; } = entityParameter;
        public ModifierValueType ModifierValueType { get; } = valueType;
        public float Value { get; set; } = value;
        public float BaseValue { get; } = value;
        public object Source { get; } = source;
        public float Weight { get; set; } = weight;
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public IModifierInstance Copy() => new SimpleModifier(EntityParameter, ModifierValueType, Value, Source, Weight);

        public void ApplyTo(IEntity target) => target.ParameterModifiers.AddModifier(this);

        public void RemoveFrom(IEntity target) => target.ParameterModifiers.RemoveModifier(this);

        public override bool Equals(object? obj)
        {
            if (obj is not IModifier other) return false;
            return EntityParameter == other.EntityParameter && ModifierValueType == other.ModifierValueType;
        }

        public override int GetHashCode() => HashCode.Combine(EntityParameter, ModifierValueType);
    }
}
