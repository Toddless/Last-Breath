namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>Which vocabulary a field is written from, or null where it is written from none. The words
    /// belong to whoever owns them — a key the schema can only call free json is a list of conditions to
    /// the host and nothing at all here.</summary>
    public delegate VocabularyBinding? FieldVocabulary(FieldSchema field);

    /// <summary>One field of a record as the walk met it: where it stands, the schema it is drawn by, and
    /// what the file holds there. Taken down for whoever asks about the SHAPE a value is written in rather
    /// than about the words inside it — a range written as a number, a record wearing two shapes at once.
    /// The two questions are one descent, and a second walk over the same schemas would be a second answer
    /// to keep in step.</summary>
    public sealed record SchemaNode(CatalogFile File, JsonPointer At, FieldSchema Field, JToken Token);

    /// <summary>
    /// Every id a record writes where another catalog's record is meant, at the address the FILE holds it
    /// at. One walk, driven by the schema: a catalog whose DTO grows a reference is walked without anyone
    /// remembering it, and the three places a word can stand — a field of its own, an element of a list,
    /// the key of a map — are told apart because a rename rewrites each of them differently.
    /// <para>Godot-free and document-free: it is handed a token and hands back addresses, so the same walk
    /// answers for a record of a catalog and for an entry of a vocabulary nested inside one.</para>
    /// </summary>
    public static class ReferenceWalk
    {
        /// <summary>Every reference one record writes, <paramref name="at"/> being where the record itself
        /// stands in its file. A token that is not a record writes nothing: the file said so when it was
        /// read, and a walk that guessed would address nodes nobody wrote.</summary>
        /// <param name="nodes">Where every field met on the way is taken down as well, for a caller asking
        /// about the shape of what is written; null for one asking only about the words.</param>
        public static void Record(
            CatalogFile file,
            RecordSchema record,
            JToken? token,
            JsonPointer at,
            FieldVocabulary? vocabulary,
            ICollection<ReferenceMention> found,
            ICollection<SchemaNode>? nodes = null)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(at);
            ArgumentNullException.ThrowIfNull(found);

            if (token is not JObject holder) return;

            foreach (FieldSchema field in Fields(record, holder))
                Field(file, field, holder[field.JsonName], at.Append(field.JsonName), ReferenceUseKind.Value, vocabulary, found, nodes);
        }

        /// <summary>Every reference the entries standing at <paramref name="at"/> write. A key the game
        /// reads as a list may be found holding a lone entry, and it is read where it stands: the author
        /// wrote it, and the game's own parser is the only thing that decides what it makes of it.</summary>
        public static void Entries(
            CatalogFile file,
            VocabularyBinding binding,
            JToken? token,
            JsonPointer at,
            FieldVocabulary? vocabulary,
            ICollection<ReferenceMention> found,
            ICollection<SchemaNode>? nodes = null)
        {
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentNullException.ThrowIfNull(at);

            if (binding.List && token is JArray written)
            {
                for (int index = 0; index < written.Count; index++)
                    Entry(file, binding, written[index], at.Append(index), vocabulary, found, nodes);

                return;
            }

            Entry(file, binding, token, at, vocabulary, found, nodes);
        }

        /// <summary>One entry of a vocabulary, read by the type it names. An entry naming a type this
        /// build does not hold is passed over: nothing here can say which of its keys is an id, and
        /// guessing would rewrite a word the author meant as something else.</summary>
        private static void Entry(
            CatalogFile file,
            VocabularyBinding binding,
            JToken? token,
            JsonPointer at,
            FieldVocabulary? vocabulary,
            ICollection<ReferenceMention> found,
            ICollection<SchemaNode>? nodes)
        {
            if (TypedRecords.Worn(binding, token) is not { } type) return;

            Record(file, type, token, at, vocabulary, found, nodes);
        }

        private static void Field(
            CatalogFile file,
            FieldSchema field,
            JToken? token,
            JsonPointer at,
            ReferenceUseKind kind,
            FieldVocabulary? vocabulary,
            ICollection<ReferenceMention> found,
            ICollection<SchemaNode>? nodes)
        {
            if (token is null || token.Type == JTokenType.Null) return;

            // Taken down before the kind decides what to do with it: a range written as a plain number is
            // a field whose schema says object and whose token says otherwise, and a walk that took nodes
            // down only where it descended would never meet the one case worth reporting.
            nodes?.Add(new SchemaNode(file, at, field, token));

            // Asked before the kind, the way the panel drawing these asks it: a key the schema can only
            // call free json, and a condition nested inside another condition, are both answered by the
            // vocabulary and by nothing the schema holds.
            if (vocabulary?.Invoke(field) is { } binding)
            {
                Entries(file, binding, token, at, vocabulary, found, nodes);
                return;
            }

            switch (field.Kind)
            {
                case FieldKind.Reference when token is JValue { Value: not null }:
                    Named(file, at, kind, JsonScalars.Written(token), field.RefTargets, found);
                    break;

                case FieldKind.Object when field.Record is { } record:
                    Record(file, record, token, at, vocabulary, found, nodes);
                    break;

                case FieldKind.Array when field.Item is { } item && token is JArray array:
                    for (int index = 0; index < array.Count; index++)
                        Field(file, item, array[index], at.Append(index), ReferenceUseKind.ListItem, vocabulary, found, nodes);

                    break;

                case FieldKind.Dictionary when token is JObject map:
                    Map(file, field, map, at, vocabulary, found, nodes);
                    break;

                default:
                    break;
            }
        }

        /// <summary>A map, whose key may be a reference as much as its value: a pool keyed by the effects
        /// it may roll writes those ids nowhere else, and a rename that passed the keys over would leave
        /// the pool rolling effects nobody has.</summary>
        private static void Map(
            CatalogFile file,
            FieldSchema field,
            JObject map,
            JsonPointer at,
            FieldVocabulary? vocabulary,
            ICollection<ReferenceMention> found,
            ICollection<SchemaNode>? nodes)
        {
            foreach (JProperty pair in map.Properties())
            {
                // The address of a key is the address of what stands under it: that is the node whose key
                // a rename gives another word to, and the one place both can be named from.
                JsonPointer under = at.Append(pair.Name);

                if (field.Key is { Kind: FieldKind.Reference } key)
                    Named(file, under, ReferenceUseKind.MapKey, pair.Name, key.RefTargets, found);

                if (field.Item is { } item) Field(file, item, pair.Value, under, ReferenceUseKind.Value, vocabulary, found, nodes);
            }
        }

        /// <summary>One written word taken down where it stands. An id nothing answers is taken down like
        /// any other — a reference pointing at a record nobody wrote is exactly the place an author has to
        /// be able to find — while an empty one is not: it names nothing on purpose, and a field the schema
        /// gave nowhere to point is a word no catalog has a say over.</summary>
        private static void Named(
            CatalogFile file,
            JsonPointer at,
            ReferenceUseKind kind,
            string id,
            SchemaList<ReferenceTarget> targets,
            ICollection<ReferenceMention> found)
        {
            if (id.Length == 0 || targets.Count == 0) return;

            found.Add(new ReferenceMention(new ReferenceUse(file, at, kind), id, targets));
        }

        /// <summary>
        /// The fields a record standing in a file is read by: its own, and those of the shape it wears.
        /// <para>A record written in no shape the schema knows is read by every shape at once. The words
        /// are in the file whether or not it says which shape wrote them, and a rename that passed them
        /// over would leave the file naming a record nobody has any more.</para>
        /// <para>Where two shapes write one name over different things, nothing is vouched for: the name
        /// keeps its place and the walk reads nothing under it. Reading one shape's key by another's
        /// schema would rewrite a word the author never meant as an id.</para>
        /// </summary>
        private static IReadOnlyList<FieldSchema> Fields(RecordSchema record, JToken token) =>
            record.Variants is { } variants
                ? RecordFieldOrder.Merged(record, Shapes(variants, token), Clash)
                : record.Fields;

        private static IEnumerable<RecordSchema> Shapes(VariantSet variants, JToken token) =>
            RecordTemplates.Worn(variants, token) is { } worn
                ? [worn.Record]
                : variants.Variants.Select(variant => variant.Record);

        private static FieldSchema Clash(FieldSchema placed, FieldSchema brought) =>
            Alike(placed, brought) ? placed : Unread(placed.JsonName);

        /// <summary>Whether two shapes write the same thing under a name, as far as this walk is
        /// concerned: what stands there, what is inside it, and where a word in it may point.</summary>
        private static bool Alike(FieldSchema placed, FieldSchema brought) =>
            placed.Kind == brought.Kind
            && placed.Record == brought.Record
            && placed.Item == brought.Item
            && placed.Key == brought.Key
            && placed.RefTargets == brought.RefTargets;

        /// <summary>A name whose contents no shape can be held to. It keeps its place among the keys and
        /// nothing is read under it.</summary>
        private static FieldSchema Unread(string jsonName) => new() { JsonName = jsonName, Kind = FieldKind.Any };
    }
}
