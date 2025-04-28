namespace Playground.Script.Attribute
{
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public class Dexterity() : AttributeBase(GetEffects())
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
               (Parameter.CriticalStrikeChance,
               ModifierType.Additive,
               0.01f
               );

            yield return new AttributeEffect
                (Parameter.CriticalStrikeChance,
                ModifierType.MultiplicativeSum,
                0.01f
                );

            yield return new AttributeEffect
              (Parameter.CriticalStrikeDamage,
              ModifierType.Additive,
              0.01f
              );

            yield return new AttributeEffect
              (Parameter.AdditionalStrikeChance,
              ModifierType.Additive,
              0.02f
              );

            yield return new AttributeEffect
              (Parameter.Dodge,
              ModifierType.Additive,
              0.01f
              );
        }

    }
}
