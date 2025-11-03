namespace LastBreath.Components
{
    using Godot;
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using System.Collections.Generic;

    public class Calculations
    {
        public static float CalculateFloatValue(float value, IReadOnlyList<IModifierInstance> modifiers) => Math.Max(0, CalculateModifiers(modifiers, value));

        public static float DamageReduceByArmor(IAttackContext context)
        {
            var damage = context.Damage;
            if (context.IsCritical)
            {
                damage *= context.CriticalDamageMultiplier;
            }

            if(context.IgnoreArmor) return damage;

            return Mathf.Max(0, damage * (1 - Mathf.Min(context.Armor / 1000, context.MaxReduceDamage)));
        }

        private static float CalculateModifiers(IEnumerable<IModifierInstance> modifiers, float value)
        {
            var factor = 1f;
            foreach (var group in modifiers.GroupBy(m => m.ModifierType).OrderBy(g => g.Key))
            {
                switch (group.Key)
                {
                    case ModifierType.Flat:
                        value += group.Sum(x=>x.Value);
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
            {
                value *= modifier.Value;
            }
            return value;
        }
    }
}
