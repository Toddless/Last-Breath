namespace Core.Battle.DamageResolution
{
    using System.Linq;
    using Abilities;
    using Context;
    using Entity;

    /// <summary>The shield layer: a separate defense on top of the barrier. A blow that is nothing but
    /// absorption-bypassing damage never reaches the shield — it is not spent on what it cannot stop.</summary>
    public class ShieldAbsorptionLayer : IDamageAbsorptionLayer
    {
        public AbsorptionSplit Absorb(IDamageContext context, IFightable target, AbsorptionSplit damage)
        {
            if (damage.Absorbable <= 0) return damage;

            var shield = target.Effects.GetBy(e => e is IShieldEffect).OfType<IShieldEffect>().FirstOrDefault();
            if (shield == null) return damage;

            float leftover = shield.Absorb(damage.Absorbable);
            context.AbsorbedByShield = damage.Absorbable - leftover;
            return damage with { Absorbable = leftover };
        }
    }
}
