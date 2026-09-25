namespace Tooling.Localization
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>The words and marks a gettext catalog is spelled with.</summary>
    internal static class PoSyntax
    {
        public const string MsgId = "msgid";
        public const string MsgIdPlural = "msgid_plural";
        public const string MsgStr = "msgstr";
        public const string MsgContext = "msgctxt";

        public const char Comment = '#';
        public const char Quote = '"';
        public const char Backslash = '\\';
        public const char PluralOpen = '[';
        public const char PluralClose = ']';
        public const char Space = ' ';

        /// <summary>What a comment carries after its <c>#</c> when it was written against one message:
        /// extracted, reference, flag, previous, obsolete. A comment with none of them is the file's own.</summary>
        public const string CommentMarks = ".:,|~";

        public const char LineFeed = '\n';
        public const char CarriageReturn = '\r';
        public const string Lf = "\n";
        public const string CrLf = "\r\n";
        public const char ByteOrderMark = (char)0xFEFF;

        /// <summary>The form index of an entry gettext does not count — a plain <c>msgstr</c>.</summary>
        public const int Singular = -1;
    }

    /// <summary>
    /// Entries in and out of gettext lines: the escapes the format spells, and the layout a value arrived
    /// in. A value read across several lines is written back across the same lines, so an entry the author
    /// edited does not collapse into one long line the next reader has to scroll.
    /// </summary>
    internal static class PoWriter
    {
        /// <summary>Every escape the format has, read in both directions from one table — two tables would
        /// be two chances for a character to survive the way in and be lost on the way out.</summary>
        private static readonly (char Character, char Mark)[] Escapes =
        [
            (PoSyntax.Backslash, PoSyntax.Backslash),
            (PoSyntax.Quote, PoSyntax.Quote),
            ('\n', 'n'),
            ('\t', 't'),
            ('\r', 'r'),
            ('\a', 'a'),
            ('\b', 'b'),
            ('\f', 'f'),
            ('\v', 'v')
        ];

        public static string PluralKeyword(int form) =>
            $"{PoSyntax.MsgStr}{PoSyntax.PluralOpen}{form}{PoSyntax.PluralClose}";

        /// <summary>Writes one keyword and its value, on one line or laid out across several the way the
        /// value came in: an empty first line and one line per break.</summary>
        public static void WriteValue(List<string> lines, string keyword, string value, bool multiline)
        {
            IReadOnlyList<string> chunks = multiline ? SplitAfterBreaks(value) : [];

            if (chunks.Count == 0)
            {
                lines.Add($"{keyword}{PoSyntax.Space}{Quoted(value)}");
                return;
            }

            lines.Add($"{keyword}{PoSyntax.Space}{Quoted(string.Empty)}");

            foreach (string chunk in chunks)
            {
                lines.Add(Quoted(chunk));
            }
        }

        public static string Quoted(string value) => $"{PoSyntax.Quote}{Escape(value)}{PoSyntax.Quote}";

        public static string Escape(string value)
        {
            var text = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                if (MarkOf(character) is { } mark) text.Append(PoSyntax.Backslash).Append(mark);
                else text.Append(character);
            }

            return text.ToString();
        }

        /// <summary>Splits a value after every line break. gettext lays a long string out this way, and a
        /// rewritten entry keeps the same shape as the ones around it.</summary>
        public static IReadOnlyList<string> SplitAfterBreaks(string value)
        {
            List<string> chunks = [];
            int start = 0;

            for (int at = 0; at < value.Length; at++)
            {
                if (value[at] != PoSyntax.LineFeed) continue;

                chunks.Add(value[start..(at + 1)]);
                start = at + 1;
            }

            if (start < value.Length) chunks.Add(value[start..]);

            return chunks;
        }

        /// <summary>Reads the quoted string standing at <paramref name="start"/> and refuses anything after
        /// it. A line the writer could never have produced is a line the tool cannot promise to keep.</summary>
        public static string ReadQuoted(string line, int start, int number)
        {
            // gettext lets a value stand any distance from its keyword, and a continuation be indented under
            // it. The blanks are read past here; an entry nobody edits is written back from its own lines.
            while (start < line.Length && char.IsWhiteSpace(line[start])) start++;

            if (start >= line.Length || line[start] != PoSyntax.Quote) throw Broken(number, "a quoted value was expected");

            var value = new StringBuilder(line.Length - start);

            for (int at = start + 1; at < line.Length; at++)
            {
                char character = line[at];

                if (character == PoSyntax.Quote)
                {
                    RefuseTrailing(line, at + 1, number);
                    return value.ToString();
                }

                if (character != PoSyntax.Backslash)
                {
                    value.Append(character);
                    continue;
                }

                at++;

                if (at >= line.Length) throw Broken(number, "a value ends with a backslash");

                value.Append(CharacterOf(line[at], number));
            }

            throw Broken(number, "a quoted value is not closed");
        }

        public static FormatException Broken(int line, string what) => new($"line {line}: {what}");

        private static void RefuseTrailing(string line, int from, int number)
        {
            for (int at = from; at < line.Length; at++)
            {
                if (!char.IsWhiteSpace(line[at])) throw Broken(number, "a line holds something after the closing quote");
            }
        }

        private static char? MarkOf(char character)
        {
            foreach ((char Character, char Mark) escape in Escapes)
            {
                if (escape.Character == character) return escape.Mark;
            }

            return null;
        }

        private static char CharacterOf(char mark, int number)
        {
            foreach ((char Character, char Mark) escape in Escapes)
            {
                if (escape.Mark == mark) return escape.Character;
            }

            throw Broken(number, $"unknown escape {PoSyntax.Backslash}{mark}");
        }
    }
}
