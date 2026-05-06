namespace Battle.Source.Abilities.Effects
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
            if (AppliedTo != null)
            {
                float healAmount = AppliedTo.Parameters.MaxHealth * PercentOfMaxHealth;
                AppliedTo.Heal(healAmount);
            }
            base.TurnEnd();
        }

        public override IEffect Clone() => new PercentHealthRegenerationEffect(PercentOfMaxHealth, Duration, MaxStacks, Status);
    }
}
