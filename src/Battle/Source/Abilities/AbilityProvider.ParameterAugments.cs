namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Enums;

    public partial class AbilityProvider
    {
        /// <summary>
        /// The augments that are numbers and nothing else, written as the numbers they are. Each of
        /// them used to be a factory calling a class written for that one record, and every one of
        /// those classes said the same three things in a different order — which parameter key to
        /// stand on, what to do to the value there, and where to read the amount. Said as data, the
        /// record is one line and the code behind all of them is one class
        /// (<see cref="AbilityAugmentParameterSet"/>).
        ///
        /// The table is code and not part of the augment's json record, because the record is a
        /// contract shared with everything that reads augments and it is not the place to say how the
        /// game implements one. What the record keeps is the number itself, under the property named
        /// here — the same number the augment's description prints, so the text and the effect can
        /// never drift apart. The figure written beside the property is what the augment falls back to
        /// when the record does not carry it: a lost property is worth zero and says nothing about it,
        /// so the fallback is spelled out rather than left to the reader.
        ///
        /// An augment reaching into the members of one ability class is not here — it stays a factory
        /// of its own in <c>AbilityProvider.Upgrades.cs</c>, and so do the augments whose amount is a
        /// share of the number they move rather than a figure of their own.
        ///
        /// The headings below say which ability a record was WRITTEN for, and that is all they say. A
        /// row standing on a key of <see cref="AbilityParameter"/> works on every ability that declared
        /// the concept, and a row standing on one ability's own key works on that ability alone — which
        /// of the two a record is is read off the key it names and nowhere else.
        /// </summary>
        private readonly Dictionary<string, AugmentParameterMove[]> _parameterAugments = new()
        {
            // Series of Attacks
            ["Augment_Additional_Max_Attacks"] =
            [
                new(SeriesOfAttacks.SeriesOfAttacks.Parameters.MaxAttacks, OperationType.Add, "maxAdditionalAttacks", 3f)
            ],
            ["Augment_More_Attack_Damage"] =
            [
                new(AbilityParameter.DamageMultiplier, OperationType.Add, "damageMultiplier", 0.15f)
            ],
            ["Augment_Additional_Attacks"] =
            [
                new(SeriesOfAttacks.SeriesOfAttacks.Parameters.MinAttacks, OperationType.Add, "amountAttacks", 1f),
                new(SeriesOfAttacks.SeriesOfAttacks.Parameters.MaxAttacks, OperationType.Add, "amountAttacks", 1f)
            ],

            // Increasing Pressure
            ["Augment_Additional_Amount_Attacks"] =
            [
                new(AbilityParameter.Attacks, OperationType.Add, "amountAttacks", 2f)
            ],
            ["Augment_Additional_Damage_Multiplier"] =
            [
                new(IncreasingPressure.IncreasingPressure.Parameters.AttackDamageStepMultiplier, OperationType.Add, "damageMultiplier", 0.05f)
            ],

            // Jar of Poison
            ["Augment_Increasing_Scales"] =
            [
                new(AbilityParameter.WeaponDamageScale, OperationType.Add, "weaponDamageScale", 0.6f),
                new(AbilityParameter.SpellDamageScale, OperationType.Add, "spellDamageScale", 0.85f)
            ],
            ["Augment_Poison_Duration"] =
            [
                new(AbilityParameter.PoisonDuration, OperationType.Add, "poisonDuration", 1f)
            ],

            // Overload
            ["Ability_Ov_Augment_Mana_Step"] =
            [
                new(Overload.Overload.Parameters.ManaPerStep, OperationType.Subtract, "amount", 1.5f)
            ],
            ["Ability_Ov_Augment_Additional_Multiplier"] =
            [
                new(Overload.Overload.Parameters.DamagePerStep, OperationType.Add, "amount", 0.02f)
            ],

            // Porcupine
            ["Augment_Cooldown_Chance"] =
            [
                new(Porcupine.Porcupine.Parameters.CooldownReduceChance, OperationType.Add, "chance", 0.15f)
            ],
            ["Augment_Heal_On_Hit"] =
            [
                new(Porcupine.Porcupine.Parameters.HealOnHit, OperationType.Add, "amount", 0.07f)
            ],
            ["Augment_More_Armor_Return"] =
            [
                new(Porcupine.Porcupine.Parameters.ArmorReturn, OperationType.Add, "amount", 0.15f)
            ],
            ["Augment_More_Damage_Return"] =
            [
                new(Porcupine.Porcupine.Parameters.DamageReturn, OperationType.Add, "amount", 0.20f)
            ],

            // Sacrifice
            ["Augment_Heal_From_Damage"] =
            [
                new(Sacrifice.Sacrifice.Parameters.HealPercent, OperationType.Add, "amount", 0.15f)
            ],

            // Berserk Fury
            ["Augment_Fury_Duration"] =
            [
                new(AbilityParameter.Duration, OperationType.Subtract, "duration", 1f)
            ],
            ["Augment_More_Burn"] =
            [
                new(BerserkFury.BerserkFury.Parameters.FuryHealthPercent, OperationType.Add, "amount", 0.035f)
            ],
            ["Augment_Less_Burn"] =
            [
                new(BerserkFury.BerserkFury.Parameters.FuryHealthPercent, OperationType.Subtract, "amount", 0.02f)
            ],

            // Ares Blessing
            ["Augment_Buff_Duration"] =
            [
                new(AbilityParameter.Duration, OperationType.Add, "duration", 1f)
            ],
            ["Augment_Recovery_Bonus"] =
            [
                new(AresBlessing.AresBlessing.Parameters.RecoveryBonus, OperationType.Add, "recoveryBonus", 0.15f)
            ],
            ["Augment_Health_Bonus"] =
            [
                new(AresBlessing.AresBlessing.Parameters.HealthBonus, OperationType.Add, "healthBonus", 0.15f)
            ],

            // Double Strike
            ["Augment_Damage_Multiplier"] =
            [
                new(AbilityParameter.DamageMultiplier, OperationType.Add, "amount", 0.25f)
            ],
            ["Augment_Restore_On_Hit"] =
            [
                new(DoubleStrike.DoubleStrike.Parameters.HealthRestore, OperationType.Add, "healthRestore", 0.07f),
                new(DoubleStrike.DoubleStrike.Parameters.ManaRestore, OperationType.Add, "manaRestore", 0.07f)
            ],

            // Head Butt
            ["Ability_Hb_Augment_Additional_Scales"] =
            [
                new(AbilityParameter.WeaponDamageScale, OperationType.Add, "weaponDamageScale", 0.15f),
                new(AbilityParameter.SpellDamageScale, OperationType.Add, "spellDamageScale", 0.15f)
            ],
            ["Augment_Extend_Stun_Add_Cost"] =
            [
                new(AbilityParameter.StunDuration, OperationType.Add, "stunDuration", 1f),
                new(AbilityParameter.CostValue, OperationType.Add, "additionalCost", 50f)
            ],
            ["Augment_Additional_Lunges"] =
            [
                new(AbilityParameter.Attacks, OperationType.Add, "amount", 1f)
            ],

            // Critical Calculation
            ["Augment_More_Stacks_More_Cost"] =
            [
                new(AbilityParameter.Stacks, OperationType.Add, "stacks", 1f),
                new(AbilityParameter.CostValue, OperationType.Add, "cost", 50f)
            ],

            // Dark Shroud
            ["Augment_Additional_Health_Regen"] =
            [
                new(DarkShroud.DarkShroud.Parameters.HealthRegen, OperationType.Add, "additionalRegen", 0.025f)
            ],
            ["Augment_Add_Effectiveness_Reduce_Stacks"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "additionalEffectiveness", 0.35f),
                new(AbilityParameter.Stacks, OperationType.Subtract, "amountStacks", 2f)
            ],
            ["Augment_Increased_Buff_Duration"] =
            [
                new(AbilityParameter.Duration, OperationType.Add, "duration", 1f)
            ],

            // The effectiveness family. One key and one direction, so any two of these are one offer at
            // two strengths — what tells them apart is the tags, which decide WHICH abilities are
            // offered the deal at all. That is the whole segmentation: the kind of thing an ability
            // applies is a fact about the ability, and a key that tried to carry it would need one per
            // kind and an owner able to answer "which of my three".
            ["Augment_Buff_Effectiveness"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "effectiveness", 0.25f)
            ],
            ["Augment_Recovery_Effectiveness"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "effectiveness", 0.35f)
            ],
            ["Augment_Debuff_Effectiveness"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "effectiveness", 0.25f)
            ],

            // Poison Explosion
            ["Augment_Reduce_Execution_Trahsold"] =
            [
                new(PoisonExplosion.PoisonExplosion.Parameters.ExecutionThreshold, OperationType.Subtract, "amount", 5f)
            ],

            // Ice Shards
            ["Augment_Additional_Projectiles"] =
            [
                new(AbilityParameter.ProjectileCount, OperationType.Add, "amount", 2f)
            ],
            ["Ability_Is_Augment_Additional_Scales"] =
            [
                new(AbilityParameter.WeaponDamageScale, OperationType.Add, "weaponDamageScale", 0.05f),
                new(AbilityParameter.SpellDamageScale, OperationType.Add, "spellDamageScale", 0.15f)
            ],
            ["Ability_Is_Augment_Additional_Crit_Damage"] =
            [
                new(AbilityParameter.CriticalDamageBonus, OperationType.Add, "amount", 0.75f)
            ],
            ["Ability_Is_Augment_Additional_Crit_Chance"] =
            [
                new(AbilityParameter.CriticalChanceBonus, OperationType.Add, "amount", 0.35f)
            ],

            // Ice Blocks
            ["Ability_Ib_Augment_Withering_Value"] =
            [
                new(IceBlock.IceBlocks.Parameters.WitheringValue, OperationType.Add, "amount", 0.05f)
            ],
            ["Ability_Ib_Augment_Withering_Stacks"] =
            [
                new(AbilityParameter.Stacks, OperationType.Add, "amount", 1f)
            ],
            ["Ability_Ib_Augment_Extra_Block_Damage"] =
            [
                new(IceBlock.IceBlocks.Parameters.ExtraBlockDamagePercent, OperationType.Add, "amount", 0.25f)
            ],
            ["Ability_Ib_Augment_Heavy_Blocks"] =
            [
                new(AbilityParameter.Damage, OperationType.Add, "damage", 150f),
                new(AbilityParameter.WeaponDamageScale, OperationType.Add, "weaponDamageScale", 0.15f),
                new(AbilityParameter.SpellDamageScale, OperationType.Add, "spellDamageScale", 0.45f)
            ],

            // Deep Freeze
            ["Ability_Df_Augment_Frostbite_Duration"] =
            [
                new(DeepFreeze.DeepFreeze.Parameters.FrostbiteDuration, OperationType.Add, "amount", 1f)
            ],
            ["Ability_Df_Augment_More_Shred"] =
            [
                new(DeepFreeze.DeepFreeze.Parameters.ColdResistanceShred, OperationType.Add, "amount", 0.15f)
            ],

            // Discharge
            ["Ability_Dis_Augment_Multiplier"] =
            [
                new(Discharge.Discharge.Parameters.BarrierMultiplier, OperationType.Add, "amount", 0.5f)
            ],
            ["Ability_Dis_Augment_More_Multiplier"] =
            [
                new(Discharge.Discharge.Parameters.BarrierMultiplier, OperationType.Add, "amount", 1f)
            ],
            ["Ability_Dis_Augment_More_Restore"] =
            [
                new(Discharge.Discharge.Parameters.StageThreeBarrierRestore, OperationType.Add, "amount", 0.25f)
            ],
            ["Ability_Dis_Augment_Spell_Scale"] =
            [
                new(AbilityParameter.SpellDamageScale, OperationType.Add, "amount", 0.35f)
            ],

            // Static Armor
            ["Ability_Sa_Augment_Detonation_Scales"] =
            [
                new(StaticArmor.StaticArmor.Parameters.DetonationWeaponScale, OperationType.Add, "weaponDamageScale", 0.25f),
                new(StaticArmor.StaticArmor.Parameters.DetonationSpellScale, OperationType.Add, "spellDamageScale", 0.35f)
            ],
            ["Ability_Sa_Augment_Buff_Duration"] =
            [
                new(AbilityParameter.Duration, OperationType.Add, "amount", 1f)
            ],
            ["Ability_Sa_Augment_More_Splash"] =
            [
                new(StaticArmor.StaticArmor.Parameters.StageThreeSplashDamage, OperationType.Add, "amount", 0.5f)
            ],
            ["Ability_Sa_Augment_Less_Stacks"] =
            [
                new(StaticArmor.StaticArmor.Parameters.RequiredStacks, OperationType.Subtract, "amount", 1f)
            ]
        };
    }
}
