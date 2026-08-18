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
        /// <summary>
        /// The augments that need code — a behaviour to install, a strategy to swap, a member of one
        /// ability class to reach for, or a number measured as a share of the one it moves. An augment
        /// that only moves numbers is not written here: it is a row of the parameter table
        /// (<c>AbilityProvider.ParameterAugments.cs</c>) and shares its one class with all the others.
        /// The two halves never name the same augment.
        /// <para>Built on first use rather than in a field initializer: three of the entries reach the
        /// effect registry for their canonical numbers, and a field initializer may not touch the
        /// instance that holds it.</para>
        /// </summary>
        private Dictionary<string, Func<AbilityAugmentData, IAbilityAugment>> AbilityUpgrades => field ??= new()
        {
            // Augments of the base contract every ability honours. They belong to no ability, so they
            // are written once — and those that move a number state it as a share of the number they
            // move, the only figure that means the same thing on a free cast and on a five-hundred one.
            ["Augment_Reduce_Cost"] = data =>
                new AbilityAugmentReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.3f)),
            ["Augment_Reduce_Cooldown"] = data =>
                new AbilityAugmentReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownTurns", 1f)),
            ["Augment_Reduce_Cooldown_Add_Cost"] = data =>
                new AbilityAugmentReduceCooldownAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownTurns", 2f),
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.15f)),
            // The three records whose design line mixes the shapes: whole turns or scale points of their
            // own on one key, a share of the ability's own price on the other. The share is what keeps
            // them out of the parameter table.
            ["Augment_Reduce_Cooldown_And_Cost"] = data =>
                new AbilityAugmentReduceCooldownAndCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownTurns", 1f),
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.10f)),
            ["Augment_Reduce_Cost_Add_Cooldown"] = data =>
                new AbilityAugmentReduceCostAddCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.25f),
                    data.UpgradeProperties.GetValueOrDefault("cooldownTurns", 1f)),
            ["Augment_Increasing_Scales_Add_Cost"] = data =>
                new AbilityAugmentRaiseScalesAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.25f),
                    data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.25f),
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.15f)),
            // The only record that moves a FAMILY of keys rather than a named one — which is why it is a
            // factory and not a table row: the table says which key a record stands on, and this one does
            // not know until it is seated.
            ["Augment_Applied_Duration"] = data =>
                new AbilityAugmentAppliedDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("turns", 1f)),
            ["Augment_Cost_Type_Health"] = data =>
                new AbilityAugmentCostTypeOverride(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Costs.Health),
            // Belongs to no ability either, though it is not the base contract it works through but the
            // delivery one: whoever lands a hit extends the poison on what he hit.
            ["Augment_Extend_Poison"] = data =>
                new AugmentExtendPoison(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1)),
            // Nor does this one: whatever lands the impact, the target is poisoned. Both of its numbers
            // become parameters of the ability it is seated on, which is what lets an amplifier of
            // poison reach the stacks it lays.
            ["Augment_Poison_Attack_Series"] = data =>
                new AugmentPoisonOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("poisonDuration", 3f),
                    data.UpgradeProperties.GetValueOrDefault("poisonPotency", 0.7f)),
            // Both tally the caster's successful attacks, which every attacking ability has — hence the
            // generic impact rider rather than the Series of Attacks execution strategy they used to be.
            ["Augment_Apply_Buff_Critical_Chance"] = data =>
                new AbilityAugmentImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new BuffAfterAttacksImpactRider(
                        data.Id,
                        (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 6),
                        new CriticalChanceBuffEffect(
                            (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                            (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3),
                            data.UpgradeProperties.GetValueOrDefault("criticalChance", 0.15f)))),
            ["Augment_Apply_Buff_Critical_Damage"] = data =>
                new AbilityAugmentImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new BuffAfterAttacksImpactRider(
                        data.Id,
                        (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 9),
                        new CriticalDamageBuffEffect(
                            (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                            (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3),
                            data.UpgradeProperties.GetValueOrDefault("criticalDamage", 0.25f)))),
            ["Augment_Increasing_Pressure_Single_Empowered_Attack"] = data =>
                new IpAugmentSingleEmpoweredAttack(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Increasing_Pressure_Last_Attack_Always_Crit"] = data =>
                new IpAugmentLastAttackAlwaysCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new LastAttackAlwaysCritContextModifier()),
            // The modifier already asks the context whether the attack is the first one, so the record
            // needs the generic attack-modifier upgrade rather than Increasing Pressure's own.
            ["Augment_First_Attack_Crit_Damage"] = data =>
                new AbilityAugmentAttackModifier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new FirstAttackCritContextModifier(data.UpgradeProperties.GetValueOrDefault("critDamageBonus", 0.70f))),
            ["Augment_Random_Cooldown"] = data =>
                new AbilityAugmentActivationRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ReduceRandomCooldownActivationRider(data.Id, (int)data.UpgradeProperties.GetValueOrDefault("amount", 1))),
            ["Augment_Next_Cast_Pure"] = data =>
                new AbilityAugmentCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new NextCastPureConversionEffect(ability.Id,
                        data.UpgradeProperties.GetValueOrDefault("fraction", 0.3f))),
            ["Augment_Overload_Stage_Four_Resets_Cooldown"] = data =>
                new DelegateAugment<Overload.Overload>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ResetCooldownOnFinalStage = true,
                    ability => ability.ResetCooldownOnFinalStage = false),
            ["Augment_Chain_Lightning_Ignore_Resistances"] = data =>
                new DelegateAugment<ChainLightning.ChainLightning>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.IgnoreResistances = true,
                    ability => ability.IgnoreResistances = false),
            ["Augment_Armageddon_Burning"] = data =>
                new ArmAugmentStage3Burning(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.7f)),
            ["Augment_Armageddon_All_Targets"] = data =>
                new ArmAugmentAllTargets(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("additionalCooldown", 3)),
            ["Augment_Porcupine_Echo"] = data =>
                new AbilityAugmentCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new TemporarySkillEffect("Effect_Echo", ((Porcupine.Porcupine)ability).Duration,
                        new EchoPassiveSkill(
                            data.UpgradeProperties.GetValueOrDefault("delayedPercent", 0.3f),
                            (int)data.UpgradeProperties.GetValueOrDefault("turns", 2)))),
            ["Augment_Empowered_Ability_Free_Cast"] = data =>
                new AbilityAugmentCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new FreeCastEffect(ability.Id)),
            ["Augment_Berserk_Fury_Burning"] = data =>
                new BfAugmentFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon("Effect_Burning_Fury", duration, healthPercent)),
            ["Augment_Berserk_Fury_Primal"] = data =>
                new BfAugmentFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon("Effect_Primal_Fury", duration, healthPercent)),
            ["Augment_Berserk_Fury_Healing"] = data =>
                new BfAugmentFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon("Effect_Healing_Fury", duration, healthPercent)),
            ["Augment_Double_Strike_Two_Attacks_Apply_Buff"] = data =>
                new DstAugmentBothHitsBuff(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.25f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Augment_Critical_Calculation_Lucky_Crit"] = data =>
                new CcAugmentLuckyCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            // A buff laid on the caster after the cast is what an activation rider is; no member of
            // Critical Calculation was ever touched, only the rider dictionary every ability has.
            ["Augment_Leach_On_Crit"] = data =>
                new AbilityAugmentActivationRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new AbilityBuffActivationRider(new CritLeechEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)))),
            ["Augment_Dark_Shroud_Immortality"] = data =>
                new DsAugmentImmortality(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => EffectFromCanon("Effect_Evade_First_Death")),
            ["Augment_Poison_Explosion_Execute_Bosses"] = data =>
                new PeAugmentExecuteBosses(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("stacksMultiplier", 2)),
            ["Augment_Poison_Explosion_Spread_Poison"] = data =>
                new PeAugmentSpreadPoison(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Poison_Explosion_Transfer_Poison_On_Death"] = data =>
                new PeAugmentTransferPoisonOnDeath(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Poison_Coating_Apply_Poison_For_Each_Enemy"] = data =>
                new PcAugmentMultiStackOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Ice_Shards_Multicast"] = data =>
                new DelegateAugment<IceShards.IceShards>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.Activation.BonusChance += data.UpgradeProperties.GetValueOrDefault("amount", 0.25f),
                    ability => ability.Activation.BonusChance -= data.UpgradeProperties.GetValueOrDefault("amount", 0.25f)),
            ["Augment_Ice_Shards_Critical_Hit_Ignores_Cold_Res"] = data =>
                new DelegateAugment<IceShards.IceShards>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.CritIgnoresColdResistance = true,
                    ability => ability.CritIgnoresColdResistance = false),
            ["Augment_Ice_Aegis_Shield_Reflect_Damage"] = data =>
                new IaAugmentParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.ReflectPercent,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_Ice_Aegis_Turn_End_Heal_Under_Shield"] = data =>
                new IaAugmentParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    AbilityParameter.HealthRegeneration,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_Ice_Block_Random_Extra_Blocks"] = data =>
                new DelegateAugment<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ExtraBlocksHitRandomTargets = true,
                    ability => ability.ExtraBlocksHitRandomTargets = false),
            ["Augment_Ice_Block_Consume_Stun_Deal_Double_Damage"] = data =>
                new DelegateAugment<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ConsumeStunForDoubleDamage = true,
                    ability => ability.ConsumeStunForDoubleDamage = false),
            ["Augment_Deep_Freeze_Spread"] = data =>
                new DelegateAugment<DeepFreeze.DeepFreeze>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.SpreadFreezeChance = data.UpgradeProperties.GetValueOrDefault("chance", 0.5f),
                    ability => ability.SpreadFreezeChance = 0f),
            ["Augment_Deep_Freeze_Control_Extend_Effects"] = data =>
                new DelegateAugment<DeepFreeze.DeepFreeze>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ExtendTargetEffects = true,
                    ability => ability.ExtendTargetEffects = false),
            ["Augment_Reduce_All_Cooldowns"] = data =>
                new AbilityAugmentActivationRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ReduceAllCooldownsActivationRider(data.Id, (int)data.UpgradeProperties.GetValueOrDefault("amount", 2))),
            ["Augment_Deep_Freeze_Execute_Frozen_On_Hit"] = data =>
                new AbilityAugmentImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ExecuteImpactRider(data.Id, data.UpgradeProperties.GetValueOrDefault("threshold", 0.30f))),
            ["Augment_Discharge_Hits_Ignore_Resistances"] = data =>
                new DelegateAugment<Discharge.Discharge>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.AlwaysIgnoreResistances = true,
                    ability => ability.AlwaysIgnoreResistances = false),
            ["Augment_Discharge_Consume_Mana"] = data =>
                new DelegateAugment<Discharge.Discharge>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ConsumeManaInstead = true,
                    ability => ability.ConsumeManaInstead = false),
            ["Augment_Reduce_Execution_Threshold"] = data =>
                new AbilityAugmentReduceParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    AbilityParameter.ExecutionThreshold,
                    data.UpgradeProperties.GetValueOrDefault("share", 0.25f)),
            ["Augment_Cost_Barrier"] = data =>
                new AbilityAugmentCostTypeOverride(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Costs.Barrier),
        };

        /// <summary>
        /// A fury variant built from the canon. The two numbers the ABILITY owns are handed over — the
        /// duration it was cast with and the share of health it burns, both of them keys an augment can
        /// move — and everything the VARIANT is about (how much of the burned health becomes damage, how
        /// long that damage lasts, what the primal multiplier is, what the healing gives back) comes from
        /// <c>SharedData/Effects</c> like every other effect's balance. These three were the last records
        /// carrying effect figures of their own: typed factories, so the gate CL-3b put on data-declared
        /// behaviours never saw them, and their fallbacks had drifted a wave behind the design list.
        /// <para>Without a registry composed there is nothing to read the canon from, and the plain fury
        /// is laid instead of the variant — a sandbox answer, never a shipped one.</para>
        /// </summary>
        /// <summary>An effect built entirely from the canon — every figure it carries is balanced in
        /// <c>SharedData/Effects</c> and the record adds none of its own. Null without a composed registry,
        /// which the riders read as "lay nothing" rather than throwing.</summary>
        private IEffect? EffectFromCanon(string effectId) =>
            _effects()?.CreateEffect(effectId, RecordProperties.Empty);

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
