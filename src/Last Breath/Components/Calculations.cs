namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class Calculations
    {
        public static float CalculateFloatValue(float value, IReadOnlyList<IModifier> modifiers) => Math.Max(0, CalculateModifiers(modifiers, value));

        public static float DamageAfterCrit(float damage, ICharacter target) => damage *= target.Damage.CriticalDamage;

        public static float DamageAfterArmor(float damage, ICharacter target) => Mathf.Max(0, damage * (1 - Mathf.Min(target.Defense.Armor / 1000, target.Defense.MaxReduceDamage)));

        private static float CalculateModifiers(IEnumerable<IModifier> modifiers, float value)
        {
            var factor = 1f;
            foreach (var group in modifiers.GroupBy(m => m.Type).OrderBy(g => g.Key))
            {
                switch (group.Key)
                {
                    case ModifierType.Flat:
                        value = ModifyValue(value, group);
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

        private static float ModifyValue(float value, IGrouping<ModifierType, IModifier> modifiers)
        {
            foreach (var modifier in modifiers.OrderByDescending(x => x.Priority))
            {
                value = modifier.ModifyValue(value);
            }
            return value;
        }
    }
}
