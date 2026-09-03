namespace Tooling.Schema.Model
{
    /// <summary>One kind of record in a catalog: the DTO it was read from, the fields it is written
    /// with, the field carrying its id, and the shapes it may take.</summary>
    public sealed record RecordSchema
    {
        public required string TypeName
        {
            get => field;
            init => field = SchemaGuard.Text(value, nameof(TypeName));
        }

        public required SchemaList<FieldSchema> Fields { get; init; }

        /// <summary>Json name of the field holding this record's id; null when it has none — a
        /// settings document, a position at a table.</summary>
        public string? IdField { get; init; }

        /// <summary>The shapes this record may take; null when it has only the one.</summary>
        public VariantSet? Variants { get; init; }
    }
}
