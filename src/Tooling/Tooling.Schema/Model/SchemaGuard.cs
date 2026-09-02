namespace Tooling.Schema.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>The checks the schema records make on what they are handed, in one place so every
    /// door into a schema refuses the same things.</summary>
    internal static class SchemaGuard
    {
        public static string Text(string value, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
            return value;
        }

        public static T NotNull<T>(T value, string name) where T : class
        {
            ArgumentNullException.ThrowIfNull(value, name);
            return value;
        }

        public static SchemaList<T> NotEmpty<T>(SchemaList<T> items, string name) =>
            items.Count > 0 ? items : throw new ArgumentException($"'{name}' must hold at least one entry.", name);

        /// <summary>A range that ends below where it starts accepts nothing at all.</summary>
        public static double Ordered(double min, double max) => max >= min
            ? max
            : throw new ArgumentOutOfRangeException(nameof(max), max, $"A range cannot end ({max}) below where it starts ({min}).");

        public static NumericRange? Ordered(NumericRange? range)
        {
            if (range is { } value) Ordered(value.Min, value.Max);
            return range;
        }

        /// <summary>Sections must be there and must not share a key: two under one key means one of
        /// them is never written to the file.</summary>
        public static SchemaList<SectionSchema> Sections(SchemaList<SectionSchema> sections)
        {
            NotEmpty(sections, nameof(sections));

            HashSet<string> keys = new(StringComparer.Ordinal);
            foreach (SectionSchema section in sections)
                if (!keys.Add(section.Key))
                    throw new ArgumentException($"Two sections are written under the key '{section.Key}'.", nameof(sections));

            return sections;
        }

        /// <summary>A file that is one record, or one map of records, has one section to put them in.</summary>
        public static void Holds(RootShape shape, SchemaList<SectionSchema> sections)
        {
            if (shape is (RootShape.Single or RootShape.Dictionary) && sections.Count > 1)
                throw new ArgumentException($"A {shape} catalog holds one section, not {sections.Count}.", nameof(sections));
        }

        /// <summary>
        /// The field telling the shapes apart has to be on the shapes themselves — named by
        /// <paramref name="discriminator"/>, or, with no discriminator, by each variant's own value.
        /// A variant that does not carry it cannot be recognised in a file or written back to one.
        /// </summary>
        public static void Agree(SchemaList<VariantSchema> variants, string? discriminator)
        {
            foreach (VariantSchema variant in variants)
            {
                string field = discriminator ?? variant.DiscriminatorValue;
                if (Names(variant.Record, field)) continue;

                throw new ArgumentException(
                    $"The variant '{variant.DiscriminatorValue}' has no field '{field}' to be told apart by.",
                    nameof(variants));
            }
        }

        private static bool Names(RecordSchema record, string field)
        {
            foreach (FieldSchema candidate in record.Fields)
                if (string.Equals(candidate.JsonName, field, StringComparison.Ordinal))
                    return true;

            return false;
        }
    }
}
