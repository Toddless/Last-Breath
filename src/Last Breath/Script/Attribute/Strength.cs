namespace Playground.Script.Attribute
{
    using System.Collections.Generic;
    using Playground.Components;
    using Playground.Script.Enums;

    public class Strength(ModifierManager manager) : AttributeBase(GetEffects(),manager)
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
              (Parameter.StrikeDamage,
              ModifierType.Additive,
              15f,
              priority: ModifierPriorities.BaseParameters
              );

            yield return new AttributeEffect
              (Parameter.Armor,
              ModifierType.Additive,
              100f,
              priority: ModifierPriorities.BaseParameters
              );

            yield return new AttributeEffect
              (Parameter.MaxHealth,
              ModifierType.Additive,
              10f,
              priority: ModifierPriorities.BaseParameters
              );
        }
    }
}
