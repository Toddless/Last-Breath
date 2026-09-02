namespace Tooling.Schema.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// The order a catalog's keys are written in: the order its DTOs declare them. An object is found by
    /// walking the schema down the pointer that addresses it — root, section, element, nested field — and
    /// its keys are ranked by where the record writes them.
    /// <para>A key the schema does not know, and every key of an object the walk cannot reach — a free map,
    /// a shape the schema has no record for — is <see cref="IKeyOrder.Unknown"/>, which leaves it where the
    /// file had it. That is the point of the whole thing: an editor built against one version of the game
    /// carries a newer file's keys through a save untouched.</para>
    /// <para>A record taking several shapes is ranked by all of them at once: the writer asks about a key,
    /// not about a value, so which shape stands there cannot be told at that moment. The shapes contribute
    /// their fields in turn, each new name after the ones already placed, and the record's own fields last;
    /// the first shape to write a name is also the one the walk goes on through. Where two shapes write one
    /// name over different things, the name keeps its place and the walk stops there — ranking the inside of
    /// one shape by the fields of another would move lines the author never touched.</para>
    /// </summary>
    public sealed class SchemaKeyOrder : IKeyOrder
    {
        /// <summary>Name of the record standing for the file's own root — the object whose keys are the
        /// catalog's sections. It is never shown: nothing but this class reads it.</summary>
        private const string RootTypeName = "catalog";

        private readonly Dictionary<RecordSchema, KeyLayout> _layouts = new(ReferenceEqualityComparer.Instance);
        private readonly FieldSchema _root;

        public SchemaKeyOrder(CatalogSchema catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _root = Root(catalog);
        }

        public int Rank(JsonPointer objectPointer, string key)
        {
            ArgumentNullException.ThrowIfNull(objectPointer);
            ArgumentNullException.ThrowIfNull(key);

            if (Locate(objectPointer) is not { Kind: FieldKind.Object, Record: { } record }) return IKeyOrder.Unknown;

            return Layout(record).Ranks.TryGetValue(key, out int rank) ? rank : IKeyOrder.Unknown;
        }

        private static FieldSchema Holder(RecordSchema record) =>
            new() { JsonName = FieldSchema.Unnamed, Kind = FieldKind.Object, Record = record };

        private static FieldSchema ListOf(RecordSchema record) =>
            new() { JsonName = FieldSchema.Unnamed, Kind = FieldKind.Array, Item = Holder(record) };

        /// <summary>The node standing for the whole file, so that every object in it is one walk from here.</summary>
        private static FieldSchema Root(CatalogSchema catalog) => catalog.Shape switch
        {
            RootShape.Single => Holder(catalog.Sections[0].Record),
            RootShape.Dictionary => new FieldSchema
            {
                JsonName = FieldSchema.Unnamed,
                Kind = FieldKind.Dictionary,
                Item = Holder(catalog.Sections[0].Record)
            },
            _ => Sections(catalog.Sections)
        };

        /// <summary>Sections are keys of the root object, in the order the catalog names them — unless the
        /// one section has no key at all, in which case the root IS the array of records.</summary>
        private static FieldSchema Sections(SchemaList<SectionSchema> sections)
        {
            if (sections.Count == 1 && sections[0].Key.Length == 0) return ListOf(sections[0].Record);

            SchemaList<FieldSchema> keys =
                [.. sections.Select(section => ListOf(section.Record) with { JsonName = section.Key })];

            return Holder(new RecordSchema { TypeName = RootTypeName, Fields = keys });
        }

        private FieldSchema? Locate(JsonPointer pointer)
        {
            FieldSchema node = _root;

            foreach (string segment in pointer.Segments)
            {
                if (Step(node, segment) is not { } child) return null;

                node = child;
            }

            return node;
        }

        private FieldSchema? Step(FieldSchema node, string segment) => node.Kind switch
        {
            FieldKind.Object when node.Record is { } record => Layout(record).Fields.GetValueOrDefault(segment),
            FieldKind.Array when JsonPointer.TryReadIndex(segment, out _) => node.Item,
            FieldKind.Dictionary => node.Item,
            _ => null
        };

        private KeyLayout Layout(RecordSchema record)
        {
            if (_layouts.TryGetValue(record, out KeyLayout? cached)) return cached;

            List<FieldSchema> merged = [];

            if (record.Variants is { } variants)
                foreach (VariantSchema variant in variants.Variants)
                    Merge(merged, variant.Record.Fields);

            Merge(merged, record.Fields);

            KeyLayout layout = new(merged);
            _layouts[record] = layout;

            return layout;
        }

        /// <summary>Adds the fields a shape brings that no earlier shape has placed. The first shape to
        /// write a name owns its place, so the order is the same however many times it is built; a name two
        /// shapes write different things under keeps the place and loses what is under it.</summary>
        private static void Merge(List<FieldSchema> merged, SchemaList<FieldSchema> fields)
        {
            foreach (FieldSchema field in fields)
            {
                int at = merged.FindIndex(placed => string.Equals(placed.JsonName, field.JsonName, StringComparison.Ordinal));

                if (at < 0) merged.Add(field);
                else if (!Alike(merged[at], field)) merged[at] = Anything(field.JsonName);
            }
        }

        /// <summary>Whether two shapes write the same thing under a name, as far as the walk is concerned:
        /// what stands there and what is inside it. Whether one of them requires the key, or starts it at
        /// some value, is no business of the order they are written in.</summary>
        private static bool Alike(FieldSchema placed, FieldSchema field) =>
            placed.Kind == field.Kind && placed.Record == field.Record && placed.Item == field.Item && placed.Key == field.Key;

        /// <summary>A name whose contents the schema cannot vouch for: it keeps its rank, and everything
        /// inside it keeps the order the file had.</summary>
        private static FieldSchema Anything(string jsonName) => new() { JsonName = jsonName, Kind = FieldKind.Any };

        /// <summary>One record's keys: where each stands, and what stands under it.</summary>
        private sealed class KeyLayout
        {
            public KeyLayout(List<FieldSchema> fields)
            {
                Ranks = new Dictionary<string, int>(fields.Count, StringComparer.Ordinal);
                Fields = new Dictionary<string, FieldSchema>(fields.Count, StringComparer.Ordinal);

                for (int rank = 0; rank < fields.Count; rank++)
                {
                    Ranks[fields[rank].JsonName] = rank;
                    Fields[fields[rank].JsonName] = fields[rank];
                }
            }

            public Dictionary<string, int> Ranks { get; }

            public Dictionary<string, FieldSchema> Fields { get; }
        }
    }
}
