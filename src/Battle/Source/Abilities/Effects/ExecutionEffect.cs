namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    public class ExecutionEffect(
        int duration,
        int maxStacks,
        float percentage,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Execution", duration, maxStacks, statusEffect)
    {
        public float Percentage { get; } = percentage;

        public override void AfterAttack(IAttackContext context)
        {
            float healthAsPercentLeft = context.Target.CurrentHealth / context.Target.Parameters.MaxHealth;

            if (healthAsPercentLeft <= Percentage) context.Target.Kill();
        }

        public override IEffect Clone() => new ExecutionEffect(Duration, MaxMaxStacks, Percentage, Status);

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not ExecutionEffect execution) return false;
            return execution.Percentage > Percentage;
        }
    }
}
