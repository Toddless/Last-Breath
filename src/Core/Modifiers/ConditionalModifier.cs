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
    /// ever being written onto him, and the flip has the same work to do either way.</para>
    /// <para>A null condition is a line with no gate: it always counts, and binding is then only about
    /// knowing the carrier — which is what a kin that measures its value off him needs.</para></summary>
    public class ConditionalModifier(float weight, ModifierValueType valueType, EntityParameter parameter, float value, ICondition? condition, string source)
        : IConditionalModifier
    {
        /// <summary>The fighter this line is currently bound to, for kin that read him for more than the gate.</summary>
        protected IFightable? Owner { get; private set; }

        /// <summary>The gate itself, for kin that have to hand it on to a copy.</summary>
        protected ICondition? Condition => condition;

        public float Weight { get; set; } = weight;
        public ModifierValueType ModifierValueType { get; } = valueType;
        public EntityParameter EntityParameter { get; } = parameter;
        public ModifierScope Scope { get; set; } = ModifierScope.Global;
        public float BaseValue { get; } = value;
        public float Value { get; set; } = value;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Source { get; } = source;
        public bool IsActive => condition?.IsMet ?? true;

        public virtual IModifierInstance Copy() =>
            new ConditionalModifier(Weight, ModifierValueType, EntityParameter, BaseValue, condition?.Copy(), Source);

        /// <summary>Points the predicate at the fighter whose state answers it. One fighter at a time:
        /// binding to another releases the previous one first, so a modifier that moves across a scene
        /// change cannot leave a predicate subscribed to the fighter it left behind.</summary>
        public virtual void Bind(IFightable target)
        {
            if (Owner != null) Unbind();

            Owner = target;
            if (condition is null) return;

            condition.StateChanged += OnConditionStateChanged;
            condition.Attach(target);
        }

        /// <summary>Lets the fighter go. Everything the predicate subscribed to is released, so a
        /// contribution that is rebuilt or taken back leaves nothing listening.</summary>
        public virtual void Unbind()
        {
            if (Owner == null) return;

            Owner = null;
            if (condition is null) return;

            condition.StateChanged -= OnConditionStateChanged;
            condition.Detach();
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

        private void OnConditionStateChanged(bool isMet) => Owner?.ParameterModifiers.RefreshParameter(EntityParameter);
    }
}
