namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class ExecutionEffect(
        string id,
        int duration,
        int stacks,
        float percentage,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks, statusEffect)
    {
        public float Percentage { get; } = percentage;

        public override void AfterAttack(IEntity source, IAttackContext context)
        {
            float healthAsPercentLeft = context.Target.CurrentHealth / context.Target.Parameters.MaxHealth;

            if (healthAsPercentLeft <= Percentage) context.Target.Kill();
        }

        public override IEffect Clone() => new ExecutionEffect(Id, Duration, MaxStacks, Percentage, Status);

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not ExecutionEffect execution) return false;
            return execution.Percentage > Percentage;
        }
    }
}
