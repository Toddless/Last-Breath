namespace Tooling.Schema.Model
{
    using System;

    /// <summary>One file per value of a field — equipment split by the slot it is worn in.</summary>
    /// <remarks>The value is handed back as the record wrote it; making a file name out of it is the
    /// writer's job, which is the only side that knows what its file system will take.</remarks>
    public sealed record FieldFilePlacement : FilePlacement
    {
        public required string FieldName
        {
            get => field;
            init => field = SchemaGuard.Text(value, nameof(FieldName));
        }

        public override string? FileFor(RecordFieldLookup lookup)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            string? value = lookup(FieldName);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
