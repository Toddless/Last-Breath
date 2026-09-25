namespace Core.Enums
{
    /// <summary>Slot family of a rollable modifier line. None = authored/implicit lines outside the affix roll.
    /// Mythic is a family of its OWN: the ascension gift occupies a dedicated slot that stands apart from the
    /// rarity's prefix/suffix count, so mythic pool entries are marked Mythic in data and never compete for a
    /// normal slot. The affix roll ignores them; only the ascender draws from a Mythic pool.</summary>
    public enum AffixKind : byte
    {
        None = 0,
        Prefix,
        Suffix,
        Mythic
    }
}
