namespace Tooling.Schema.Model
{
    /// <summary>Nothing about a record says where it goes: the tool splits the catalog as it likes.</summary>
    public sealed record FreeFilePlacement : FilePlacement
    {
        public override string? FileFor(RecordFieldLookup lookup) => null;
    }
}
