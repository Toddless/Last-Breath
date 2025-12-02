namespace Utilities
{
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using System.Collections.Generic;

    public static class Calculations
    {
        public static float CalculateFloatValue(IReadOnlyList<IModifier> modifiers, float baseValue = 0)
            => Math.Max(0, CalculateModifiers(modifiers, baseValue));

        private static float CalculateModifiers(IEnumerable<IModifier> modifiers, float value = 0)
        {
            float sumAdditions = 0 + value;
            float sumIncreases = 1;
            float sumMultiplicative = 1;
            foreach (var group in modifiers.GroupBy(m => m.ModifierType).OrderBy(g => g.Key))
            {
                switch (group.Key)
                {
                    case ModifierType.Flat:
                        sumAdditions += group.Sum(x => x.Value);
                        break;
                    case ModifierType.Increase:
                        sumIncreases += group.Sum(x => x.Value);
                        break;
                    case ModifierType.Multiplicative:
                        sumMultiplicative += group.Sum(x => x.Value);
                        break;
                }
            }

            return (sumAdditions * sumIncreases) * sumMultiplicative;
        }
    }
}
