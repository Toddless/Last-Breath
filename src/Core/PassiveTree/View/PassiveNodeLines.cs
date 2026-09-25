namespace Core.PassiveTree.View
{
    using System.Collections.Generic;
    using Battle;
    using Battle.Skills;
    using Localization;
    using Modifiers;

    /// <summary>One line of a node as the player reads it.</summary>
    /// <param name="Text">The finished sentence, already through the game's own templates.</param>
    /// <param name="IsConditional">The line is gated on a predicate — marked rather than hidden, so the
    /// wheel never shows a number the character doesn't always have.</param>
    public readonly record struct PassiveNodeLine(string Text, bool IsConditional);

    /// <summary>
    /// What a node says, in the same words the rest of the game uses: modifier lines through
    /// <see cref="ModifierFormatter"/> and pipeline knobs through <see cref="ContextModifierFormatter"/>,
    /// so a passive reads exactly like the same line in an item tooltip — same .po templates, same
    /// percent-versus-number decision. One reading for the whole game: the wheel's tooltip and its node
    /// card both come through here, so the two can never word a node differently.
    /// </summary>
    public static class PassiveNodeLines
    {
        /// <summary>The shape a gated line falls back to with no catalog behind it — the parts, the way
        /// the rest of this class falls back to them. The wording of a gate is the catalog's.</summary>
        private const string ConditionTemplate = "{0}  ({1})";

        /// <summary>The separator between the parts of one composite line, the way an item tooltip joins
        /// the parts of a composite roll.</summary>
        private const string PartSeparator = ", ";

        /// <summary>
        /// Every line of the node, parametric ones first and pipeline knobs after, in authored order;
        /// records sharing a group stamp arrive as ONE line, joined in the group's order and gated once.
        /// A missing formatter falls back to the raw parts rather than disappearing.
        /// <para>A node that hands over a PASSIVE says what the passive says instead — see
        /// <see cref="OfPassive"/>. The two are alternatives in the data (a node carrying both is refused
        /// where it is read), so the passive is asked about first and the line channel is left alone.</para>
        /// </summary>
        /// <param name="skills">The registry the GRANT builds a named passive with. Handed in so the popup
        /// promises the very instance the click hands over — the numbers of THIS node, through the passive's
        /// own card. Optional: a reader with no registry behind it falls back to the untouched wording.</param>
        public static List<PassiveNodeLine> Of(
            PassiveNode node,
            ModifierFormatter? modifiers,
            ContextModifierFormatter? knobs,
            ILocalizationProvider? localization,
            TextFormat format = TextFormat.Plain,
            ISkillProvider? skills = null)
        {
            List<PassiveNodeLine> lines = [];

            if (node.PassiveId is { } passiveId) return OfPassive(passiveId, node, modifiers, localization, format, skills);

            foreach (NodeLineGroup group in node.LineGroups())
                lines.Add(new PassiveNodeLine(Describe(group, modifiers, knobs, localization, format), group.IsConditional));

            return lines;
        }

        /// <summary>
        /// What a passive-bearing node says. The two families answer differently because they are written
        /// differently:
        /// <list type="bullet">
        /// <item>a passive built from FIELDS (<see cref="StatPassiveGrammar"/>) has no rule text anybody
        /// could have written — its whole content is its numbers, so the node prints them through the very
        /// formatter an ordinary node line goes through, and the wheel and the passive's own card cannot
        /// disagree;</item>
        /// <item>a passive written as a CLASS has a hand-written description under its own
        /// <c>&lt;Id&gt;_Description</c> key, and the node BUILDS that passive from its own properties —
        /// the same pair the grant is made from — so the card the popup shows is the card of the very
        /// instance the click hands over, numbers and all. Reading the key out raw was the older shape of
        /// this branch and printed the template itself: "{PercentFromDamage:%}" where a player wanted 75%.
        /// Without a registry to build with, that older reading is what is left.</item>
        /// </list>
        /// <para>Nothing here is gated: a gate lives on a node's parametric records, and a passive carries
        /// none.</para>
        /// <para>A record the grammar cannot read whole says nothing at all — the node is left with its
        /// title and its class. The popup promises exactly what the grant hands over, and the grant refuses
        /// such a record entire; half a card would sell the node for lines the player never receives.</para>
        /// </summary>
        private static List<PassiveNodeLine> OfPassive(
            string passiveId,
            PassiveNode node,
            ModifierFormatter? modifiers,
            ILocalizationProvider? localization,
            TextFormat format,
            ISkillProvider? skills)
        {
            List<PassiveNodeLine> lines = [];

            if (!StatPassiveGrammar.Owns(passiveId))
            {
                lines.Add(new PassiveNodeLine(NamedPassive(passiveId, node, localization, format, skills), IsConditional: false));

                return lines;
            }

            foreach (string text in StatPassiveLineText.Lines(node.Properties, modifiers, format))
                lines.Add(new PassiveNodeLine(text, IsConditional: false));

            return lines;
        }

        /// <summary>
        /// What a passive written as a CLASS says on this node. The passive is built from the pair the
        /// grant is built from — the id and the node's own properties — and then asked for its card, so
        /// the popup and the card cannot word one keystone two ways, and a second node of the same passive
        /// tuned differently reads with ITS numbers rather than with anybody's defaults.
        /// <para>A registry that cannot build the id has already said why; the wording is read out
        /// untouched rather than left blank, which is the reading this branch had before it could build.</para>
        /// </summary>
        private static string NamedPassive(
            string passiveId,
            PassiveNode node,
            ILocalizationProvider? localization,
            TextFormat format,
            ISkillProvider? skills) =>
            skills?.CreateSkill(passiveId, new RecordProperties(passiveId, node.Properties)) is { } skill
                ? skill.Describe(format)
                : Translate(localization, passiveId + LocalizationService.DescriptionSuffix);

        /// <summary>The node's headline: its title, or what it is about when it was never titled — the
        /// passive it hands over, the ability it unlocks, and its own id when it is none of those.</summary>
        public static string TitleOf(PassiveNode node, ILocalizationProvider? localization)
        {
            if (!string.IsNullOrWhiteSpace(node.Title)) return node.Title;
            if (node.PassiveId is { } passiveId) return Translate(localization, passiveId);
            if (!string.IsNullOrWhiteSpace(node.AbilityId)) return Translate(localization, node.AbilityId);

            return node.Id;
        }

        /// <summary>One line's sentence: every part of the group worded on its own, joined, and the gate
        /// named once at the end — a clause per part would say the same thing twice.</summary>
        private static string Describe(
            NodeLineGroup group,
            ModifierFormatter? modifiers,
            ContextModifierFormatter? knobs,
            ILocalizationProvider? localization,
            TextFormat format)
        {
            List<string> parts = [];

            foreach (ModifierLine line in group.Modifiers) parts.Add(Describe(line, modifiers, format));
            foreach (ContextModifierLine line in group.ContextModifiers) parts.Add(Describe(line, knobs, format));

            return WithCondition(string.Join(PartSeparator, parts), group.Condition, group.IsConditional, localization, format);
        }

        /// <summary>A line measured per unit of a carrier parameter hands that parameter to the formatter,
        /// which words it — the number alone would read as an outright bonus.</summary>
        private static string Describe(ModifierLine line, ModifierFormatter? formatter, TextFormat format) =>
            formatter is null
                ? $"{line.Parameter} {line.ValueType} {line.Value}{(line.IsScaled ? $" per {line.PerParameter}" : string.Empty)}"
                : formatter.Format(
                    new SimpleModifier(line.Parameter, line.ValueType, line.Value, PassiveTreeDocument.ModifierSource), format, line.PerParameter);

        /// <summary>The knob entry exists for the sentence and nothing else — it is never attached to
        /// anyone, since what a fighter gets is the sum of the taken lines, not one modifier per node.</summary>
        private static string Describe(ContextModifierLine line, ContextModifierFormatter? formatter, TextFormat format) =>
            formatter is null
                ? $"{line.Parameter} {line.ValueType} {line.Value}"
                : formatter.Format(new ContextModifierEntry(line.Parameter, line.ValueType, line.Value), format);

        /// <summary>The gate, joined to the sentence it holds up through the game's ONE reading of a
        /// conditional line: the clause is worded under the condition's own catalog key, so a passive
        /// announces a gate in the same words an item does instead of printing the raw id.</summary>
        private static string WithCondition(
            string text, string condition, bool conditional, ILocalizationProvider? localization, TextFormat format)
        {
            if (!conditional) return text;

            return localization is null
                ? string.Format(ConditionTemplate, text, condition)
                : ConditionalLineText.Join(localization, text, condition, format);
        }

        private static string Translate(ILocalizationProvider? localization, string key) =>
            localization is null ? key : localization.Translate(key);
    }
}
