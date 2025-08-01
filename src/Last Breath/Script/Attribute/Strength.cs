namespace LastBreath.Script.Attribute
{
    using System.Collections.Generic;
    using Contracts.Enums;

    public class Strength() : AttributeBase(GetEffects())
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
                (Parameter.Damage,
                ModifierType.Flat,
                15f);

            yield return new AttributeEffect
                (Parameter.Armor,
                ModifierType.Flat,
                100f);

            yield return new AttributeEffect
                (Parameter.Armor,
                ModifierType.Increase,
                0.02f);

            yield return new AttributeEffect
                (Parameter.MaxHealth,
                ModifierType.Flat,
                10f);
        }
    }
}
