namespace Core.PassiveTree.View
{
    using System.Collections.Generic;
    using Localization;
    using Modifiers;

    /// <summary>One line of a node as the player reads it.</summary>
    /// <param name="Text">The finished sentence, already through the game's own templates.</param>
    /// <param name="IsConditional">The line is gated on a predicate. Marked rather than hidden: a
    /// conditional line is content the node carries, and a wheel that showed it as unconditional would
    /// promise a number the character does not always have.</param>
    public readonly record struct PassiveNodeLine(string Text, bool IsConditional);

    /// <summary>
    /// What a node says, in the same words the rest of the game uses: modifier lines through
    /// <see cref="ModifierFormatter"/> and pipeline knobs through <see cref="ContextModifierFormatter"/>,
    /// so a passive reads exactly the way the same line reads in an item tooltip — same .po templates,
    /// same percent-versus-number decision per parameter.
    /// <para>One reading for the whole game: the wheel's tooltip and its node card both come through
    /// here, so the two can never word the same node differently.</para>
    /// </summary>
    public static class PassiveNodeLines
    {
        /// <summary>How many steps a route needs before its price is worth printing beside the node. Every
        /// node in the tree costs a point, so on a neighbour the figure only repeats what the player
        /// already knows; from two steps on it stops being a property of the node and becomes one of the
        /// distance to it, which is the number a plan is actually made with.</summary>
        public const int ShortestPricedRoute = 2;

        /// <summary>Wraps the condition after the sentence it gates: "…  (WhileWounded)".</summary>
        private const string ConditionTemplate = "{0}  ({1})";

        /// <summary>
        /// Every line of the node, parametric ones first and pipeline knobs after, in the order they
        /// were authored. A missing formatter is not an error the player should meet: the line falls
        /// back to its raw parts rather than disappearing.
        /// </summary>
        public static List<PassiveNodeLine> Of(
            PassiveNode node,
            ModifierFormatter? modifiers,
            ContextModifierFormatter? knobs,
            ILocalizationProvider? localization,
            TextFormat format = TextFormat.Plain)
        {
            List<PassiveNodeLine> lines = [];

            foreach (ModifierLine line in node.Modifiers)
                lines.Add(new PassiveNodeLine(Describe(line, modifiers, localization, format), line.IsConditional));

            foreach (ContextModifierLine line in node.ContextModifiers)
                lines.Add(new PassiveNodeLine(Describe(line, knobs, localization, format), line.IsConditional));

            return lines;
        }

        /// <summary>Whether a route of this many steps is worth quoting a price for — see
        /// <see cref="ShortestPricedRoute"/>. Unreachable nodes price at nothing and say nothing.</summary>
        public static bool PricesTheRoute(int steps) => steps >= ShortestPricedRoute;

        /// <summary>The node's headline: its title, or what it is about when it was never titled.</summary>
        public static string TitleOf(PassiveNode node, ILocalizationProvider? localization)
        {
            if (!string.IsNullOrWhiteSpace(node.Title)) return node.Title;
            if (!string.IsNullOrWhiteSpace(node.AbilityId)) return Translate(localization, node.AbilityId);

            return node.Id;
        }

        private static string Describe(ModifierLine line, ModifierFormatter? formatter, ILocalizationProvider? localization, TextFormat format)
        {
            string text = formatter is null
                ? $"{line.Parameter} {line.ValueType} {line.Value}"
                : formatter.Format(new SimpleModifier(line.Parameter, line.ValueType, line.Value, PassiveTreeDocument.ModifierSource), format);

            return WithCondition(text, line.Condition, line.IsConditional, localization);
        }

        /// <summary>The knob entry exists for the sentence and nothing else — it is never attached to
        /// anyone, because what a fighter gets is the sum of the taken lines and not one modifier per
        /// node.</summary>
        private static string Describe(ContextModifierLine line, ContextModifierFormatter? formatter, ILocalizationProvider? localization, TextFormat format)
        {
            string text = formatter is null
                ? $"{line.Parameter} {line.ValueType} {line.Value}"
                : formatter.Format(new ContextModifierEntry(line.Parameter, line.ValueType, line.Value), format);

            return WithCondition(text, line.Condition, line.IsConditional, localization);
        }

        private static string WithCondition(string text, string condition, bool conditional, ILocalizationProvider? localization) =>
            conditional ? string.Format(ConditionTemplate, text, Translate(localization, condition)) : text;

        private static string Translate(ILocalizationProvider? localization, string key) =>
            localization is null ? key : localization.Translate(key);
    }
}
