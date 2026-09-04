namespace Tooling.Narrative
{
    using System;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;

    /// <summary>
    /// The words a narrative document is written in that more than one reading of it needs, and the ways
    /// a value is taken out of one.
    /// <para>Written down once: the outline an author walks and the map he reads the same conversation as
    /// are two readings of one file, and a word spelled apart in the two of them is two answers about
    /// where a route leads.</para>
    /// </summary>
    internal static class NarrativeDocument
    {
        public const string EntryRules = "entryRules";

        public const string Nodes = "nodes";

        public const string Lines = "lines";

        public const string Options = "options";

        /// <summary>What a node and an option are both named under, and what a record of any catalog
        /// carries its own name under where its schema names no other field.</summary>
        public const string Id = "id";

        /// <summary>The node an entry rule opens the conversation on.</summary>
        public const string Node = "node";

        public const string Next = "next";

        public const string SpeechCheck = "speechCheck";

        public const string FailNext = "failNext";

        public const string VisibleConditions = "visibleConditions";

        public const string EnabledConditions = "enabledConditions";

        /// <summary>What a key holds, or null for a key the record does not carry.</summary>
        public static JToken? Held(JToken token, string name) =>
            token is JObject holder && holder.TryGetValue(name, StringComparison.Ordinal, out JToken? value)
                ? value
                : null;

        /// <summary>The text a key holds, spelled the way the file spells it; null for a key the record
        /// does not hold and for one written as nothing at all.</summary>
        public static string? Written(JToken token, string name) =>
            Held(token, name) is JValue { Value: not null } value ? JsonScalars.Written(value) : null;

        /// <summary>Whether a key is written true. An absent switch is off, which is what the game reads
        /// in its place.</summary>
        public static bool Flag(JToken token, string name) =>
            Held(token, name) is JValue { Type: JTokenType.Boolean } value && value.Value<bool>();

        /// <summary>The elements a key holds, and none where it holds no list: a key written as something
        /// other than a collection is one the game reads no element out of either.</summary>
        public static JArray Elements(JToken token, string name) => Held(token, name) as JArray ?? [];
    }
}
