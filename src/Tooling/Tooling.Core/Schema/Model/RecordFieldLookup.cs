namespace Tooling.Schema.Model
{
    /// <summary>Reads one field of the record being placed, by its json name; null when the record has
    /// no such field.</summary>
    public delegate string? RecordFieldLookup(string jsonName);
}
