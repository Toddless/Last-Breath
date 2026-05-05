namespace Core.Modifiers
{
    using Enums;
    using System.Collections.Generic;

    public abstract class ModifiersCreator
    {
        public static List<IModifierInstance> CreateModifierInstances(List<IModifier> stats, object source)
        {
            List<IModifierInstance> modifiers = [];
            stats.ForEach(mod => modifiers.Add(CreateModifierInstance(mod.EntityParameter, mod.ModifierValueType, mod.BaseValue, source)));
            return modifiers;
        }

        public static IModifierInstance CreateModifierInstance(EntityParameter entityParameter, ModifierValueType modifierValueType, float value, object source, int priority = 10)
            => new SimpleModifier(entityParameter, modifierValueType, value, source, priority);
    }
}
