namespace Core.Localization
{
    using System.Collections.Generic;
    using Battle.Skills;
    using Modifiers;

    /// <summary>
    /// A stat passive's field as the player reads it, and the ONE wording of it. The line goes through
    /// <see cref="ModifierFormatter"/> exactly the way a tree node's own parametric line does — same .po
    /// templates, same percent-versus-number decision, same "per {per}" tail — so the popup on the node,
    /// the card of the passive it hands over and an item that grants the same passive cannot word one
    /// number three ways.
    /// </summary>
    public static class StatPassiveLineText
    {
        /// <summary>What the lines of one record are joined with, the way a card stacks its rows.</summary>
        public const string LineSeparator = "\n";

        /// <summary>Named so a modifier minted for reading alone can be told from one anybody wears. The
        /// wording never depends on it — <see cref="ModifierFormatter"/> reads the bucket, the parameter
        /// and the number and nothing else — but a nameless source in a debugger is a puzzle.</summary>
        private const string Source = "StatPassive";

        /// <summary>One field as a sentence. A line measured per unit of a carrier hands that carrier to
        /// the formatter, which words it — the number alone would read as an outright bonus. With no
        /// formatter behind it the parts are printed raw rather than dropped, the way every other reader
        /// of a modifier line falls back.</summary>
        public static string Line(StatPassiveLine line, ModifierFormatter? formatter, TextFormat format = TextFormat.Plain) =>
            formatter is null
                ? $"{line.Parameter} {line.ValueType} {line.Value}{(line.PerParameter is { } carrier ? $" per {carrier}" : string.Empty)}"
                : formatter.Format(
                    new SimpleModifier(line.Parameter, line.ValueType, line.Value, Source), format, line.PerParameter);

        /// <summary>The whole record as sentences, in authored order — and nothing at all when one field
        /// of it will not parse, because that is exactly what the grant hands over. See
        /// <see cref="StatPassiveGrammar.ReadWhole"/>.</summary>
        public static List<string> Lines(
            IReadOnlyDictionary<string, float> fields, ModifierFormatter? formatter, TextFormat format = TextFormat.Plain)
        {
            List<string> lines = [];
            foreach (StatPassiveLine line in StatPassiveGrammar.ReadWhole(fields)) lines.Add(Line(line, formatter, format));

            return lines;
        }
    }

    /// <summary>Lets a stat line be handed to <see cref="ILocalizationService.Format"/> like any other
    /// domain object, so a passive holding lines can say what it does without carrying a formatter of its
    /// own — one class and one registration, which is what the registry exists for.</summary>
    public class StatPassiveLineTextFormatter(ModifierFormatter formatter) : ITextFormatter
    {
        public bool CanFormat(object value) => value is StatPassiveLine;

        public string Format(object value, TextFormat format) => StatPassiveLineText.Line((StatPassiveLine)value, formatter, format);
    }
}
