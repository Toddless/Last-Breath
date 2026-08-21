namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Data.AbilityData;
    using Core.Enums;
    using CriticalCalculation;
    using DarkShroud;
    using Armageddon;
    using BerserkFury;
    using DoubleStrike;
    using Effects;
    using IncreasingPressure;
    using PoisonCoating;
    using PoisonExplosion;
    using PassiveSkills;
    using Riders;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;
    using IceAegis;
    using IceBlock;

    public partial class AbilityProvider
    {
        /// <summary>One factory registration: the parameter keys the augment it builds moves, declared
        /// beside the code that builds it, and the build itself. The keys are why the registration is a
        /// pair rather than a bare delegate — a factory carries its work where the parameter table
        /// cannot see it, so without them <see cref="ParametersMovedBy"/> answers nothing for this half
        /// of the registry and every ledger of shared keys has to write its rows by hand. Declared here
        /// and not guessed elsewhere: a new factory cannot be added without saying what it moves, and
        /// an empty list is the honest answer for the many that install a rider, a strategy or a flag
        /// and touch no key at all.
        /// <para>MOVES is meant strictly — a key of the ability's own that the augment decorates.
        /// LENDING a key the ability never had is the opposite direction and is not declared here: it
        /// creates the number rather than moving it, so a ledger reading it as a move would report a
        /// landing on every ability that does not have the key and is inert on all of them.</para></summary>
        private readonly record struct AugmentFactory(string[] MovedParameters, Func<AbilityAugmentData, IAugment> Build)
        {
            /// <summary>The effect the augment lays, for a factory that names one; empty for the rest.
            /// A record naming its effect in DATA is not written here — there the record is the word, and
            /// a second one beside it would be free to disagree.</summary>
            public string LaidEffectId { get; private init; } = string.Empty;

            /// <summary>A registration whose augment lays an effect the code names. The id is written
            /// ONCE and serves both halves — the build receives it and the card reads it off the same
            /// registration — so declaring one effect and laying another is not a thing that can be
            /// written down.</summary>
            public static AugmentFactory Laying(
                string effectId,
                string[] movedParameters,
                Func<AbilityAugmentData, string, IAugment> build) =>
                new(movedParameters, data => build(data, effectId)) { LaidEffectId = effectId };
        }

        /// <summary>
        /// The augments that need code — a behaviour to install, a strategy to swap, a member of one
        /// ability class to reach for, or a number measured as a share of the one it moves. An augment
        /// that only moves numbers is not written here: it is a row of the parameter table
        /// (<c>AbilityProvider.ParameterAugments.cs</c>) and shares its one class with all the others.
        /// The two halves never name the same augment.
        /// <para>Built on first use rather than in a field initializer: the entries laying an effect reach
        /// the effect registry for its canonical numbers, and a field initializer may not touch the
        /// instance that holds it.</para>
        /// </summary>
        private Dictionary<string, AugmentFactory> AbilityUpgrades => field ??= new()
        {
            // Augments of the base contract every ability honours. They belong to no ability, so they
            // are written once — and those that move a number state it as a share of the number they
            // move, the only figure that means the same thing on a free cast and on a five-hundred one.
            ["Augment_Reduce_Cost"] = new([AbilityParameter.CostValue], data =>
                new AugmentReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.15f))),
            ["Augment_Reduce_Cooldown"] = new([AbilityParameter.Cooldown], data =>
                new AugmentReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownTurns", 1f))),
            ["Augment_Reduce_Cooldown_Add_Cost"] = new([AbilityParameter.Cooldown, AbilityParameter.CostValue], data =>
                new AugmentReduceCooldownAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownTurns", 2f),
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.15f))),
            // The three records whose design line mixes the shapes: whole turns or scale points of their
            // own on one key, a share of the ability's own price on the other. The share is what keeps
            // them out of the parameter table.
            ["Augment_Reduce_Cooldown_And_Cost"] = new([AbilityParameter.Cooldown, AbilityParameter.CostValue], data =>
                new AugmentReduceCooldownAndCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownTurns", 1f),
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.10f))),
            ["Augment_Reduce_Cost_Add_Cooldown"] = new([AbilityParameter.CostValue, AbilityParameter.Cooldown], data =>
                new AugmentReduceCostAddCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.25f),
                    data.UpgradeProperties.GetValueOrDefault("cooldownTurns", 1f))),
            ["Augment_Increasing_Scales_Add_Cost"] = new([AbilityParameter.WeaponDamageScale, AbilityParameter.SpellDamageScale, AbilityParameter.CostValue], data =>
                new AugmentRaiseScalesAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.25f),
                    data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.25f),
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.15f))),
            // The only record that moves a FAMILY of keys rather than a named one — which is why it is a
            // factory and not a table row: the table says which key a record stands on, and this one does
            // not know WHICH of them it lands on until it is seated. What it declares is the shared half
            // of that family: every key of the book that any shipped ability calls an applied duration.
            // The private members of the family are the abilities' own words and no ledger of shared keys
            // asks about them. Kept honest by TheAppliedDurationRecordDeclaresExactlyTheSharedDurations-
            // TheBookRegisters, which reads the family off the shipped abilities in both directions — a
            // new shared duration would otherwise go unledgered in silence.
            ["Augment_Applied_Duration"] = new([AbilityParameter.StunDuration, AbilityParameter.PoisonDuration], data =>
                new AugmentAppliedDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("turns", 1f))),
            ["Augment_Cost_Type_Health"] = new([AbilityParameter.CostType], data =>
                new AugmentCostTypeOverride(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Costs.Health)),
            // Belongs to no ability either, though it is not the base contract it works through but the
            // delivery one: whoever lands a hit extends the poison on what he hit.
            ["Augment_Extend_Poison"] = new([], data =>
                new AugmentExtendPoison(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1))),
            // Nor does this one: whatever lands the impact, the target is poisoned. Both of its numbers
            // become parameters of the ability it is seated on, which is what lets an amplifier of
            // poison reach the stacks it lays — and is also why it declares no key: it LENDS those two
            // numbers rather than moving numbers of the ability's own.
            ["Augment_Poison_Attack_Series"] = new([], data =>
                new AugmentPoisonOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("poisonDuration", 3f),
                    data.UpgradeProperties.GetValueOrDefault("poisonPotency", 0.7f))),
            // Both tally the caster's successful attacks, which every attacking ability has — hence the
            // generic impact rider rather than the Series of Attacks execution strategy they used to be.
            ["Augment_Apply_Buff_Critical_Chance"] = new([], data =>
                new AugmentImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new BuffAfterAttacksImpactRider(
                        data.Id,
                        (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 6),
                        new CriticalChanceBuffEffect(
                            (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                            (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3),
                            data.UpgradeProperties.GetValueOrDefault("criticalChance", 0.15f))))),
            ["Augment_Apply_Buff_Critical_Damage"] = new([], data =>
                new AugmentImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new BuffAfterAttacksImpactRider(
                        data.Id,
                        (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 9),
                        new CriticalDamageBuffEffect(
                            (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                            (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3),
                            data.UpgradeProperties.GetValueOrDefault("criticalDamage", 0.25f))))),
            ["Augment_Increasing_Pressure_Single_Empowered_Attack"] = new([], data =>
                new AugmentIpSingleEmpoweredAttack(
                    data.Id,
                    data.Tags,
                    data.Tier)),
            ["Augment_Increasing_Pressure_Last_Attack_Always_Crit"] = new([], data =>
                new AugmentIpLastAttackAlwaysCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new LastAttackAlwaysCritContextModifier())),
            // The modifier already asks the context whether the attack is the first one, so the record
            // needs the generic attack-modifier upgrade rather than Increasing Pressure's own.
            ["Augment_First_Attack_Crit_Damage"] = new([], data =>
                new AugmentAttackModifier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new FirstAttackCritContextModifier(data.UpgradeProperties.GetValueOrDefault("critDamageBonus", 0.70f)))),
            ["Augment_Random_Cooldown"] = new([], data =>
                new AugmentActivationRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new ReduceRandomCooldownActivationRider(data.Id, (int)data.UpgradeProperties.GetValueOrDefault("amount", 1)))),
            ["Augment_Next_Cast_Sacred"] = new([], data =>
                new AugmentCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new NextCastSacredConversionEffect(ability.Id,
                        data.UpgradeProperties.GetValueOrDefault("fraction", 0.3f)))),
            ["Augment_Overload_Stage_Four_Resets_Cooldown"] = new([], data =>
                new DelegateAugment<Overload.Overload>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ResetCooldownOnFinalStage = true,
                    ability => ability.ResetCooldownOnFinalStage = false)),
            ["Augment_Chain_Lightning_Ignore_Resistances"] = new([], data =>
                new DelegateAugment<ChainLightning.ChainLightning>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.IgnoreResistances = true,
                    ability => ability.IgnoreResistances = false)),
            ["Augment_Armageddon_Burning"] = new([], data =>
                new AugmentArmStage3Burning(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.7f))),
            ["Augment_Armageddon_All_Targets"] = new([AbilityParameter.Cooldown], data =>
                new AugmentArmAllTargets(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("additionalCooldown", 3))),
            ["Augment_Porcupine_Echo"] = new([], data =>
                new AugmentCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new TemporarySkillEffect("Effect_Echo", ((Porcupine.Porcupine)ability).Duration,
                        new EchoPassiveSkill(
                            data.UpgradeProperties.GetValueOrDefault("delayedPercent", 0.3f),
                            (int)data.UpgradeProperties.GetValueOrDefault("turns", 2))))),
            ["Augment_Empowered_Ability_Free_Cast"] = new([], data =>
                new AugmentCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new FreeCastEffect(ability.Id))),
            ["Augment_Berserk_Fury_Burning"] = AugmentFactory.Laying("Effect_Burning_Fury", [], (data, effectId) =>
                new AugmentBfFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon(effectId, duration, healthPercent))),
            ["Augment_Berserk_Fury_Primal"] = AugmentFactory.Laying("Effect_Primal_Fury", [], (data, effectId) =>
                new AugmentBfFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon(effectId, duration, healthPercent))),
            ["Augment_Berserk_Fury_Healing"] = AugmentFactory.Laying("Effect_Healing_Fury", [], (data, effectId) =>
                new AugmentBfFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon(effectId, duration, healthPercent))),
            ["Augment_Double_Strike_Two_Attacks_Apply_Buff"] = new([], data =>
                new AugmentDsBothHitsBuff(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.45f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3))),
            ["Augment_Critical_Calculation_Lucky_Crit"] = new([], data =>
                new AugmentCcLuckyCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3))),
            // A buff laid on the caster after the cast is what an activation rider is; no member of
            // Critical Calculation was ever touched, only the rider dictionary every ability has.
            ["Augment_Leach_On_Crit"] = new([], data =>
                new AugmentActivationRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new AbilityBuffActivationRider(new CritLeechEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 1),
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.05f))))),
            ["Augment_Dark_Shroud_Immortality"] = AugmentFactory.Laying("Effect_Evade_First_Death", [], (data, effectId) =>
                new AugmentDsImmortality(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => EffectFromCanon(effectId))),
            ["Augment_Poison_Explosion_Execute_Bosses"] = new([], data =>
                new AugmentPeExecuteBosses(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("stacksMultiplier", 2))),
            ["Augment_Poison_Explosion_Spread_Poison"] = new([], data =>
                new AugmentPeSpreadPoison(
                    data.Id,
                    data.Tags,
                    data.Tier)),
            ["Augment_Poison_Explosion_Transfer_Poison_On_Death"] = new([], data =>
                new AugmentPeTransferPoisonOnDeath(
                    data.Id,
                    data.Tags,
                    data.Tier)),
            ["Augment_Poison_Coating_Apply_Poison_For_Each_Enemy"] = new([], data =>
                new AugmentPcMultiStackOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier)),
            ["Augment_Ice_Shards_Multicast"] = new([], data =>
                new DelegateAugment<IceShards.IceShards>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.Activation.BonusChance += data.UpgradeProperties.GetValueOrDefault("amount", 0.25f),
                    ability => ability.Activation.BonusChance -= data.UpgradeProperties.GetValueOrDefault("amount", 0.25f))),
            ["Augment_Ice_Shards_Critical_Hit_Ignores_Cold_Res"] = new([], data =>
                new DelegateAugment<IceShards.IceShards>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.CritIgnoresColdResistance = true,
                    ability => ability.CritIgnoresColdResistance = false)),
            ["Augment_Ice_Aegis_Shield_Reflect_Damage"] = new([IceAegis.IceAegis.Parameters.ReflectPercent], data =>
                new AugmentIaParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.ReflectPercent,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f))),
            ["Augment_Ice_Aegis_Turn_End_Heal_Under_Shield"] = new([AbilityParameter.HealthRegeneration], data =>
                new AugmentIaParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    AbilityParameter.HealthRegeneration,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f))),
            ["Augment_Ice_Block_Random_Extra_Blocks"] = new([], data =>
                new DelegateAugment<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ExtraBlocksHitRandomTargets = true,
                    ability => ability.ExtraBlocksHitRandomTargets = false)),
            ["Augment_Ice_Block_Consume_Stun_Deal_Double_Damage"] = new([], data =>
                new DelegateAugment<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ConsumeStunDamageMultiplier = data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 2f),
                    ability => ability.ConsumeStunDamageMultiplier = 0f)),
            ["Augment_Deep_Freeze_Spread"] = new([], data =>
                new DelegateAugment<DeepFreeze.DeepFreeze>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.SpreadFreezeChance = data.UpgradeProperties.GetValueOrDefault("chance", 0.15f),
                    ability => ability.SpreadFreezeChance = 0f)),
            ["Augment_Deep_Freeze_Control_Extend_Effects"] = new([], data =>
                new DelegateAugment<DeepFreeze.DeepFreeze>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ExtendTargetEffects = true,
                    ability => ability.ExtendTargetEffects = false)),
            ["Augment_Reduce_All_Cooldowns"] = new([], data =>
                new AugmentActivationRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new ReduceAllCooldownsActivationRider(data.Id, (int)data.UpgradeProperties.GetValueOrDefault("amount", 2)))),
            ["Augment_Deep_Freeze_Execute_Frozen_On_Hit"] = new([], data =>
                new AugmentImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new ExecuteImpactRider(data.Id, data.UpgradeProperties.GetValueOrDefault("threshold", 0.30f)))),
            ["Augment_Discharge_Hits_Ignore_Resistances"] = new([], data =>
                new DelegateAugment<Discharge.Discharge>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.AlwaysIgnoreResistances = true,
                    ability => ability.AlwaysIgnoreResistances = false)),
            ["Augment_Discharge_Consume_Mana"] = new([], data =>
                new DelegateAugment<Discharge.Discharge>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ConsumeManaInstead = true,
                    ability => ability.ConsumeManaInstead = false)),
            ["Augment_Reduce_Execution_Threshold"] = new([AbilityParameter.ExecutionThreshold], data =>
                new AugmentReduceParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    AbilityParameter.ExecutionThreshold,
                    data.UpgradeProperties.GetValueOrDefault("share", 0.10f))),
            ["Augment_Cost_Barrier"] = new([AbilityParameter.CostType], data =>
                new AugmentCostTypeOverride(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Costs.Barrier)),
        };

        /// <summary>An effect built entirely from the canon — every figure it carries is balanced in
        /// <c>SharedData/Effects</c> and the record adds none of its own. Null without a composed registry,
        /// which the riders read as "lay nothing" rather than throwing.</summary>
        private IEffect? EffectFromCanon(string effectId) =>
            _effects()?.CreateEffect(effectId, RecordProperties.Empty);

        /// <summary>
        /// A fury variant built from the canon. The two numbers the ABILITY owns are handed over — the
        /// duration it was cast with and the share of health it burns, both of them keys an augment can
        /// move — and everything the VARIANT is about (how much of the burned health becomes damage, how
        /// long that damage lasts, what the primal multiplier is, what the healing gives back) comes from
        /// <c>SharedData/Effects</c> like every other effect's balance.
        /// <para>Without a registry composed there is nothing to read the canon from, and the plain fury
        /// is laid instead of the variant — a sandbox answer, never a shipped one.</para>
        /// </summary>
        private IEffect FuryFromCanon(string effectId, int duration, float healthPercent)
        {
            var owned = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                ["duration"] = duration,
                ["healthPercent"] = healthPercent
            };

            return _effects()?.CreateEffect(effectId, new RecordProperties(effectId, owned))
                   ?? new FuryEffect(duration, maxStacks: 1, healthPercent);
        }
    }
}
