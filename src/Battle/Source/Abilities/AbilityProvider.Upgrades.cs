namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Data.AbilityData;
    using Core.Enums;
    using CriticalCalculation;
    using DarkShroud;
    using AresBlessing;
    using Armageddon;
    using BerserkFury;
    using DoubleStrike;
    using Effects;
    using IncreasingPressure;
    using PoisonCoating;
    using PoisonExplosion;
    using PassiveSkills;
    using Riders;
    using SeriesOfAttacks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;
    using IceAegis;
    using IceBlock;
    using IceShards;

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
                    data.UpgradeProperties.GetValueOrDefault("cooldownShare", 0.25f)),
            ["Augment_Reduce_Cooldown_Add_Cost"] = data =>
                new AbilityAugmentReduceCooldownAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownShare", 0.4f),
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.4f)),
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
            ["Augment_Poison_On_Hit"] = data =>
                new AugmentPoisonOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("poisonDuration", 3f),
                    data.UpgradeProperties.GetValueOrDefault("poisonPotency", 0.7f)),
            ["Augment_Apply_Buff_Critical_Chance"] = data =>
                new SoAsAugmentApplyBuffCriticalChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 6),
                    data.UpgradeProperties.GetValueOrDefault("criticalChance", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3f)),
            ["Augment_Apply_Buff_Critical_Damage"] = data =>
                new SoAsAugmentApplyBuffCriticalDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 9),
                    data.UpgradeProperties.GetValueOrDefault("criticalDamage", 0.25f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3)),
            ["Ability_SoA_Augment_Attacks_Cannot_Be_Evaded"] = data =>
                new SoAsAugmentUnevadable(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Ip_Augment_Single_Empowered_Attack"] = data =>
                new IpAugmentSingleEmpoweredAttack(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Attack_Random_Target"] = data =>
                new IpAugmentAttackRandomTarget(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("splashDamage", 0.45f)),
            ["Ability_Ip_Augment_Last_Attack_Always_Crit"] = data =>
                new IpAugmentLastAttackAlwaysCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new LastAttackAlwaysCritContextModifier()),
            ["Ability_Ip_Augment_First_Attack_Crit_Damage"] = data =>
                new IpAugmentFirstAttackCritDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new FirstAttackCritContextModifier(data.UpgradeProperties.GetValueOrDefault("critDamageBonus", 1.3f))),
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
            ["Augment_Stage_Four_Resets_Cooldown"] = data =>
                new DelegateAugment<Overload.Overload>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ResetCooldownOnFinalStage = true,
                    ability => ability.ResetCooldownOnFinalStage = false),
            ["Ability_Cl_Augment_Ignore_Resistances"] = data =>
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
            ["Augment_Burning_Fury"] = data =>
                new BfAugmentFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon("Effect_Burning_Fury", duration, healthPercent)),
            ["Augment_Primal_Fury"] = data =>
                new BfAugmentFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon("Effect_Primal_Fury", duration, healthPercent)),
            ["Augment_Healing_Fury"] = data =>
                new BfAugmentFuryVariant(data.Id, data.Tags, data.Tier,
                    (duration, healthPercent) => FuryFromCanon("Effect_Healing_Fury", duration, healthPercent)),
            ["Augment_Accuracy"] = data =>
                new DstAugmentAccuracy(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_Two_Attacks_Apply_Buff"] = data =>
                new DstAugmentBothHitsBuff(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.25f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Augment_Lucky_Crit"] = data =>
                new CcAugmentLuckyCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Augment_Apply_Enhanced_Defence"] = data =>
                new CcAugmentApplyEnhancedDefence(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 3),
                    data.UpgradeProperties.GetValueOrDefault("value", 0.15f)),
            ["Augment_Leach_On_Crit"] = data =>
                new CcAugmentLeachOnCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Augment_Immortality"] = data =>
                new DsAugmentImmortality(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("lifeToRecover", 0.35f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 1)),
            ["Ability_Pe_Augment_Execute_Bosses"] = data =>
                new PeAugmentExecuteBosses(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("stacksMultiplier", 2)),
            ["Ability_Pe_Augment_Spread_Poison"] = data =>
                new PeAugmentSpreadPoison(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Transfer_Poison_On_Death"] = data =>
                new PeAugmentTransferPoisonOnDeath(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Attacks_Reduce_Incoming_Heal"] = data =>
                new PcAugmentApplyDebuffOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new HealReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 1),
                        data.UpgradeProperties.GetValueOrDefault("reduceBy", 0.6f))),
            ["Augment_Attacks_Reduce_Armor"] = data =>
                new PcAugmentApplyDebuffOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new ArmorReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 4),
                        data.UpgradeProperties.GetValueOrDefault("reduceArmorBy", 0.15f))),
            ["Augment_Apply_Poison_For_Each_Enemy"] = data =>
                new PcAugmentMultiStackOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Multicast"] = data =>
                new DelegateAugment<IceShards.IceShards>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.Activation.BonusChance += data.UpgradeProperties.GetValueOrDefault("amount", 0.25f),
                    ability => ability.Activation.BonusChance -= data.UpgradeProperties.GetValueOrDefault("amount", 0.25f)),
            ["Augment_Critical_Hit_Ignores_Cold_Res"] = data =>
                new DelegateAugment<IceShards.IceShards>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.CritIgnoresColdResistance = true,
                    ability => ability.CritIgnoresColdResistance = false),
            ["Augment_Shield_Reflect_Damage"] = data =>
                new IaAugmentParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.ReflectPercent,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_Turn_End_Heal_Under_Shield"] = data =>
                new IaAugmentParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    AbilityParameter.HealthRegeneration,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_Reset_Chance"] = data =>
                new DelegateAugment<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ResetCooldownChance = data.UpgradeProperties.GetValueOrDefault("chance", 0.25f),
                    ability => ability.ResetCooldownChance = 0f),
            ["Augment_Ice_Blocks_Random_Extra_Blocks"] = data =>
                new DelegateAugment<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ExtraBlocksHitRandomTargets = true,
                    ability => ability.ExtraBlocksHitRandomTargets = false),
            ["Augment_Ice_Blocks_Consume_Stun_Deal_Double_Damage"] = data =>
                new DelegateAugment<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ConsumeStunForDoubleDamage = true,
                    ability => ability.ConsumeStunForDoubleDamage = false),
            ["Augment_Spread_Freeze"] = data =>
                new DelegateAugment<DeepFreeze.DeepFreeze>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.SpreadFreezeChance = data.UpgradeProperties.GetValueOrDefault("chance", 0.5f),
                    ability => ability.SpreadFreezeChance = 0f),
            ["Augment_Control_Extend_Effects"] = data =>
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
            ["Augment_Freezed_Target_Execute_On_Hit"] = data =>
                new AbilityAugmentImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ExecuteImpactRider(data.Id, data.UpgradeProperties.GetValueOrDefault("threshold", 0.30f))),
            ["Augment_Hits_Ignore_Resistances"] = data =>
                new DelegateAugment<Discharge.Discharge>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.AlwaysIgnoreResistances = true,
                    ability => ability.AlwaysIgnoreResistances = false),
            ["Augment_Consume_Mana"] = data =>
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
