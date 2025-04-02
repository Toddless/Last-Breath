namespace Playground.Script.Attribute
{
    using System.Collections.Generic;
    using Playground.Components;
    using Playground.Script.Enums;

    public class Dexterity(ModifierManager manager) : AttributeBase(GetEffects(), manager)
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
               (Parameter.CriticalStrikeChance,
               ModifierType.Additive,
               0.01f,
               priority: ModifierPriorities.BaseParameters
               );

            yield return new AttributeEffect
              (Parameter.CriticalStrikeDamage,
              ModifierType.Additive,
              0.5f,
              priority: ModifierPriorities.BaseParameters
              );

            yield return new AttributeEffect
              (Parameter.AdditionalStrikeChance,
              ModifierType.Additive,
              0.02f,
              priority: ModifierPriorities.BaseParameters
              );

            yield return new AttributeEffect
              (Parameter.Dodge,
              ModifierType.Additive,
              0.01f,
              priority: ModifierPriorities.BaseParameters
              );
        }
    }
}
