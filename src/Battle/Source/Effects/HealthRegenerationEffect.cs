namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;

    public class HealthRegenerationEffect(
        EffectValue percentRegeneration,
        int duration,
        int maxStacks,
        string id = "Effect_Percent_Health_Regeneration",
        StatusEffects statusEffect = StatusEffects.Regeneration)
        : Effect(id, duration, maxStacks, statusEffect)
    {
        public float PercentRegeneration => Effective(percentRegeneration);

        public override void TurnEnd()
        {
            if (Target == null) return;
            float healAmount = Target.Parameters.MaxHealth * PercentRegeneration;
            Target.Heal(new HealContext(Target, Target) { Amount = healAmount });
            base.TurnEnd();
        }

        public override IEffect Copy() => new HealthRegenerationEffect(percentRegeneration, Duration, MaxStacks, Id, Status);
    }
}
