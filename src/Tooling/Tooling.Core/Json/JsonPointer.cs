namespace Tooling.Json
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Newtonsoft.Json.Linq;

    /// <summary>The address of one node in a JSON tree, written the way RFC 6901 writes it
    /// (<c>/npcs/3/abilities/0</c>) and compared by value.</summary>
    public sealed class JsonPointer : IEquatable<JsonPointer>
    {
        public const char Separator = '/';

        private const char Escape = '~';

        /// <summary>What follows <see cref="Escape"/> to mean the escape character itself.</summary>
        private const char EscapedEscapeMark = '0';

        /// <summary>What follows <see cref="Escape"/> to mean a separator inside a segment.</summary>
        private const char EscapedSeparatorMark = '1';

        /// <summary>An index never carries one: <c>01</c> is a key, and reading it as element one would
        /// address a node the author did not write.</summary>
        private const char LeadingZero = '0';

        private readonly string[] _segments;
        private readonly string _text;

        private JsonPointer(string[] segments)
        {
            _segments = segments;

            var builder = new StringBuilder();
            foreach (string segment in segments) builder.Append(Separator).Append(Escaped(segment));

            _text = builder.ToString();
        }

        /// <summary>The whole document: no segments, written as an empty string.</summary>
        public static JsonPointer Root { get; } = new([]);

        public IReadOnlyList<string> Segments => _segments;

        public bool IsRoot => _segments.Length == 0;

        /// <summary>The last segment — a key or an array index — or null at the root.</summary>
        public string? Last => IsRoot ? null : _segments[^1];

        /// <summary>The container this node sits in, or null at the root.</summary>
        public JsonPointer? Parent => IsRoot ? null : new JsonPointer(_segments[..^1]);

        public static bool operator ==(JsonPointer? left, JsonPointer? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(JsonPointer? left, JsonPointer? right) => !(left == right);

        /// <summary>Reads a pointer. An empty string is the root; anything else has to start with a
        /// separator and escape what it must, so a malformed address is refused instead of addressing
        /// something else.</summary>
        public static JsonPointer Parse(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            if (text.Length == 0) return Root;

            if (text[0] != Separator)
                throw new FormatException($"'{text}' is not a JSON pointer: one is either empty or starts with '{Separator}'");

            string[] parts = text.Split(Separator);
            string[] segments = new string[parts.Length - 1];

            for (int part = 1; part < parts.Length; part++) segments[part - 1] = Unescaped(parts[part], text);

            return new JsonPointer(segments);
        }

        /// <summary>Whether a segment is an array index as RFC 6901 writes one: digits only, no sign and
        /// no leading zero, so <c>01</c> stays a key and never becomes element one.</summary>
        public static bool TryReadIndex(string segment, out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(segment)) return false;
            if (segment.Length > 1 && segment[0] == LeadingZero) return false;

            foreach (char symbol in segment)
                if (!char.IsAsciiDigit(symbol))
                    return false;

            if (int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index)) return true;

            index = -1;
            return false;
        }

        public JsonPointer Append(string segment)
        {
            ArgumentNullException.ThrowIfNull(segment);

            return new JsonPointer([.. _segments, segment]);
        }

        public JsonPointer Append(int index) => Append(index.ToString(CultureInfo.InvariantCulture));

        /// <summary>Whether this address lies at or below <paramref name="of"/>: the node itself and
        /// everything written inside it. Read segment by segment and never off the text — <c>/nodes/10</c>
        /// is no part of <c>/nodes/1</c>, and whoever asked by the letter would answer for the neighbour of
        /// the node he means.</summary>
        public bool Within(JsonPointer of)
        {
            ArgumentNullException.ThrowIfNull(of);

            if (_segments.Length < of._segments.Length) return false;

            for (int index = 0; index < of._segments.Length; index++)
                if (!string.Equals(_segments[index], of._segments[index], StringComparison.Ordinal))
                    return false;

            return true;
        }

        /// <summary>The node this pointer addresses, or null when the tree has nothing there. A missing
        /// path is an answer, not a failure: a tool asks about paths the author has not typed yet.</summary>
        public JToken? Resolve(JToken root)
        {
            ArgumentNullException.ThrowIfNull(root);

            JToken? current = root;

            foreach (string segment in _segments)
            {
                current = current switch
                {
                    JObject holder => holder.TryGetValue(segment, StringComparison.Ordinal, out JToken? child) ? child : null,
                    JArray array when TryReadIndex(segment, out int index) && index < array.Count => array[index],
                    _ => null
                };

                if (current is null) return null;
            }

            return current;
        }

        public bool Equals(JsonPointer? other) => other is not null && string.Equals(_text, other._text, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as JsonPointer);

        public override int GetHashCode() => _text.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => _text;

        private static string Escaped(string segment)
        {
            if (segment.IndexOf(Escape) < 0 && segment.IndexOf(Separator) < 0) return segment;

            var builder = new StringBuilder(segment.Length);

            foreach (char symbol in segment)
            {
                if (symbol == Escape) builder.Append(Escape).Append(EscapedEscapeMark);
                else if (symbol == Separator) builder.Append(Escape).Append(EscapedSeparatorMark);
                else builder.Append(symbol);
            }

            return builder.ToString();
        }

        private static string Unescaped(string segment, string pointer)
        {
            if (segment.IndexOf(Escape) < 0) return segment;

            var builder = new StringBuilder(segment.Length);

            for (int symbol = 0; symbol < segment.Length; symbol++)
            {
                if (segment[symbol] != Escape)
                {
                    builder.Append(segment[symbol]);
                    continue;
                }

                if (symbol + 1 == segment.Length)
                    throw new FormatException($"'{pointer}' is not a JSON pointer: '{Escape}' at the end of a segment escapes nothing");

                char mark = segment[++symbol];

                builder.Append(mark switch
                {
                    EscapedEscapeMark => Escape,
                    EscapedSeparatorMark => Separator,
                    _ => throw new FormatException($"'{pointer}' is not a JSON pointer: '{Escape}{mark}' is not an escape")
                });
            }

            return builder.ToString();
        }
    }
}
