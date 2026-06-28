namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class PercentHealthRegenerationEffect(
        float percentOfMaxHealth,
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.Regeneration)
        : Effect(id: "Effect_Percent_Health_Regeneration", duration, maxStacks, statusEffect)
    {
        public float PercentOfMaxHealth { get; } = percentOfMaxHealth;

        public override void TurnEnd()
        {
            if (Target != null)
            {
                float healAmount = Target.Parameters.MaxHealth * PercentOfMaxHealth;
                Target.Heal(new HealContext(Target, Target) { Amount = healAmount });
            }

            base.TurnEnd();
        }

        public override IEffect Copy() => new PercentHealthRegenerationEffect(PercentOfMaxHealth, Duration, MaxStacks, Status);
    }
}
