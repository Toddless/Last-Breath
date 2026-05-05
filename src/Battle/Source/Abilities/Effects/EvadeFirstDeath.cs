namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class EvadeFirstDeath(
        int duration,
        int maxStacks,
        float percentHealthToRecover,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Evade_First_Death", duration, maxStacks, statusEffect)
    {
        public float PercentHealthToRecover { get; } = percentHealthToRecover;

        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            AppliedTo = context.Target;
            AppliedTo.CurrentHealthChanged += OnCurrentHealthChanges;
        }

        private void OnCurrentHealthChanges(float value)
        {
            if (value > 0) return;
            if (AppliedTo == null) return;
            float toRecover = AppliedTo.Parameters.MaxHealth * PercentHealthToRecover;
            AppliedTo.Heal(toRecover);
            Remove();
        }

        public override void Remove()
        {
            base.Remove();
            AppliedTo?.CurrentHealthChanged -= OnCurrentHealthChanges;
            AppliedTo = null;
        }

        public override IEffect Clone() => new EvadeFirstDeath(Duration, MaxMaxStacks, PercentHealthToRecover);
    }
}
