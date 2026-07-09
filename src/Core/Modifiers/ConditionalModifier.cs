namespace Core.Modifiers
{
    using System;
    using Entity;
    using Enums;
    using Interfaces;

    /// <summary>A parameter modifier that only counts while its condition holds.
    /// Stays in the owner's modifier list permanently; <see cref="Calculations"/> skips it when inactive,
    /// and every condition flip refreshes the parameter so it recalculates.</summary>
    public class ConditionalModifier(float weight, ModifierValueType valueType, EntityParameter parameter, float value, ICondition condition, string source)
        : IConditionalModifier
    {
        private IFightable? _owner;

        public float Weight { get; set; } = weight;
        public ModifierValueType ModifierValueType { get; } = valueType;
        public EntityParameter EntityParameter { get; } = parameter;
        public ModifierScope Scope { get; set; } = ModifierScope.Global;
        public float BaseValue { get; } = value;
        public float Value { get; set; } = value;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Source { get; } = source;
        public bool IsActive => condition.IsMet;

        public IModifierInstance Copy() => new ConditionalModifier(Weight, ModifierValueType, EntityParameter, BaseValue, condition.Copy(), Source);

        public void ApplyTo(IFightable target)
        {
            _owner = target;
            condition.StateChanged += OnConditionStateChanged;
            condition.Attach(target);
            target.ParameterModifiers.AddModifier(this);
        }

        public void RemoveFrom(IFightable target)
        {
            condition.StateChanged -= OnConditionStateChanged;
            condition.Detach();
            target.ParameterModifiers.RemoveModifier(this);
            _owner = null;
        }

        private void OnConditionStateChanged(bool isMet) => _owner?.ParameterModifiers.RefreshParameter(EntityParameter);
    }
}
