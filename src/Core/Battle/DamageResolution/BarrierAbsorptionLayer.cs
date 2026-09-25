namespace Core.Battle.DamageResolution
{
    using System;
    using Context;
    using Entity;

    /// <summary>Barrier resource: eats damage before health. Two different bypasses meet here and both
    /// hold: <see cref="IDamageContext.IgnoreBarrier"/> takes a whole hit past this one layer (the
    /// attacker's mark), while absorption-bypassing damage types are past every layer by their nature.</summary>
    public class BarrierAbsorptionLayer : IDamageAbsorptionLayer
    {
        public AbsorptionSplit Absorb(IDamageContext context, IFightable target, AbsorptionSplit damage)
        {
            if (context.IgnoreBarrier) return damage;
            float absorbed = Math.Min(target.CurrentBarrier, damage.Absorbable);
            if (absorbed <= 0) return damage;

            context.AbsorbedByBarrier = absorbed;
            target.CurrentBarrier -= absorbed;
            return damage with { Absorbable = damage.Absorbable - absorbed };
        }
    }
}
