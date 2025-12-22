namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class EvadeFirstDeath(
        string id,
        int duration,
        float percentToRecover,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks: 1, statusEffect)
    {
        public float PercentToRecover { get; } = percentToRecover;

        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            Owner = context.Target;
            Owner.CurrentHealthChanged += OnCurrentHealthChanges;
        }

        private void OnCurrentHealthChanges(float value)
        {
            if (value > 0) return;
            if (Owner == null) return;
            float toRecover = Owner.Parameters.MaxHealth * PercentToRecover;
            Owner.Heal(toRecover);
            Remove();
        }

        public override void Remove()
        {
            base.Remove();
            Owner?.CurrentHealthChanged -= OnCurrentHealthChanges;
            Owner = null;
        }

        public override IEffect Clone() => new EvadeFirstDeath(Id, Duration, PercentToRecover);
    }
}
