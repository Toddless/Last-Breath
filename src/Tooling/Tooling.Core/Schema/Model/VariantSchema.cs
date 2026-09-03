namespace Tooling.Schema.Model
{
    /// <summary>One shape a polymorphic record may take, together with the value that picks it.</summary>
    public sealed record VariantSchema
    {
        public required string DiscriminatorValue
        {
            get => field;
            init => field = SchemaGuard.Text(value, nameof(DiscriminatorValue));
        }

        public required RecordSchema Record
        {
            get => field;
            init => field = SchemaGuard.NotNull(value, nameof(Record));
        }
    }
}
