namespace LastBreath.Script.Attribute
{
    using System.Collections.Generic;
    using LastBreath.Script.Enums;

    public class Dexterity() : AttributeBase(GetEffects())
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
                (Parameter.CriticalChance,
                ModifierType.Flat,
                0.01f);

            yield return new AttributeEffect
                (Parameter.CriticalDamage,
                ModifierType.Flat,
                0.01f);

            yield return new AttributeEffect
                (Parameter.AdditionalHitChance,
                ModifierType.Flat,
                0.02f);

            yield return new AttributeEffect
                (Parameter.Evade,
                ModifierType.Flat,
                0.01f);
        }
    }
}
