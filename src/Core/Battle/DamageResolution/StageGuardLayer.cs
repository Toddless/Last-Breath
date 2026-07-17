namespace Core.Battle.DamageResolution
{
    using System;
    using System.Linq;
    using Abilities;
    using Context;
    using Entity;

    /// <summary>
    /// The anti-oneshot floor of staged bosses: while an <see cref="IStageGuardEffect"/> holds,
    /// health cannot drop below its floor — the overkill is prevented (and reported in the context),
    /// the boss lands exactly on the transition threshold and the transition machinery takes over.
    /// </summary>
    public class StageGuardLayer : IDamageAbsorptionLayer
    {
        public float Absorb(IDamageContext context, IFightable target, float remaining)
        {
            var guard = target.Effects.GetBy(e => e is IStageGuardEffect).OfType<IStageGuardEffect>().FirstOrDefault();
            if (guard == null) return remaining;

            float allowed = Math.Max(0, target.CurrentHealth - guard.FloorHealth);
            if (remaining <= allowed) return remaining;

            context.PreventedByStageGuard = remaining - allowed;
            return allowed;
        }
    }
}
