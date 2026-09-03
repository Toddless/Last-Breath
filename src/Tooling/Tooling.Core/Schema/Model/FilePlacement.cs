namespace Tooling.Schema.Model
{
    /// <summary>Which file of a catalog folder a record is written to. A rule answers for itself, so a
    /// catalog that later splits its files a new way is a new rule here and nothing at the writer.</summary>
    public abstract record FilePlacement
    {
        /// <summary>The file's name without extension; null when the rule has no answer for this
        /// record and the tool is left to choose.</summary>
        public abstract string? FileFor(RecordFieldLookup lookup);
    }
}
