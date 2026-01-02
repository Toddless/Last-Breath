namespace Utilities
{
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using System.Collections.Generic;

    public static class Calculations
    {
        public static float CalculateFloatValue(IReadOnlyList<IModifier> modifiers, float baseValue = 0)
            => Math.Max(0, CalculateModifiers(modifiers, baseValue));

        public static void CalculateFinalDamage(IAttackContext context)
        {
            float baseDamage = context.BaseDamage;
            float additionalDamage = context.AdditionalDamage;
            context.FinalDamage = baseDamage + additionalDamage;
            if (context.IsCritical)
                context.FinalDamage *= context.Attacker.Parameters.CriticalDamage;
        }

        public static void CalculateHitSucceeded(IAttackContext context)
        {
            // if (ChanceSuccessful(context.Target.Parameters.Evade, context.Rnd.RandFloat()))
            // {
            //     context.Result = AttackResults.Evaded;
            //     context.Attacker.CombatEvents.Publish<AttackEvadedEvent>(new(context));
            //     if (context.Result is AttackResults.Evaded) return;
            // }
            //
            // if (ChanceSuccessful(context.Target.Parameters.BlockChance, context.Rnd.RandFloat()))
            // {
            //     context.Result = AttackResults.Blocked;
            //     context.Attacker.CombatEvents.Publish<AttackBlockedEvent>(new(context));
            //     if (context.Result is AttackResults.Blocked) return;
            // }

            context.Result = AttackResults.Succeed;
        }

        private static bool ChanceSuccessful(float chance, float randomNumber) => randomNumber <= chance;


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
