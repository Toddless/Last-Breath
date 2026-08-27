namespace Core.Battle.Skills
{
    using System;
    using System.Collections.Generic;
    using Localization;

    /// <summary>
    /// What a named passive's rule text is filled in with, read off the very numbers a node (or an item
    /// record) is tuned by. The template asks for values by NAME — <c>{PoisonDuration|turn|turns}</c> — and
    /// the record writes FIELDS — <c>"duration": 3</c>; the two are not the same word, and some names are
    /// not a field at all but a figure derived from one (a price stated as the magnitude the sentence
    /// around it already gives a direction to). This is the one place that knows both.
    ///
    /// <para>Why it is here and not battle-side: the passive classes live in a project the authoring tool
    /// cannot reach, so a tool that wanted to show a keystone's sentence had only the id and a bag of
    /// numbers. The rules being in Core, the wheel's popup, the passive's own card and the tree editor's
    /// tooltip all reach the same sentence — and the DERIVED figures (a cut written as the magnitude of a
    /// penalty) exist exactly once, here, with the classes that state them calling in rather than keeping a
    /// second copy of the arithmetic.</para>
    ///
    /// <para>A passive missing from the table hands over no values: its template names none (a keystone
    /// whose whole content is a rule), or it is the stat family, which has no hand-written text at all and
    /// words itself through <see cref="StatPassiveGrammar"/>. A rule whose field the record does not carry
    /// is skipped rather than guessed at, so the engine keeps a visible <c>{X}</c> instead of a silent hole.</para>
    /// </summary>
    public static class PassiveDisplayValues
    {
        /// <summary>One value of a template, and where it comes from.</summary>
        /// <param name="Name">The name the template asks for.</param>
        /// <param name="Field">The record field it is read off.</param>
        /// <param name="Negated">The value is the field turned around — a price stored as the negative
        /// share its modifier is built from (−0.6), stated to the reader as the magnitude (60%) because the
        /// sentence around it already says "less". Rendering the signed share there would write the cost
        /// twice and point it both ways.</param>
        private readonly record struct DisplayValue(string Name, string Field, bool Negated = false);

        private static readonly Dictionary<string, DisplayValue[]> s_rules = new(StringComparer.Ordinal)
        {
            ["Passive_Skill_Regeneration"] = [new DisplayValue("PercentFromMaxHealth", "percent")],
            ["Passive_Skill_Mana_Regeneration"] = [new DisplayValue("PercentFromMaxMana", "percent")],
            ["Passive_Skill_Current_Health_Regeneration"] =
                [new DisplayValue("PercentFromCurrentHealth", "percentFromCurrentHealth")],
            ["Passive_Skill_Critical_Leech"] = [new DisplayValue("Percent", "percent")],
            ["Passive_Skill_Critical_Mana_Leech"] = [new DisplayValue("Percent", "percent")],
            ["Passive_Skill_Mana_On_Attack"] = [new DisplayValue("Amount", "amount")],
            ["Passive_Skill_Porcupine"] =
            [
                new DisplayValue("DamageReturn", "damagePercent"),
                new DisplayValue("ArmorAsDamage", "armorPercent")
            ],
            ["Passive_Skill_Bleeding"] =
            [
                new DisplayValue("PercentFromDamage", "percentFromDamage"),
                new DisplayValue("BleedDuration", "duration"),
                new DisplayValue("MaxStack", "maxStacks")
            ],
            ["Passive_Skill_Bloodthirsty"] =
            [
                new DisplayValue("StackThreshold", "stackThreshold"),
                new DisplayValue("HealPercent", "healPercent")
            ],
            ["Passive_Skill_Righteous_Wrath"] =
            [
                new DisplayValue("PercentFromDamage", "percentFromDamage"),
                new DisplayValue("Duration", "burningDuration"),
                new DisplayValue("StackThreshold", "stackThreshold"),
                new DisplayValue("IncinerationDuration", "incinerationDuration")
            ],
            ["Passive_Skill_Silent_Fury"] = [new DisplayValue("SilenceSealChance", "chance")],
            ["Passive_Skill_Creators_Nature"] =
            [
                new DisplayValue("ManaRecovery", "manaRecovery"),
                new DisplayValue("HealthRecovery", "healthRecovery"),
                new DisplayValue("RecoveryEfficiency", "recoveryEfficiency")
            ],
            ["Passive_Skill_Servant_Hell"] = [new DisplayValue("Chance", "chance")],
            ["Passive_Skill_Execute"] = [new DisplayValue("Threshold", "threshold")],
            ["Passive_Skill_Vampire"] = [new DisplayValue("LeachPercent", "percent")],
            ["Passive_Skill_Poisoned_Claws"] =
            [
                new DisplayValue("PercentFromDamage", "percentFromDamage"),
                new DisplayValue("PoisonDuration", "duration")
            ],
            ["Passive_Skill_Accelerator"] = [new DisplayValue("Amount", "amount")],
            ["Passive_Skill_Decomposition"] =
            [
                new DisplayValue("ReduceBy", "reduceBy"),
                new DisplayValue("MaxStacks", "maxStacks")
            ],
            ["Passive_Skill_First_Strike"] = [new DisplayValue("Bonus", "bonus")],
            ["Passive_Skill_Bastion"] = [new DisplayValue("Reduce", "reduce")],
            ["Passive_Skill_Mana_Resonance"] = [new DisplayValue("Rate", "rate")],
            ["Passive_Skill_Counter_Attack"] = [new DisplayValue("Chance", "chance")],
            ["Passive_Skill_Echo"] =
            [
                new DisplayValue("DelayedDamage", "delayedDamagePercent"),
                new DisplayValue("Turns", "turns")
            ],
            ["Passive_Skill_Soul_Devouring"] = [new DisplayValue("BarrierRecoveryAmount", "barrierRecoveryAmount")],
            ["Passive_Skill_Trapped_Beast"] =
            [
                new DisplayValue("HealthPercent", "healthPercent"),
                new DisplayValue("DamageBonus", "damageBonus")
            ],
            ["Passive_Skill_Mana_Burn"] = [new DisplayValue("PercentToBurn", "percentToBurn")],
            ["Passive_Skill_Burning"] =
            [
                new DisplayValue("PercentFromDamage", "percentFromDamage"),
                new DisplayValue("BurningDuration", "duration"),
                new DisplayValue("BurningStacks", "maxStacks")
            ],
            ["Passive_Skill_Resonance"] =
            [
                new DisplayValue("SpellDamagePerStack", "spellDamagePerStack"),
                new DisplayValue("MulticastPerStack", "multicastPerStack")
            ],
            ["Passive_Skill_Agnostic"] = [new DisplayValue("CostScale", "costScale")],
            ["Passive_Skill_Vicious_Bite"] =
            [
                new DisplayValue("PerOvercap", "perOvercap"),
                new DisplayValue("ResistancePenalty", "resistancePenalty"),
                new DisplayValue("ResistanceCut", "resistancePenalty", Negated: true)
            ],
            ["Passive_Skill_Gift_Of_Nature"] = [new DisplayValue("ElementalBonus", "elementalBonus")],
            ["Passive_Skill_Strength_Of_Spirit"] =
            [
                new DisplayValue("Chance", "chance"),
                new DisplayValue("PercentOfMaxMana", "percentOfMaxMana"),
                new DisplayValue("RecoveryPenalty", "recoveryPenalty"),
                new DisplayValue("RecoveryCut", "recoveryPenalty", Negated: true)
            ],
            ["Passive_Skill_Elemental_Fury"] =
            [
                new DisplayValue("ResistancePenalty", "resistancePenalty"),
                new DisplayValue("ResistanceCut", "resistancePenalty", Negated: true)
            ],
        };

        /// <summary>The values the passive's template is filled in with, read off the record it is tuned by.
        /// Empty for a passive whose text names none — which is why a caller renders through this rather
        /// than asking whether it should.</summary>
        public static IReadOnlyDictionary<string, object?> Of(
            string? passiveId, IReadOnlyDictionary<string, float>? properties)
        {
            Dictionary<string, object?> values = new(StringComparer.Ordinal);
            if (passiveId is null || properties is null || !s_rules.TryGetValue(passiveId, out DisplayValue[]? rules))
                return values;

            foreach (DisplayValue rule in rules)
                if (properties.TryGetValue(rule.Field, out float value))
                    values[rule.Name] = rule.Negated ? -value : value;

            return values;
        }

        /// <summary>
        /// Every row of the table, as the passive it belongs to, the name the template asks for and the
        /// field it is read off. Handed out whole — and NOT filtered down to the turned-around ones —
        /// so the invariant behind the flag can be walked from the outside: a walk over rows that already
        /// carry the flag would lose the very row a dropped flag turns wrong, and pass.
        /// <para>What the walk asks of a row is decided by the FIELD: a price is written into a record as
        /// the negative share its modifier is built from, and a name restating that price under another
        /// word is owed the magnitude, because the sentence around it already says "less". A row that
        /// stopped turning its value around would otherwise reach the player as "-45% less" — the cost
        /// written twice and pointing both ways.</para>
        /// </summary>
        public static IEnumerable<(string PassiveId, string Name, string Field, bool Negated)> Rules()
        {
            foreach ((string passiveId, DisplayValue[] rules) in s_rules)
                foreach (DisplayValue rule in rules)
                    yield return (passiveId, rule.Name, rule.Field, rule.Negated);
        }

        /// <summary>
        /// The passive's rule text with its numbers in it, for a reader holding nothing but the catalog and
        /// the record — the tree editor, and any test standing where it stands. The game itself reaches the
        /// same sentence through the passive it built (<see cref="ISkill.Describe"/>), which is the same
        /// template filled from the same rules.
        /// <para>In <see cref="TextFormat.Plain"/> a keyword arrives as the bare word it names: a tool with
        /// no tooltip windows behind it has nothing to open, and a raw <c>{@Poison}</c> would be worse than
        /// an unlinked "Poison".</para>
        /// </summary>
        public static string Describe(
            string passiveId,
            IReadOnlyDictionary<string, float>? properties,
            ILocalizationProvider localization,
            TextFormat format = TextFormat.Plain) =>
            new TextTemplateEngine(localization).Render(
                localization.Translate(passiveId + LocalizationService.DescriptionSuffix),
                Of(passiveId, properties),
                format);
    }
}
