namespace Playground.Components
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    public class Calculations
    {
        public static float CalculateFloatValue(float value, List<IModifier> modifiers) => Math.Max(0, CalculateModifiers(modifiers, value));

        private static float CalculateModifiers(IEnumerable<IModifier> modifiers, float value)
        {
            var factor = 1f;
            foreach (var group in modifiers.GroupBy(m => m.Type).OrderBy(g => g.Key))
            {
                switch (group.Key)
                {
                    case ModifierType.Additive:
                        value = ModifyValue(value, group);
                        break;
                    case ModifierType.MultiplicativeSum:
                        value *= factor += group.Sum(x => x.Value);
                        break;
                    case ModifierType.Multiplicative:
                        value = ModifyValue(value, group);
                        break;

                }
            }
            return value;
        }

        private static float ModifyValue(float value, IGrouping<ModifierType, IModifier> modifiers)
        {
            foreach (var modifier in modifiers.OrderBy(x => x.Priority))
            {
                value = modifier.ModifyValue(value);
            }
            return value;
        }
    }
}
