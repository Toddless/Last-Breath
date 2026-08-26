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

        /// <summary>Poison's resistance pair, deliberately kept OUT of <see cref="s_resistanceByType"/>: that table is
        /// exactly what the attack-side "ignore resistances" mark strips, and a damage-over-turn tick is not an attack.</summary>
        private static readonly (EntityParameter Resistance, EntityParameter Penetration) s_poisonResistance =
            (EntityParameter.PoisonResistance, EntityParameter.PoisonResistancePenetration);

        public static float CalculateFloatValue(IReadOnlyList<IModifier> modifiers, float baseValue = 0)
            => Math.Max(0, CalculateModifiers(modifiers, baseValue));

        /// <summary>Crit mitigation cuts the crit's SURPLUS and never the hit under it, so a fully
        /// mitigated critical (1.0, the cap) lands as an ordinary hit rather than for nothing.</summary>
        public static void CalculateInitialAttackDamage(IAttackContext context)
        {
            if (context is { IsCritical: false, ForceCriticalAttack: false }) return;

            // Mitigation is 0 for most targets -> factor is the raw multiplier (no-op). Bounds (0..1) live in
            // EntityParametersComponent. Scaling the WHOLE multiplier instead would invert the crit above
            // 1/RawCriticalDamage of mitigation: an 1.8x crit under 80% landed at 0.36x — weaker than a normal hit.
            float critMitigation = context.Target.Parameters.GetValueForParameter(EntityParameter.CriticalDamageMitigation);
            // The crit multiplies the whole dictionary — elemental components crit alongside Physical.
            context.ScaleDamage(1 + ((context.RawCriticalDamage - 1) * (1 - critMitigation)));
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
        /// Runs both modifier sides of one hit: the receiver's list first, then the source's. The hit names its
        /// receiver here, which is what lets a line about damage TAKEN gate on the fighter it is written for
        /// instead of on "anyone but the source" — the reading that made self-inflicted damage invisible.
        /// Self-inflicted damage runs the list ONCE: source and receiver are the same handler, and a second
        /// pass would square every line it holds.
        /// </summary>
        public static void ApplyDamageModifiers(IDamageContext context, IFightable target)
        {
            context.Target = target;
            target.ModifierHandler.Apply(context);
            if (context.Source.IsSame(target.InstanceId)) return;

            context.Source.ModifierHandler.Apply(context);
        }

        /// <summary>
        /// Defender-side mitigation — the single place that knows how each damage type is reduced.
        /// Pipeline order: incoming modifiers (target) -> outgoing modifiers (source) -> mitigation per component
        /// -> suppression -> shield -> barrier -> stage guard -> health.
        /// Rules: Physical and Bleed — armor scaled by the source's armor penetration; Fire/Cold/Lightning —
        /// the matching resistance (fraction 0..1) scaled by the source's resistance penetration; Burning — fire
        /// resistance and Poison — poison resistance the same way (the "attacks ignore resistances" flag covers
        /// neither: that mark is attack-side and a damage-over-turn tick is not an attack); Sacred and Blight
        /// pass through untouched, each by its own named rule.
        /// Mitigated values are written back per component (<see cref="IDamageContext.Set"/>),
        /// so UI and statistics see the real post-mitigation damage split.
        /// <paramref name="rnd"/> is the stream the suppression roll burns. It is required rather than
        /// defaulted: a hit resolved on a generator nobody named is a hit nobody can reproduce, and the
        /// caller owning the target already owns a stream.
        /// </summary>
        public static void CalculateMitigation(IDamageContext context, IFightable target, IRandomNumberGenerator rnd)
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
        private static void ApplySuppression(IDamageContext context, IFightable target, IRandomNumberGenerator rnd)
        {
            if (!OriginatesFromAbility(context)) return;

            // Bounds (chance 0..1, fraction 0..0.75) live in EntityParametersComponent's table.
            float chance = target.Parameters.GetValueForParameter(EntityParameter.SuppressChance);
            float suppression = target.Parameters.GetValueForParameter(EntityParameter.Suppress);
            if (chance <= 0 || suppression <= 0 || context.TotalDamage <= 0) return;

            if (!ChanceRoll.Roll(chance, rnd, target.Parameters.GetChanceLuck(EntityParameter.SuppressChance))) return;

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
                DamageType.Poison => ApplyResistance(damage, context.Source, target, s_poisonResistance),
                DamageType.Physical or DamageType.Bleed => ApplyArmor(damage, context.Source, target),
                // Sacred ignores armor and resistances by design; Blight damages health directly and neither cuts it.
                DamageType.Sacred or DamageType.Blight => damage,
                _ => Unruled(type, damage)
            };
        }

        /// <summary>A type no rule names passes through and says so: staying silent would hand a typo
        /// or a freshly added member the "reduced by nothing" of the sacred.</summary>
        private static float Unruled(DamageType type, float damage)
        {
            Tracker.TrackNotFound($"Mitigation rule for damage type '{type}'");
            return damage;
        }

        private static float ApplyResistance(float damage, IFightable source, IFightable target, (EntityParameter Resistance, EntityParameter Penetration) elemental)
        {
            // The target mitigates with its resistance cut down to its own maximum, and the source's
            // penetration bites into that: an overcap is a reserve against shred, never against penetration.
            float effective = ResistanceParameters.Effective(target.Parameters, elemental.Resistance);
            float resist = effective * (1 - source.Parameters.GetValueForParameter(elemental.Penetration));
            return damage * (1 - resist);
        }

        private static float ApplyArmor(float damage, IFightable source, IFightable target)
        {
            float effectiveArmor = target.Parameters.Armor * (1 - source.Parameters.ArmorPenetration);
            return damage * (1 - effectiveArmor / (effectiveArmor + ArmorScalingFactor));
        }


        /// <summary>How the attack lands, rolled on the attack's own stream.</summary>
        public static AttackResults ResolveAttackOutcome(IAttackContext context) => ResolveAttackOutcome(context, context.Rnd.Randf);

        /// <summary>
        /// The landing rule: evasion is rolled first, block second, and a hit marked unevadable/unblockable
        /// skips the branch that would stop it. Both rolls answer to the defender's luck on the parameter
        /// behind them. <paramref name="draw"/> is named rather than taken from the context because the
        /// generator an attack carries is an engine object no sandbox can build.
        /// Evasion is contested by the attacker's accuracy; block is the defender's flat chance and is not.
        /// Either verdict negates the attack whole — no damage and no effects, since riders and reactions
        /// alike fire on <see cref="AttackResults.Succeed"/> only — but a series of attacks is interrupted
        /// by an evade alone.
        /// </summary>
        public static AttackResults ResolveAttackOutcome(IAttackContext context, Func<float> draw)
        {
            var defender = context.Target.Parameters;

            if (!context.IsUnevadable && RollsEvade(context, defender, draw))
                return AttackResults.Evaded;

            if (!context.IsUnblockable && RollsBlock(context.Target, defender, draw))
                return AttackResults.Blocked;

            return AttackResults.Succeed;
        }

        /// <summary>
        /// The evasion roll and the denial that can take its verdict away: a defender whose evasion is denied
        /// (Stoicism) is hit whatever his numbers say.
        ///
        /// The draw is taken WHATEVER the denial and only the verdict is gated — the same discipline the
        /// block roll keeps: a roll skipped would pull every later draw of the fight one step forward, and
        /// the same seed would play out differently for no reason but what the defender happens to carry.
        ///
        /// The denial is read off the defender at the moment of the roll, so a keystone taken or refunded
        /// mid-battle is in force from the very next swing. Evasion itself stays the number it was, and
        /// everything counted per point of it is worth exactly what it was worth before.
        /// </summary>
        private static bool RollsEvade(IAttackContext context, IEntityParametersComponent defender, Func<float> draw)
        {
            bool rolled = ChanceRoll.Roll(CalculateEvasionChance(defender.Evade, context.RawAccuracy), draw,
                defender.GetChanceLuck(EntityParameter.Evade));

            return rolled && !defender.IsChanceDenied(EntityParameter.Evade);
        }

        /// <summary>
        /// The block roll and the guard that owns it: block belongs to the Strength stance and to nothing
        /// else, so outside that stance neither the baseline every fighter is born with nor a line off gear
        /// or the tree ever stops a swing.
        ///
        /// The draw is taken WHATEVER the stance and only the verdict is gated. A roll skipped would pull
        /// every later draw of the fight one step forward, and then the same seed would play out differently
        /// for no reason but which guard the defender happened to stand in — the stance may decide whether a
        /// hit is blocked and may not decide anybody's stream.
        ///
        /// The stance is read off the defender's own ability book at the moment of the roll, the same live
        /// reading <see cref="Modifiers.Conditions.StanceCondition"/> makes, so a switch mid-battle is in
        /// force from the very next swing. NPCs answer through the same book and block on whatever chance
        /// they carry — today none of them carries any.
        /// </summary>
        private static bool RollsBlock(IFightable defender, IEntityParametersComponent parameters, Func<float> draw)
        {
            bool rolled = ChanceRoll.Roll(parameters.BlockChance, draw, parameters.GetChanceLuck(EntityParameter.BlockChance));
            return rolled && defender.AbilityBook.CurrentStance == Stance.Strength;
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

        /// <summary>A conditional modifier is skipped while its condition is off instead of being taken
        /// away — wherever the resolver reached it from, the entity's own list or a source it pulls from.
        /// A flip is a recalculation and never a rewiring.</summary>
        private static bool IsModifierActive(IModifier modifier) => modifier is not IConditionalModifier { IsActive: false };
    }
}
