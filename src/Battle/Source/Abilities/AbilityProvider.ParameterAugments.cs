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
                new(AbilityParameter.DamageMultiplier, OperationType.Add, "damageMultiplier", 0.10f)
            ],
            ["Augment_Additional_Attacks"] =
            [
                new(SeriesOfAttacks.SeriesOfAttacks.Parameters.MinAttacks, OperationType.Add, "amountAttacks", 1f),
                new(SeriesOfAttacks.SeriesOfAttacks.Parameters.MaxAttacks, OperationType.Add, "amountAttacks", 1f)
            ],

            // Jar of Poison
            ["Augment_Increasing_Scales"] =
            [
                new(AbilityParameter.WeaponDamageScale, OperationType.Add, "weaponDamageScale", 0.05f),
                new(AbilityParameter.SpellDamageScale, OperationType.Add, "spellDamageScale", 0.10f)
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
                new(AbilityParameter.CooldownReductionChance, OperationType.Add, "chance", 0.05f)
            ],
            ["Augment_Heal_On_Hit"] =
            [
                new(Porcupine.Porcupine.Parameters.HealOnHit, OperationType.Add, "amount", 0.03f)
            ],
            ["Augment_More_Retaliation"] =
            [
                new(Porcupine.Porcupine.Parameters.ArmorReturn, OperationType.Add, "amount", 0.15f)
            ],

            // Sacrifice
            ["Augment_Heal_From_Empowered_Ability_Damage"] =
            [
                new(AbilityParameter.HealFromEmpoweredDamage, OperationType.Add, "amount", 0.35f)
            ],

            // Berserk Fury
            ["Augment_Fury_More_Burn"] =
            [
                new(BerserkFury.BerserkFury.Parameters.FuryHealthPercent, OperationType.Add, "amount", 0.01f)
            ],
            ["Augment_Fury_Less_Burn"] =
            [
                new(BerserkFury.BerserkFury.Parameters.FuryHealthPercent, OperationType.Subtract, "amount", 0.01f)
            ],

            // Ares Blessing
            ["Augment_Health_Bonus"] =
            [
                new(AbilityParameter.HealthBonus, OperationType.Add, "healthBonus", 0.05f)
            ],

            // Double Strike
            ["Augment_Restore_Mana_Health_On_Hit"] =
            [
                new(AbilityParameter.HealthRestore, OperationType.Add, "healthRestore", 0.07f),
                new(AbilityParameter.ManaRestore, OperationType.Add, "manaRestore", 0.07f)
            ],
            ["Augment_Accuracy"] =
            [
                new(AbilityParameter.AccuracyBonus, OperationType.Add, "amount", 0.15f)
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
                new(AbilityParameter.HealthRegeneration, OperationType.Add, "additionalRegen", 0.01f)
            ],
            ["Augment_Add_Effectiveness_Reduce_Stacks"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "additionalEffectiveness", 0.10f),
                new(AbilityParameter.Stacks, OperationType.Subtract, "amountStacks", 2f)
            ],

            // The effectiveness family. One key and one direction, so any two of these are one offer at
            // two strengths — what tells them apart is the tags, which decide WHICH abilities are
            // offered the deal at all. That is the whole segmentation: the kind of thing an ability
            // applies is a fact about the ability, and a key that tried to carry it would need one per
            // kind and an owner able to answer "which of my three".
            ["Augment_Buff_Effectiveness"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "effectiveness", 0.15f)
            ],
            ["Augment_Recovery_Effectiveness"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "effectiveness", 0.10f)
            ],
            ["Augment_Debuff_Effectiveness"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "effectiveness", 0.15f)
            ],
            // The umbrella over the three above: one key, one direction, so where it meets any of them
            // they are one offer and the stronger works. What separates it is only the tag it rides —
            // 'effect', worn by every ability that lays anything at all.
            ["Augment_Applied_Effectiveness"] =
            [
                new(AbilityParameter.Effectiveness, OperationType.Add, "effectiveness", 0.10f)
            ],

            // Poison Explosion
            // Ice Shards
            ["Augment_Additional_Projectiles"] =
            [
                new(AbilityParameter.ProjectileCount, OperationType.Add, "amount", 1f)
            ],
            ["Augment_Additional_Crit_Damage"] =
            [
                new(AbilityParameter.CriticalDamageBonus, OperationType.Add, "amount", 0.35f)
            ],
            ["Augment_Additional_Crit_Chance"] =
            [
                new(AbilityParameter.CriticalChanceBonus, OperationType.Add, "amount", 0.25f)
            ],

            // Ice Blocks
            ["Augment_Stage_Four_Damage"] =
            [
                new(AbilityParameter.StageFourDamage, OperationType.Add, "amount", 0.10f)
            ],
            ["Augment_Reset_Chance"] =
            [
                new(AbilityParameter.CooldownResetChance, OperationType.Add, "chance", 0.35f)
            ],

            // Increasing Pressure
            ["Augment_Attack_Random_Target"] =
            [
                new(AbilityParameter.SplashShare, OperationType.Add, "splashDamage", 0.75f)
            ],

            // The base contract, tag-judged rather than universal: the design list gives each of these a
            // tag of its own (scale / cooldown / stacks / empowered / consume), so they reach the family
            // that owns the concept instead of the whole book. Only the flat halves live here — a record
            // stating part of itself as a share of the ability's own price is a factory (Upgrades.cs).
            ["Augment_Weapon_Scale"] =
            [
                new(AbilityParameter.WeaponDamageScale, OperationType.Add, "weaponDamageScale", 0.10f)
            ],
            ["Augment_Spell_Scale"] =
            [
                new(AbilityParameter.SpellDamageScale, OperationType.Add, "spellDamageScale", 0.15f)
            ],
            ["Augment_Increasing_Scales_Add_Cooldown"] =
            [
                new(AbilityParameter.WeaponDamageScale, OperationType.Add, "weaponDamageScale", 0.25f),
                new(AbilityParameter.SpellDamageScale, OperationType.Add, "spellDamageScale", 0.25f),
                new(AbilityParameter.Cooldown, OperationType.Add, "cooldownTurns", 1f)
            ],
            ["Augment_Additional_Stacks"] =
            [
                new(AbilityParameter.Stacks, OperationType.Add, "amount", 1f)
            ],
            ["Augment_Additional_Charges"] =
            [
                new(AbilityParameter.Charges, OperationType.Add, "amount", 1f)
            ],
            ["Augment_Consume_Effectiveness"] =
            [
                new(AbilityParameter.ConsumeEffectiveness, OperationType.Add, "amount", 0.05f)
            ],
        };
    }
}
