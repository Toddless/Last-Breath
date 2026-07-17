namespace Core.Enums
{
    /// <summary>Slot family of a rollable modifier line. None = authored/implicit lines outside the affix roll.</summary>
    public enum AffixKind : byte
    {
        None = 0,
        Prefix,
        Suffix
    }
}
