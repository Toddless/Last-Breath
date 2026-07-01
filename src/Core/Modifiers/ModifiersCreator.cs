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

        public static IModifierInstance CreateModifierInstance(EntityParameter entityParameter, ModifierValueType modifierValueType, float value, string source, int priority = 10)
            => new SimpleModifier(entityParameter, modifierValueType, value, source, priority);
    }
}
