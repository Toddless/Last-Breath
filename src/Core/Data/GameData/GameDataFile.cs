namespace Core.Data.GameData
{
    /// <summary>One JSON document read from a data catalog.</summary>
    public sealed record GameDataFile(string FileName, string Json);
}
