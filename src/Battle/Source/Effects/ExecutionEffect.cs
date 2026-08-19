namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;

    public class ExecutionEffect(
        int duration,
        int maxStacks,
        EffectValue percentage,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Execution", duration, maxStacks, statusEffect)
    {
        public override bool IsHarmful => true;

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(Percentage)] = Percentage;
                return values;
            }
        }

        /// <summary>Share of maximum health at or below which the blow finishes the target.</summary>
        public float Percentage => Effective(percentage);

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;
            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            var target = evt.Context.Target;
            float healthAsPercentLeft = target.CurrentHealth / target.Parameters.MaxHealth;

            if (healthAsPercentLeft <= Percentage) target.Kill();
        }

        // The AUTHORED share, never the effective one: a copy scaled again would execute at a threshold
        // its original never had.
        public override IEffect Copy() => new ExecutionEffect(Duration, MaxStacks, percentage, Status);

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not ExecutionEffect execution) return false;
            return Percentage > execution.Percentage;
        }
    }
}
