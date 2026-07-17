namespace Core.Battle.DamageResolution
{
    using System;
    using Context;
    using Entity;

    /// <summary>The PoE-like barrier resource: eats damage before health.</summary>
    public class BarrierAbsorptionLayer : IDamageAbsorptionLayer
    {
        public float Absorb(IDamageContext context, IFightable target, float remaining)
        {
            float absorbed = Math.Min(target.CurrentBarrier, remaining);
            if (absorbed <= 0) return remaining;

            context.AbsorbedByBarrier = absorbed;
            target.CurrentBarrier -= absorbed;
            return remaining - absorbed;
        }
    }
}
