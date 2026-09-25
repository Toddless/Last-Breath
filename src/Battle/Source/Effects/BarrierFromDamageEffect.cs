namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;

    /// <summary>Gives back a share of a blow as barrier at once and leaves. An effect rather than a
    /// direct write so the restore travels the one road every laid number does — scaled by the cast's
    /// effectiveness in one place instead of by hand where the barrier is added.</summary>
    public class BarrierFromDamageEffect(EffectValue shareOfDamage, float damage)
        : Effect(id: "Effect_Barrier_From_Damage", duration: 1, maxStacks: 1)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;

            float restored = damage * Effective(shareOfDamage);
            if (restored > 0) Target.CurrentBarrier += restored;

            Remove();
        }

        public override IEffect Copy() => new BarrierFromDamageEffect(shareOfDamage, damage);
    }
}
