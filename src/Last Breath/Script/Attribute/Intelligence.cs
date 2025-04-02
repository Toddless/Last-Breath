namespace Playground.Script.Attribute
{
    using System.Collections.Generic;
    using Playground.Components;
    using Playground.Script.Enums;

    public class Intelligence(ModifierManager manager) : AttributeBase(GetEffects(), manager)
    {
        private static IEnumerable<AttributeEffect> GetEffects()
        {
            yield return new AttributeEffect
               (Parameter.EnergyBarrier,
               ModifierType.Additive,
               0.01f,
               priority: ModifierPriorities.BaseParameters
               );

            yield return new AttributeEffect
              (Parameter.SpellDamage,
              ModifierType.Additive,
              0.2f,
              priority: ModifierPriorities.BaseParameters
              );
        }
    }
}
