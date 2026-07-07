namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events.GameEvents;

    public class ExecutionEffect(
        int duration,
        int maxStacks,
        float percentage,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Execution", duration, maxStacks, statusEffect)
    {
        public float Percentage { get; } = percentage;

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

        public override IEffect Copy() => new ExecutionEffect(Duration, MaxStacks, Percentage, Status);

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not ExecutionEffect execution) return false;
            return Percentage > execution.Percentage;
        }
    }
}
