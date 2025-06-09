namespace Playground.Script.Attribute
{
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public class Dexterity() : AttributeBase(GetEffects())
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
                (Parameter.CriticalChance,
                ModifierType.Additive,
                0.01f);

            yield return new AttributeEffect
                (Parameter.CriticalDamage,
                ModifierType.Additive,
                0.01f);

            yield return new AttributeEffect
                (Parameter.AdditionalHitChance,
                ModifierType.Additive,
                0.02f);

            yield return new AttributeEffect
                (Parameter.Evade,
                ModifierType.Additive,
                0.01f);
        }
    }
}
