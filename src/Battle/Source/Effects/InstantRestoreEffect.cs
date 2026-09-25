namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;

    /// <summary>Gives a share of the bearer's pools back at once and leaves. An effect rather than a
    /// direct call so the restore travels the one road every laid number does — scaled by the cast's
    /// effectiveness in one place, not by hand at the point of the heal.</summary>
    public class InstantRestoreEffect(EffectValue healthShare, EffectValue manaShare)
        : Effect(id: "Effect_Instant_Restore", duration: 1, maxStacks: 1)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;

            float health = Effective(healthShare);
            if (health > 0)
                Target.Heal(new HealContext(Target, Target) { Amount = Target.Parameters.MaxHealth * health, Cause = RecoveryCause.Direct });

            float mana = Effective(manaShare);
            if (mana > 0)
                Target.RestoreMana(new ManaRecoveryContext(Target, Target) { Amount = Target.Parameters.MaxMana * mana });

            Remove();
        }

        public override IEffect Copy() => new InstantRestoreEffect(healthShare, manaShare);
    }
}
