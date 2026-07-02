namespace Core.Modifiers
{
    using System;
    using Enums;
    using Interfaces;
    using Interfaces.Entity;

    public class ConditionalModifier(float weight, ModifierValueType valueType, EntityParameter parameter, float value, ICondition condition, string source)
        : IConditionalModifier
    {
        public float Weight { get; set; } = weight;
        public ModifierValueType ModifierValueType { get; } = valueType;
        public EntityParameter EntityParameter { get; } = parameter;
        public float BaseValue { get; } = value;
        public float Value { get; set; } = value;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Source { get; } = source;
        public bool IsActive => condition.IsMet;
        public IModifierInstance Copy() => new ConditionalModifier(Weight, ModifierValueType, EntityParameter, BaseValue, condition, Source);
        public void ApplyTo(IFightable target) => throw new NotImplementedException();

        public void RemoveFrom(IFightable target) => throw new NotImplementedException();
    }
}
