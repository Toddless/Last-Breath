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
    /// The floor guards health itself, so it reads the WHOLE blow: damage that walks past shields and
    /// barriers by its type still stops here.
    /// </summary>
    public class StageGuardLayer : IDamageAbsorptionLayer
    {
        public AbsorptionSplit Absorb(IDamageContext context, IFightable target, AbsorptionSplit damage)
        {
            var guard = target.Effects.GetBy(e => e is IStageGuardEffect).OfType<IStageGuardEffect>().FirstOrDefault();
            if (guard == null) return damage;

            float allowed = Math.Max(0, target.CurrentHealth - guard.FloorHealth);
            if (damage.Total <= allowed) return damage;

            context.PreventedByStageGuard = damage.Total - allowed;
            return damage.ScaledTo(allowed);
        }
    }
}
