namespace Core.PassiveTree.View
{
    using System.Collections.Generic;
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

        /// <summary>Every line of the node, parametric ones first and pipeline knobs after, in authored
        /// order; records sharing a group stamp arrive as ONE line, joined in the group's order and gated
        /// once. A missing formatter falls back to the raw parts rather than disappearing.</summary>
        public static List<PassiveNodeLine> Of(
            PassiveNode node,
            ModifierFormatter? modifiers,
            ContextModifierFormatter? knobs,
            ILocalizationProvider? localization,
            TextFormat format = TextFormat.Plain)
        {
            List<PassiveNodeLine> lines = [];

            foreach (NodeLineGroup group in node.LineGroups())
                lines.Add(new PassiveNodeLine(Describe(group, modifiers, knobs, localization, format), group.IsConditional));

            return lines;
        }

        /// <summary>The node's headline: its title, or what it is about when it was never titled.</summary>
        public static string TitleOf(PassiveNode node, ILocalizationProvider? localization)
        {
            if (!string.IsNullOrWhiteSpace(node.Title)) return node.Title;
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
