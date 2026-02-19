namespace Core.Modifiers
{
    using Enums;
    using System.Collections.Generic;

    public abstract class ModifiersCreator
    {
        public static List<IModifierInstance> CreateModifierInstances(List<IModifier> stats, object source)
        {
            List<IModifierInstance> modifiers = [];
            stats.ForEach(mod => modifiers.Add(CreateModifierInstance(mod.EntityParameter, mod.ModifierType, mod.BaseValue, source)));
            return modifiers;
        }

        public static IModifierInstance CreateModifierInstance(EntityParameter entityParameter, ModifierType modifierType, float value, object source, int priority = 10)
            => new ModifierInstance(entityParameter, modifierType, value, source, priority);
    }
}
