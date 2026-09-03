namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// Where a field holds an entry of a vocabulary rather than a record of the catalog it stands in: the
    /// types such an entry may be written as, the key whose value names which of them it is, and whether
    /// the field holds one entry or a list of them.
    /// </summary>
    /// <remarks>The vocabulary itself belongs to whoever owns the words — the host names the fields and
    /// hands the types over. Nothing here knows what a condition or an action is.</remarks>
    public sealed record VocabularyBinding(IReadOnlyList<RecordSchema> Types, string TypeKey, bool List);

    /// <summary>
    /// A record whose shape is decided by a word it carries rather than by the field it stands in — what
    /// the file says it is, what a fresh one looks like, and what changing its type does to the keys it
    /// already has.
    /// <para>The same questions <see cref="RecordTemplates"/> answers for a polymorphic record, asked of a
    /// value the schema could only call free json: the types come from outside the schema, so a variant
    /// set — which requires every shape to declare the key it is told apart by — cannot state them.</para>
    /// </summary>
    public static class TypedRecords
    {
        /// <summary>The word an entry names its type with, or nothing where it names none. Read the way
        /// the file spells it: a type the vocabulary does not hold is still the author's word, and the
        /// tool has to be able to show it back to him.</summary>
        public static string Standing(VocabularyBinding binding, JToken? token)
        {
            ArgumentNullException.ThrowIfNull(binding);

            return token is JObject holder
                   && holder.TryGetValue(binding.TypeKey, StringComparison.Ordinal, out JToken? named)
                   && named is JValue { Value: not null }
                ? JsonScalars.Written(named)
                : string.Empty;
        }

        /// <summary>The type an entry is written as, or null when it names one the vocabulary does not
        /// hold — and when it names none at all.</summary>
        public static RecordSchema? Worn(VocabularyBinding binding, JToken? token) =>
            Named(binding, Standing(binding, token));

        /// <summary>
        /// A fresh entry of one type: the keys that type cannot be written without, under the word naming
        /// it. The word comes first, because it is what the entry is read by and what the author looks
        /// for down a column of them.
        /// </summary>
        public static JObject Blank(VocabularyBinding binding, string typeName)
        {
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentException.ThrowIfNullOrEmpty(typeName);

            return Typed(Named(binding, typeName) is { } type ? RecordTemplates.Blank(type) : new JObject(),
                binding.TypeKey, typeName);
        }

        /// <summary>
        /// The value a KEY is written with when the author asks for the key itself and has said nothing
        /// about what goes in it: the empty list wherever the vocabulary is read as one, and the blank of
        /// the field's own kind everywhere else.
        /// <para>A schema that can only call a key free json blanks it as an empty OBJECT, and an object
        /// under a key the game reads as a list is a key the game reads as nothing at all — the author
        /// would go on filling in an entry the parser never looks at. This is the one place a key of a
        /// vocabulary is opened, so the picker that adds it and the block that draws it agree.</para>
        /// </summary>
        public static JToken Blank(VocabularyBinding? binding, FieldSchema field)
        {
            ArgumentNullException.ThrowIfNull(field);

            return binding is { List: true } ? new JArray() : RecordTemplates.Blank(field);
        }

        /// <summary>
        /// Writes the entry at <paramref name="at"/> as another type: the keys only the outgoing type
        /// names go, the keys the incoming one cannot be without arrive with their blanks, and everything
        /// both name — and everything neither does — keeps the value the author gave it.
        /// <para>The entry is replaced whole, so the change is ONE step of the history: an entry halfway
        /// between two types is one the game reads as neither.</para>
        /// <para>False when the document holds no entry there, and when it already names that type.</para>
        /// </summary>
        public static bool Switch(JsonTreeDocument document, JsonPointer at, VocabularyBinding binding, string typeName)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(at);
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentException.ThrowIfNullOrEmpty(typeName);

            if (document.Resolve(at) is not JObject holder) return false;

            string standing = Standing(binding, holder);

            if (string.Equals(standing, typeName, StringComparison.Ordinal)) return false;

            RecordSchema? wanted = Named(binding, typeName);
            var written = (JObject)holder.DeepClone();

            foreach (string key in Shed(Named(binding, standing), wanted)) written.Remove(key);

            if (wanted is { } type) Filled(written, type);

            return document.SetValue(at, Typed(written, binding.TypeKey, typeName));
        }

        /// <summary>
        /// Writes the single entry a field holds as a list of one. A key the game reads as a list may be
        /// found holding an entry on its own; every gesture that adds a second one has to have somewhere
        /// to add it to, and this is the one step that makes the room.
        /// <para>One step of the history like any other, and false where there is no lone entry to
        /// move — a list already, or nothing at all.</para>
        /// </summary>
        public static bool AsList(JsonTreeDocument document, JsonPointer at)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(at);

            return document.Resolve(at) is JObject one && document.SetValue(at, new JArray(one.DeepClone()));
        }

        private static RecordSchema? Named(VocabularyBinding binding, string typeName)
        {
            if (typeName.Length == 0) return null;

            foreach (RecordSchema type in binding.Types)
                if (string.Equals(type.TypeName, typeName, StringComparison.Ordinal))
                    return type;

            return null;
        }

        /// <summary>Writes the word naming the type. An entry already carrying the key keeps the place it
        /// stands in — a word rewritten where it was is a diff of one line — and one that does not gets it
        /// first, ahead of everything the type is written with.</summary>
        private static JObject Typed(JObject holder, string typeKey, string typeName)
        {
            if (holder.ContainsKey(typeKey)) holder[typeKey] = new JValue(typeName);
            else holder.AddFirst(new JProperty(typeKey, typeName));

            return holder;
        }

        /// <summary>Adds the keys the incoming type requires and the entry does not hold.</summary>
        private static void Filled(JObject holder, RecordSchema type)
        {
            foreach (FieldSchema field in type.Fields)
            {
                if (!field.Required) continue;
                if (holder.ContainsKey(field.JsonName)) continue;

                holder.Add(field.JsonName, RecordTemplates.Blank(field));
            }
        }

        /// <summary>The keys the outgoing type names and the incoming one does not. Everything else the
        /// entry holds stays: a key both types name keeps the value the author gave it, and a key no type
        /// named is not the tool's to drop.</summary>
        private static List<string> Shed(RecordSchema? worn, RecordSchema? wanted)
        {
            List<string> shed = [];

            if (worn is null) return shed;

            HashSet<string> kept = new(StringComparer.Ordinal);

            if (wanted is { } type)
                foreach (FieldSchema field in type.Fields)
                    kept.Add(field.JsonName);

            foreach (FieldSchema field in worn.Fields)
                if (!kept.Contains(field.JsonName))
                    shed.Add(field.JsonName);

            return shed;
        }
    }
}
