namespace Core.Modifiers
{
    using System.Collections.Generic;
    using Enums;

    public abstract class ModifiersCreator
    {
        public static List<IModifierInstance> CreateModifierInstances(List<IModifier> stats, string source)
        {
            List<IModifierInstance> modifiers = [];
            stats.ForEach(mod => modifiers.Add(CreateModifierInstance(mod.EntityParameter, mod.ModifierValueType, mod.BaseValue, source)));
            return modifiers;
        }

        // NOTE: the last argument is stored as the instance Weight. On-item lines never re-enter a weighted
        // roll, so it is irrelevant there. For reroll-pool fodder use CopyForPool, which keeps the real weight.
        public static IModifierInstance CreateModifierInstance(EntityParameter entityParameter, ModifierValueType modifierValueType, float value, string source, int priority = 10)
            => new SimpleModifier(entityParameter, modifierValueType, value, source, priority);

    }
}
