namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;

    /// <summary>Dodges the bearer's first death and gives a share of maximum health back instead. The
    /// share is an <see cref="EffectValue"/>, so it is read through the effectiveness of the cast that
    /// laid it like every other figure of applied content — the shroud that lays it declares the key and
    /// the recovery-effectiveness record is sold on it.</summary>
    public class EvadeFirstDeath(
        int duration,
        int maxStacks,
        EffectValue percentHealthToRecover,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Evade_First_Death", duration, maxStacks, statusEffect)
    {
        public float PercentHealthToRecover => Effective(percentHealthToRecover);

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

        // The AUTHORED share goes to the copy, never the effective one: handing over the multiplied
        // figure would multiply it a second time on the copy's own stamp.
        public override IEffect Copy() => new EvadeFirstDeath(Duration, MaxStacks, percentHealthToRecover);
    }
}
