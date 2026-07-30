namespace Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Context;
    using Entity;
    using Entity.Components;
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

        private static readonly Dictionary<DamageType, (EntityParameter Resistance, EntityParameter Penetration)> s_resistanceByType = new()
        {
            [DamageType.Fire] = (EntityParameter.FireResistance, EntityParameter.FireResistancePenetration),
            [DamageType.Cold] = (EntityParameter.ColdResistance, EntityParameter.ColdResistancePenetration),
            [DamageType.Lightning] = (EntityParameter.LightningResistance, EntityParameter.LightningResistancePenetration),
        };

        public static float CalculateFloatValue(IReadOnlyList<IModifier> modifiers, float baseValue = 0)
            => Math.Max(0, CalculateModifiers(modifiers, baseValue));

        public static void CalculateInitialAttackDamage(IAttackContext context)
        {
            if (context is { IsCritical: false, ForceCriticalAttack: false }) return;

            // Mitigation is 0 for most targets -> factor is 1 (no-op). Bounds (0..1) live in EntityParametersComponent.
            float critMitigation = context.Target.Parameters.GetValueForParameter(EntityParameter.CriticalDamageMitigation);
            // The crit multiplies the whole dictionary — elemental components crit alongside Physical.
            context.ScaleDamage(context.RawCriticalDamage * (1 - critMitigation));
        }

        /// <summary>The one place a resolved attack becomes a damage context: every component
        /// (physical + weapon elementals + whatever mutators reshaped) crosses over as-is.</summary>
        public static DamageContext ComposeAttackDamage(IAttackContext context)
        {
            var damageContext = new DamageContext
            {
                Source = context.Attacker, Cause = DamageCause.Attack, IsCrit = context.ForceCriticalAttack || context.IsCritical, SourceAbilityId = context.SourceAbilityId
            };
            foreach ((DamageType type, float damage) in context.DamageComponents)
                damageContext.Add(type, damage);
            return damageContext;
        }

        /// <summary>
        /// Defender-side mitigation — the single place that knows how each damage type is reduced.
        /// Pipeline order: outgoing modifiers (source) -> incoming modifiers (target) -> mitigation per component
        /// -> suppression -> shield -> barrier -> stage guard -> health.
        /// Rules: Physical and Bleed — armor scaled by the source's armor penetration; Fire/Cold/Lightning —
        /// the matching resistance (fraction 0..1) scaled by the source's resistance penetration; Burning — fire
        /// resistance the same way (but the "attacks ignore resistances" flag never covers it — that mark is
        /// attack-side); Pure and Poison pass through untouched.
        /// Mitigated values are written back per component (<see cref="IDamageContext.Set"/>),
        /// so UI and statistics see the real post-mitigation damage split.
        /// <paramref name="rnd"/> is the stream the suppression roll burns; callers that own one pass it in.
        /// </summary>
        // TODO:
        // Параметр rnd временный: пока его не передают, подавление роллит на анонимном генераторе, созданном
        // на этот удар. Проводку боевых стримов делает T-13 «Проводка бойцов» — после неё параметр становится
        // обязательным, и компилятор заставит каждую точку вызова назвать свой стрим.
        public static void CalculateMitigation(IDamageContext context, IFightable target, IRandomNumberGenerator? rnd = null)
        {
            // Snapshot: Set() mutates the collection we are iterating
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, MitigateComponent(type, damage, context, target));

            ApplySuppression(context, target, rnd);
        }

        /// <summary>
        /// Suppression — the defender's chance (<see cref="EntityParameter.SuppressChance"/>) to take a fraction
        /// (<see cref="EntityParameter.Suppress"/>) less of an incoming hit that originates from an ability.
        /// It is a layer of defence against abilities, and it follows origin rather than delivery: an ability that
        /// hits through real attacks is covered exactly like one that builds its damage directly
        /// (see <see cref="OriginatesFromAbility"/>). A basic attack, a damage-over-time tick, a passive, an item
        /// and the environment pass through untouched and never reach the roll.
        /// Position: AFTER per-type mitigation and BEFORE the absorption chain, so the suppressed number is what
        /// shields, barriers and the stage guard soak and what the log shows — mitigation stays the only place
        /// that knows damage types, and suppression stays type-agnostic.
        /// One roll per hit, not per component: a hit is either suppressed or it is not.
        /// A hit that cannot be suppressed — no ability origin, or a defender without the stat — never touches the
        /// stream, so nobody else's rolls shift.
        /// The roll needs no preview guard: previewing is an ability-activation concept
        /// (<see cref="Battle.Abilities.IAbilityActivationContext.IsPreview"/>), no preview path builds a
        /// <see cref="DamageContext"/> and none reaches TakeDamage — there is no second pass to burn a roll on.
        /// </summary>
        private static void ApplySuppression(IDamageContext context, IFightable target, IRandomNumberGenerator? rnd)
        {
            if (!OriginatesFromAbility(context)) return;

            // Bounds (chance 0..1, fraction 0..0.75) live in EntityParametersComponent's table.
            float chance = target.Parameters.GetValueForParameter(EntityParameter.SuppressChance);
            float suppression = target.Parameters.GetValueForParameter(EntityParameter.Suppress);
            if (chance <= 0 || suppression <= 0 || context.TotalDamage <= 0) return;

            var generator = rnd ?? new DefaultRandomNumberGenerator();
            if (!ChanceSuccessful(chance, generator.RandFloat())) return;

            // Snapshot: Set() mutates the collection we are iterating
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, damage * (1 - suppression));
        }

        /// <summary>Whether the hit was born of an ability, whichever way it was delivered: abilities that damage
        /// directly declare <see cref="DamageCause.Ability"/>, while abilities that damage through real attacks
        /// arrive as <see cref="DamageCause.Attack"/> carrying the caster's
        /// <see cref="IDamageContext.SourceAbilityId"/> (stamped on the attack, moved over by
        /// <see cref="ComposeAttackDamage"/>). A basic attack carries neither mark.</summary>
        private static bool OriginatesFromAbility(IDamageContext context)
            => context.Cause == DamageCause.Ability || !string.IsNullOrEmpty(context.SourceAbilityId);

        private static float MitigateComponent(DamageType type, float damage, IDamageContext context, IFightable target)
        {
            if (s_resistanceByType.TryGetValue(type, out (EntityParameter Resistance, EntityParameter Penetration) elemental))
                return context.IgnoreResistances ? damage : ApplyResistance(damage, context.Source, target, elemental);

            return type switch
            {
                DamageType.Burning => ApplyResistance(damage, context.Source, target, s_resistanceByType[DamageType.Fire]),
                DamageType.Physical or DamageType.Bleed => ApplyArmor(damage, context.Source, target),
                _ => damage // Pure and Poison are unmitigated by design
            };
        }

        private static float ApplyResistance(float damage, IFightable source, IFightable target, (EntityParameter Resistance, EntityParameter Penetration) elemental)
        {
            // The 0..0.8 resistance cap lives in EntityParametersComponent's bounds table; penetration is capped 0..1 there too.
            float resist = target.Parameters.GetValueForParameter(elemental.Resistance) * (1 - source.Parameters.GetValueForParameter(elemental.Penetration));
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
            foreach (var group in modifiers
                         .Where(IsModifierActive)
                         .GroupBy(m => m.ModifierValueType)
                         .OrderBy(g => g.Key))
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
                    case ModifierValueType.Flag:
                    default:
                        break;
                }
            }

            return (sumAdditions * sumIncreases) * sumMultiplicative;
        }

        /// <summary>Conditional modifiers stay in the list permanently and are skipped while their condition is off.</summary>
        private static bool IsModifierActive(IModifier modifier) => modifier is not IConditionalModifier { IsActive: false };
    }
}
