namespace Tooling.Schema.Model
{
    /// <summary>One keyed part of a catalog file and the records written under it; the key is empty
    /// when the section is the root itself.</summary>
    public sealed record SectionSchema
    {
        public required string Key
        {
            get => field;
            init => field = SchemaGuard.NotNull(value, nameof(Key));
        }

        public required RecordSchema Record
        {
            get => field;
            init => field = SchemaGuard.NotNull(value, nameof(Record));
        }
    }
}
