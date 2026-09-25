namespace Tooling.Schema.Model
{
    /// <summary>The catalog lives in one file and every record goes there.</summary>
    public sealed record SingleFilePlacement : FilePlacement
    {
        public required string FileName
        {
            get => field;
            init => field = SchemaGuard.Text(value, nameof(FileName));
        }

        public override string? FileFor(RecordFieldLookup lookup) => FileName;
    }
}
