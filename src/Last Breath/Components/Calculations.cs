namespace LastBreath.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Modifiers;
    using Godot;

    public class Calculations
    {
        public static float CalculateFloatValue(float value, IReadOnlyList<IModifier> modifiers) => Math.Max(0, CalculateModifiers(modifiers, value));

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

        private static float CalculateModifiers(IEnumerable<IModifier> modifiers, float value)
        {
            var factor = 1f;
            foreach (var group in modifiers.GroupBy(m => m.Type).OrderBy(g => g.Key))
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

        private static float ModifyValue(float value, IGrouping<ModifierType, IModifier> modifiers)
        {
            foreach (var modifier in modifiers.OrderByDescending(x => x.Priority))
            {
                value *= modifier.Value;
            }
            return value;
        }
    }
}
