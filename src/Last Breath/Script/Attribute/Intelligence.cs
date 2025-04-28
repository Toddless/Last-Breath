namespace Playground.Script.Attribute
{
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public class Intelligence() : AttributeBase(GetEffects())
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
               (Parameter.EnergyBarrier,
               ModifierType.Additive,
               0.01f
               );

            yield return new AttributeEffect
              (Parameter.SpellDamage,
              ModifierType.Additive,
              0.2f
              );
        }
    }
}
