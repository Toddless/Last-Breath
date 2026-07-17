namespace Core.Battle.DamageResolution
{
    using System.Linq;
    using Abilities;
    using Context;
    using Entity;

    /// <summary>The shield layer: a separate defense on top of the barrier.</summary>
    public class ShieldAbsorptionLayer : IDamageAbsorptionLayer
    {
        public float Absorb(IDamageContext context, IFightable target, float remaining)
        {
            var shield = target.Effects.GetBy(e => e is IShieldEffect).OfType<IShieldEffect>().FirstOrDefault();
            if (shield == null) return remaining;

            float leftover = shield.Absorb(remaining);
            context.AbsorbedByShield = remaining - leftover;
            return leftover;
        }
    }
}
