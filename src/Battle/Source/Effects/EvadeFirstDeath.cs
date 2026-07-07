namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;

    public class EvadeFirstDeath(
        int duration,
        int maxStacks,
        float percentHealthToRecover,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Evade_First_Death", duration, maxStacks, statusEffect)
    {
        public float PercentHealthToRecover { get; } = percentHealthToRecover;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            Target = context.Target;
            Target.CurrentHealthChanged += OnCurrentHealthChanges;
        }

        private void OnCurrentHealthChanges(float value)
        {
            if (value > 0) return;
            if (Target == null) return;
            float toRecover = Target.Parameters.MaxHealth * PercentHealthToRecover;
            Target.Heal(new HealContext(Target, Target) { Amount = toRecover });
            Remove();
        }

        public override void Remove()
        {
            base.Remove();
            Target?.CurrentHealthChanged -= OnCurrentHealthChanges;
            Target = null;
        }

        public override IEffect Copy() => new EvadeFirstDeath(Duration, MaxStacks, PercentHealthToRecover);
    }
}
