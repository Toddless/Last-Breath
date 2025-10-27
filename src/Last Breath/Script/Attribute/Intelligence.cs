namespace LastBreath.Script.Attribute
{
    using System.Collections.Generic;
    using Core.Enums;

    public class Intelligence() : AttributeBase(GetEffects())
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
                (Parameter.EnergyBarrier,
                ModifierType.Flat,
                10);

            yield return new AttributeEffect
                (Parameter.SpellDamage,
                ModifierType.Flat,
                0.2f);

            yield return new AttributeEffect
                (Parameter.ResourceRecovery,
                ModifierType.Flat,
                0.1f);

            //yield return new AttributeEffect
            //    (Parameter.ResourceMax,
            //    ModifierType.Flat,
            //    0.1f);
        }
    }
}
