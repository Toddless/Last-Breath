namespace Playground.Script.Attribute
{
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public class Strength() : AttributeBase(GetEffects())
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
              (Parameter.StrikeDamage,
              ModifierType.Additive,
              15f
              );

            yield return new AttributeEffect
              (Parameter.Armor,
              ModifierType.Additive,
              100f
              );

            yield return new AttributeEffect
                (Parameter.Armor,
                ModifierType.MultiplicativeSum,
                0.02f
                );

            yield return new AttributeEffect
              (Parameter.MaxHealth,
              ModifierType.Additive,
              10f
              );
        }
    }
}
