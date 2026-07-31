namespace Core.Modifiers
{
    using System;
    using Entity;
    using Enums;
    using Interfaces;

    /// <summary>A parameter modifier that only counts while its condition holds.
    /// <see cref="Calculations"/> skips it when it is inactive, and every condition flip refreshes the
    /// parameter on the fighter it watches so the value is resolved again.
    /// <para>Watching a fighter and living in his modifier list are two separate steps. A line written onto
    /// the entity takes both (<see cref="ApplyTo"/>); a line handed out by a source the entity pulls from
    /// takes only the first (<see cref="Bind"/>) — it is counted when the fighter resolves the value without
    /// ever being written onto him, and the flip has the same work to do either way.</para></summary>
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

        /// <summary>Points the predicate at the fighter whose state answers it. One fighter at a time:
        /// binding to another releases the previous one first, so a modifier that moves across a scene
        /// change cannot leave a predicate subscribed to the fighter it left behind.</summary>
        public void Bind(IFightable target)
        {
            if (_owner != null) Unbind();

            _owner = target;
            condition.StateChanged += OnConditionStateChanged;
            condition.Attach(target);
        }

        /// <summary>Lets the fighter go. Everything the predicate subscribed to is released, so a
        /// contribution that is rebuilt or taken back leaves nothing listening.</summary>
        public void Unbind()
        {
            if (_owner == null) return;

            condition.StateChanged -= OnConditionStateChanged;
            condition.Detach();
            _owner = null;
        }

        public void ApplyTo(IFightable target)
        {
            Bind(target);
            target.ParameterModifiers.AddModifier(this);
        }

        public void RemoveFrom(IFightable target)
        {
            Unbind();
            target.ParameterModifiers.RemoveModifier(this);
        }

        private void OnConditionStateChanged(bool isMet) => _owner?.ParameterModifiers.RefreshParameter(EntityParameter);
    }
}
