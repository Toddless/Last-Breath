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
            ["Augment_More_Attack_Damage"] =
            [
                new(AbilityParameter.DamageMultiplier, OperationType.Add, "damageMultiplier", 0.15f)
            ],
            ["Augment_Additional_Attacks"] =
            [
                new(SeriesOfAttacks.SeriesOfAttacks.Parameters.MinAttacks, OperationType.Add, "amountAttacks", 1f),
                new(SeriesOfAttacks.SeriesOfAttacks.Parameters.MaxAttacks, OperationType.Add, "amountAttacks", 1f)
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
            ["Augment_Overload_Mana_Step"] =
            [
                new(Overload.Overload.Parameters.ManaPerStep, OperationType.Subtract, "amount", 1.5f)
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
            ["Augment_More_Retaliation"] =
            [
                new(Porcupine.Porcupine.Parameters.ArmorReturn, OperationType.Add, "amount", 0.15f)
            ],

            // Sacrifice
            ["Augment_Heal_From_Empowered_Ability_Damage"] =
            [
                new(Sacrifice.Sacrifice.Parameters.HealPercent, OperationType.Add, "amount", 0.15f)
            ],

            // Berserk Fury
            ["Augment_Fury_More_Burn"] =
            [
                new(BerserkFury.BerserkFury.Parameters.FuryHealthPercent, OperationType.Add, "amount", 0.035f)
            ],
            ["Augment_Fury_Less_Burn"] =
            [
                new(BerserkFury.BerserkFury.Parameters.FuryHealthPercent, OperationType.Subtract, "amount", 0.02f)
            ],

            // Ares Blessing
            ["Augment_Health_Bonus"] =
            [
                new(AresBlessing.AresBlessing.Parameters.HealthBonus, OperationType.Add, "healthBonus", 0.15f)
            ],

            // Double Strike
            ["Augment_Restore_Mana_Health_On_Hit"] =
            [
                new(DoubleStrike.DoubleStrike.Parameters.HealthRestore, OperationType.Add, "healthRestore", 0.07f),
                new(DoubleStrike.DoubleStrike.Parameters.ManaRestore, OperationType.Add, "manaRestore", 0.07f)
            ],

            // Head Butt
            ["Augment_Extend_Stun_Add_Cost"] =
            [
                new(AbilityParameter.StunDuration, OperationType.Add, "stunDuration", 1f),
                new(AbilityParameter.CostValue, OperationType.Add, "additionalCost", 50f)
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
            ["Augment_Reduce_Execution_Threshold"] =
            [
                new(PoisonExplosion.PoisonExplosion.Parameters.ExecutionThreshold, OperationType.Subtract, "amount", 5f)
            ],

            // Ice Shards
            ["Augment_Additional_Projectiles"] =
            [
                new(AbilityParameter.ProjectileCount, OperationType.Add, "amount", 2f)
            ],
            ["Augment_Additional_Crit_Damage"] =
            [
                new(AbilityParameter.CriticalDamageBonus, OperationType.Add, "amount", 0.75f)
            ],
            ["Augment_Additional_Crit_Chance"] =
            [
                new(AbilityParameter.CriticalChanceBonus, OperationType.Add, "amount", 0.35f)
            ],

            // Ice Blocks
            ["Augment_Stage_Four_Damage"] =
            [
                new(IceBlock.IceBlocks.Parameters.ExtraBlockDamagePercent, OperationType.Add, "amount", 0.25f)
            ],
        };
    }
}
