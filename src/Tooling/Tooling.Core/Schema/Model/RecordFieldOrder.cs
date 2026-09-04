namespace Tooling.Schema.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>What to keep where a shape writes a name the record already declares: the field standing
    /// there, and the one the shape brings. The writer walks into what stands under a key and the
    /// inspector draws it, and those are two answers to one collision.</summary>
    public delegate FieldSchema FieldClash(FieldSchema placed, FieldSchema brought);

    /// <summary>
    /// The one order a record's keys stand in, whichever shape it is written in: the fields the record
    /// itself declares, in the order it declares them, with the fields only a shape brings placed where
    /// the shape is decided — right after the field whose value names it, or at the head of the record
    /// when the presence of a key is what names it.
    /// <para>A name both the record and a shape declare keeps the record's place. The record is the
    /// description every reader of the catalog meets, and a key that moved because some shape happens to
    /// write it too would move lines in a file nobody touched.</para>
    /// </summary>
    public static class RecordFieldOrder
    {
        /// <summary>The record's fields with those of <paramref name="shapes"/> merged in, in the order
        /// the file is written in. The shapes contribute in turn, each new name after the ones already
        /// placed, so the answer is the same however many times it is built.</summary>
        public static List<FieldSchema> Merged(RecordSchema record, IEnumerable<RecordSchema> shapes, FieldClash clash)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(shapes);
            ArgumentNullException.ThrowIfNull(clash);

            List<FieldSchema> merged = [.. record.Fields];
            int at = Anchor(merged, record.Variants?.Discriminator);

            foreach (RecordSchema shape in shapes)
                foreach (FieldSchema field in shape.Fields)
                {
                    int placed = IndexOf(merged, field.JsonName);

                    if (placed < 0) merged.Insert(at++, field);
                    else merged[placed] = clash(merged[placed], field);
                }

            return merged;
        }

        /// <summary>Where a field only a shape has goes: after the field whose value names the shape. With
        /// no such field the presence of a key is what names the shape, and those keys stand at the head —
        /// what a record IS is read before what every shape of it holds alike.</summary>
        private static int Anchor(List<FieldSchema> fields, string? discriminator)
        {
            if (discriminator is null) return 0;

            int at = IndexOf(fields, discriminator);

            return at < 0 ? 0 : at + 1;
        }

        private static int IndexOf(List<FieldSchema> fields, string jsonName) =>
            fields.FindIndex(field => string.Equals(field.JsonName, jsonName, StringComparison.Ordinal));
    }
}
