namespace Utilities
{
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using System.Collections.Generic;

    public static class Calculations
    {
        public static float CalculateFloatValue(IReadOnlyList<IModifierInstance> modifiers) =>
            Math.Max(0, CalculateModifiers(modifiers));

        private static float CalculateModifiers(IEnumerable<IModifierInstance> modifiers, float value = 0)
        {
            float factor = 1;
            foreach (var group in modifiers.GroupBy(m => m.ModifierType).OrderBy(g => g.Key))
            {
                switch (group.Key)
                {
                    case ModifierType.Flat:
                        value += group.Sum(x => x.Value);
                        break;
                    case ModifierType.Increase:
                        value *= factor += group.Sum(x => x.Value);
                        break;
                    case ModifierType.Multiplicative:
                        value = ModifyValue(value, group);
                        break;
                }
            }

            return value;
        }

        private static float ModifyValue(float value, IGrouping<ModifierType, IModifierInstance> modifiers)
        {
            foreach (var modifier in modifiers.OrderByDescending(x => x.Priority))
                value *= modifier.Value;

            return value;
        }
    }
}
