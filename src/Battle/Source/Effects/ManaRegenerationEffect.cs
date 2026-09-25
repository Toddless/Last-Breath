namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;

    public class ManaRegenerationEffect(
        EffectValue percentRegeneration,
        int duration,
        int maxStacks,
        string id = "Effect_Mana_Regeneration",
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, maxStacks, statusEffect)
    {
        public float PercentRegeneration => Effective(percentRegeneration);

        public override void TurnEnd()
        {
            if (Target == null) return;
            float regenAmount = Target.Parameters.MaxMana * PercentRegeneration;
            Target.RestoreMana(new ManaRecoveryContext(Target, Target) { Amount = regenAmount });
            base.TurnEnd();
        }

        public override IEffect Copy() => new ManaRegenerationEffect(percentRegeneration, Duration, MaxStacks, Id, Status);
    }
}
