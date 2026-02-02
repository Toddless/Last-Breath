namespace Utilities
{
    using Godot;
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;
    using Core.Modifiers;

    public static class Calculations
    {
        private const float EvasionScalingFactor = 10000f;
        private const float ArmorScalingFactor = 10000f;

        public static float CalculateFloatValue(IReadOnlyList<IModifier> modifiers, float baseValue = 0)
            => Math.Max(0, CalculateModifiers(modifiers, baseValue));

        public static void CalculateFinalDamage(IAttackContext context)
        {
            float baseDamage = context.BaseDamage;
            float additionalDamage = context.AdditionalDamage;
            context.FinalDamage = baseDamage + additionalDamage;
            if (context.IsCritical)
                context.FinalDamage *= context.Attacker.Parameters.CriticalDamage;
            float effectiveArmor = context.Target.Parameters.Armor * (1 - context.Attacker.Parameters.ArmorPenetration);
            context.FinalDamage *= 1 - (effectiveArmor / (effectiveArmor + ArmorScalingFactor));
        }

        public static void CalculateHitSucceeded(IAttackContext context)
        {
            if (ChanceSuccessful(CalculateEvasionChance(context.Target.Parameters.Evade, context.Attacker.Parameters.Accuracy), context.Rnd.RandFloat()))
            {
                context.Result = AttackResults.Evaded;
                context.Attacker.CombatEvents.Publish<TargetEvadedAttackEvent>(new(context));
                if (context.Result is AttackResults.Evaded) return;
            }

            if (ChanceSuccessful(context.Target.Parameters.BlockChance, context.Rnd.RandFloat()))
            {
                context.Result = AttackResults.Blocked;
                context.Attacker.CombatEvents.Publish<TargetBlockedAttackEvent>(new(context));
                if (context.Result is AttackResults.Blocked) return;
            }

            context.Result = AttackResults.Succeed;
        }

        public static float[] CalculateChances<TModifier>(List<INpcModifier> modifiers, float[] chances)
            where TModifier : class, IWeightable, IChangeableChances
        {
            var sortedModifiers = modifiers.OfType<TModifier>().OrderBy(x => x.Weight).ToList();

            sortedModifiers.ForEach(x => ApplyModifier(chances, x));

            NormalizeChances(chances);
            return chances;
        }

        private static void ApplyModifier(float[] chances, IChangeableChances changeableChancesModifier)
        {
            float totalIncrease = changeableChancesModifier.ChancesAffected.Sum(affectedTier => chances[affectedTier] * changeableChancesModifier.Multiplier);

            int minAffectedTier = changeableChancesModifier.ChancesAffected.Min();

            float totalLowerChances = 0f;
            for (int i = minAffectedTier + 1; i < chances.Length; i++)
                totalLowerChances += chances[i];

            float actualDecrease = Mathf.Min(totalIncrease, totalLowerChances);

            float remainingDecrease = actualDecrease;

            for (int i = minAffectedTier + 1; i < chances.Length && remainingDecrease > 0.0001f; i++)
            {
                float proportion = chances[i] / totalLowerChances;
                float decrease = actualDecrease * proportion;

                decrease = Mathf.Min(decrease, chances[i]);
                decrease = Mathf.Min(decrease, remainingDecrease);

                chances[i] -= decrease;
                remainingDecrease -= decrease;
            }

            float increaseRatio = actualDecrease / totalIncrease;

            foreach (int affectedTier in changeableChancesModifier.ChancesAffected)
            {
                float originalIncrease = chances[affectedTier] * changeableChancesModifier.Multiplier;
                float adjustedIncrease = originalIncrease * increaseRatio;
                chances[affectedTier] += adjustedIncrease;
            }
        }

        private static void NormalizeChances(float[] tierChances)
        {
            float sum = tierChances.Sum();
            if (!(Mathf.Abs(sum - 1.0f) > 0.0001f)) return;

            for (int i = 0; i < tierChances.Length; i++)
                tierChances[i] /= sum;
        }

        private static bool ChanceSuccessful(float chance, float randomNumber) => randomNumber <= chance;

        private static float CalculateEvasionChance(float evasion, float accuracy) => 1f / (1f + MathF.Exp(-(evasion - accuracy) / EvasionScalingFactor));

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
