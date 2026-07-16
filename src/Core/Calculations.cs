namespace Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Context;
    using Entity;
    using Entity.NpcModifiers;
    using Enums;
    using Events;
    using Interfaces;
    using Modifiers;
    using Godot;

    public static class Calculations
    {
        private const float EvasionScalingFactor = 10000f;
        private const float ArmorScalingFactor = 10000f;

        /// <summary>Hard cap for elemental resistances: damage of a resisted type cannot be reduced by more than this fraction.</summary>
        private const float MaxResistance = 0.8f;

        private static readonly Dictionary<DamageType, EntityParameter> s_resistanceByType = new()
        {
            [DamageType.Fire] = EntityParameter.FireResistance,
            [DamageType.Cold] = EntityParameter.ColdResistance,
            [DamageType.Lightning] = EntityParameter.LightningResistance,
        };

        public static float CalculateFloatValue(IReadOnlyList<IModifier> modifiers, float baseValue = 0)
            => Math.Max(0, CalculateModifiers(modifiers, baseValue));

        public static void CalculateInitialAttackDamage(IAttackContext context)
        {
            // TODO:
            // for now all attacks are physical. Later if we have modifiers like "deal 30% attack damage as cold"
            // where we calculate it damage?
            context.FinalDamage = context.BaseDamage + context.AdditionalDamage;
            if (context is { IsCritical: false, ForceCriticalAttack: false }) return;

            // Mitigation is 0 for most targets -> factor is 1 (no-op). Clamped so over-stacking can't invert damage.
            float critMitigation = Mathf.Clamp(context.Target.Parameters.GetValueForParameter(EntityParameter.CriticalDamageMitigation), 0f, 1f);
            context.FinalDamage *= context.RawCriticalDamage * (1 - critMitigation);
        }

        /// <summary>
        /// Defender-side mitigation — the single place that knows how each damage type is reduced.
        /// Pipeline order: outgoing modifiers (source) -> incoming modifiers (target) -> mitigation per component -> barrier -> health.
        /// Rules: Physical/Normal — armor scaled by the source's armor penetration; Fire/Cold/Lightning — the matching
        /// resistance (fraction 0..1); Pure and DoT types (Poison/Burning/Bleed) pass through untouched.
        /// Mitigated values are written back per component (<see cref="IDamageContext.Set"/>),
        /// so UI and statistics see the real post-mitigation damage split.
        /// </summary>
        public static void CalculateMitigation(IDamageContext context, IFightable target)
        {
            // Snapshot: Set() mutates the collection we are iterating
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, MitigateComponent(type, damage, context, target));
        }

        private static float MitigateComponent(DamageType type, float damage, IDamageContext context, IFightable target)
        {
            if (!context.IgnoreResistances && s_resistanceByType.TryGetValue(type, out EntityParameter resistance))
                return ApplyResistance(damage, target, resistance);
            return type is DamageType.Physical ? ApplyArmor(damage, context.Source, target) : damage;
            // Pure and DoT statuses (Poison/Burning/Bleed) are unmitigated; their rules land here if defined
        }

        private static float ApplyResistance(float damage, IFightable target, EntityParameter resistance)
        {
            float resist = Mathf.Clamp(target.Parameters.GetValueForParameter(resistance), 0f, MaxResistance);
            return damage * (1 - resist);
        }

        private static float ApplyArmor(float damage, IFightable source, IFightable target)
        {
            float effectiveArmor = target.Parameters.Armor * (1 - source.Parameters.ArmorPenetration);
            return damage * (1 - effectiveArmor / (effectiveArmor + ArmorScalingFactor));
        }


        public static void CalculateSucceeded(IAttackContext context)
        {
            if (!context.IsUnevadable && ChanceSuccessful(CalculateEvasionChance(context.Target.Parameters.Evade, context.RawAccuracy), context.Rnd.Randf()))
            {
                context.Result = AttackResults.Evaded;
                context.Attacker.CombatEvents.Publish<TargetEvadedAttackEvent>(new(context));
                if (context.Result is AttackResults.Evaded) return;
            }

            if (!context.IsUnblockable && ChanceSuccessful(context.Target.Parameters.BlockChance, context.Rnd.Randf()))
            {
                context.Result = AttackResults.Blocked;
                context.Attacker.CombatEvents.Publish<TargetBlockedAttackEvent>(new(context));
                if (context.Result is AttackResults.Blocked) return;
            }

            context.Result = AttackResults.Succeed;
        }

        public static float[] CalculateChances<TModifier>(IReadOnlyList<INpcModifier> modifiers, float[] chances)
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

        /// <summary>Armor-style curve: accuracy at or above evasion guarantees a hit; only the excess of evasion over accuracy grants evade chance.</summary>
        private static float CalculateEvasionChance(float evasion, float accuracy)
        {
            float advantage = Math.Max(0, evasion - accuracy);
            return advantage / (advantage + EvasionScalingFactor);
        }

        private static float CalculateModifiers(IEnumerable<IModifier> modifiers, float value = 0)
        {
            float sumAdditions = 0 + value;
            float sumIncreases = 1;
            float sumMultiplicative = 1;
            foreach (var group in modifiers.Where(IsModifierActive).GroupBy(m => m.ModifierValueType).OrderBy(g => g.Key))
            {
                switch (group.Key)
                {
                    case ModifierValueType.Flat:
                        sumAdditions += group.Sum(x => x.Value);
                        break;
                    case ModifierValueType.Increase:
                        sumIncreases += group.Sum(x => x.Value);
                        break;
                    case ModifierValueType.Multiplicative:
                        sumMultiplicative += group.Sum(x => x.Value);
                        break;
                }
            }

            return (sumAdditions * sumIncreases) * sumMultiplicative;
        }

        /// <summary>Conditional modifiers stay in the list permanently and are skipped while their condition is off.</summary>
        private static bool IsModifierActive(IModifier modifier) => modifier is not IConditionalModifier { IsActive: false };
    }
}
