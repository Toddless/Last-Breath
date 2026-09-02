namespace Tooling.Schema.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Tooling.Schema.Model;

    /// <summary>
    /// Builds the schema of one catalog: the descriptor states the shape of the file, the reflector reads
    /// the records off the DTOs, and what neither of them can check on its own is checked here.
    /// <para>The schema model refuses what it can refuse where it is built — a section without a key, a
    /// variant that cannot be told apart. What it cannot see is agreement between the parts a descriptor
    /// assembles: a record naming an id field it does not have, a catalog split by a field no record
    /// writes, shapes registered for a type the walk never met. Those are reported, never thrown: a schema
    /// the tool can still draw is worth more than a tool that will not open.</para>
    /// <para>One builder describes one catalog: both reports gather everything said since it was made, and
    /// a reflector shared between catalogs carries the shapes registered for the earlier ones into the
    /// later — which is only right when the caller means to describe them as one body of types.</para>
    /// </summary>
    public sealed class CatalogSchemaBuilder
    {
        private readonly SchemaReflector _reflector;

        public CatalogSchemaBuilder() : this(new SchemaReflector())
        {
        }

        public CatalogSchemaBuilder(SchemaReflector reflector)
        {
            ArgumentNullException.ThrowIfNull(reflector);

            _reflector = reflector;
        }

        /// <summary>What reflection could not answer while the descriptors were read.</summary>
        public SchemaReflectionReport Reflection => _reflector.Report;

        /// <summary>Where the parts of an assembled catalog disagree with each other.</summary>
        public SchemaReflectionReport Checks { get; } = new();

        public CatalogSchema Build(ICatalogDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);

            if (string.IsNullOrWhiteSpace(descriptor.Catalog)) Note(Notes.Nameless, descriptor.GetType().Name);

            CatalogSchema schema = descriptor.Describe(_reflector);

            Check(schema, descriptor.Catalog);

            return schema;
        }

        private static bool Names(RecordSchema record, string jsonName)
        {
            foreach (FieldSchema field in record.Fields)
                if (string.Equals(field.JsonName, jsonName, StringComparison.Ordinal))
                    return true;

            return false;
        }

        /// <summary>Every record the catalog reaches, once each: the records of its sections, the shapes
        /// those take, and everything nested in their fields. A field that leads back to a record already
        /// being read carries no record of its own, so the walk always ends.</summary>
        private static IEnumerable<RecordSchema> Records(CatalogSchema schema)
        {
            HashSet<RecordSchema> seen = new(ReferenceEqualityComparer.Instance);
            Stack<RecordSchema> pending = new();

            foreach (SectionSchema section in schema.Sections) pending.Push(section.Record);

            while (pending.Count > 0)
            {
                RecordSchema record = pending.Pop();

                if (!seen.Add(record)) continue;

                yield return record;

                if (record.Variants is { } variants)
                    foreach (VariantSchema variant in variants.Variants)
                        pending.Push(variant.Record);

                foreach (FieldSchema field in record.Fields)
                    foreach (RecordSchema nested in Nested(field))
                        pending.Push(nested);
            }
        }

        private static IEnumerable<RecordSchema> Nested(FieldSchema field)
        {
            if (field.Record is { } record) yield return record;

            if (field.Item is { } item)
                foreach (RecordSchema nested in Nested(item))
                    yield return nested;

            if (field.Key is { } key)
                foreach (RecordSchema nested in Nested(key))
                    yield return nested;
        }

        private void Check(CatalogSchema schema, string catalog)
        {
            foreach (RecordSchema record in Records(schema))
                if (record.IdField is { } id && !Names(record, id))
                    Note(Notes.NoIdField, catalog, record.TypeName, id);

            // Shapes are registered against a type by name. A misspelt one registers shapes nothing wears,
            // and the schema comes out with one record where the file has several — with nothing amiss in it.
            foreach (Type shaped in _reflector.ShapesNeverMet)
                Note(Notes.ShapesNeverMet, catalog, shaped.Name);

            if (schema.Placement is not FieldFilePlacement placement) return;

            foreach (SectionSchema section in schema.Sections)
                if (Shapes(section.Record).Exists(shape => Names(shape, placement.FieldName)))
                    return;

            Note(Notes.NoPlacementField, catalog, placement.FieldName);
        }

        /// <summary>A record and the shapes it may take: a field naming the file belongs to whichever of
        /// them a record actually written to disk turns out to be.</summary>
        private static List<RecordSchema> Shapes(RecordSchema record)
        {
            List<RecordSchema> shapes = [record];

            if (record.Variants is { } variants)
                foreach (VariantSchema variant in variants.Variants)
                    shapes.Add(variant.Record);

            return shapes;
        }

        private void Note(string format, params object?[] parts) =>
            Checks.Note(string.Format(CultureInfo.InvariantCulture, format, parts));

        private static class Notes
        {
            public const string Nameless = "'{0}' describes a catalog it does not name.";
            public const string NoIdField = "{0}: '{1}' says its id is written under '{2}', which is not one of its fields.";
            public const string NoPlacementField = "{0}: the catalog is split into files by '{1}', which none of its records writes.";
            public const string ShapesNeverMet = "{0}: shapes are registered for '{1}', which nothing in the catalog holds.";
        }
    }
}
