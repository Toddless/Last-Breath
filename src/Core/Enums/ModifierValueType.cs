namespace Core.Enums
{
    public enum ModifierValueType : byte
    {
        /// <summary>
        /// Raw value. Example: +300 HP, +15 Def etc.
        /// </summary>
        Flat = 0,
        /// <summary>
        /// Percent value. Values should be 0.1, 0.3 etc
        /// </summary>
        Increase,
        /// <summary>
        /// Percent values. Values should be 0.3, 0.1 etc.
        /// </summary>
        Multiplicative,
        /// <summary>
        /// A switch, not a number ("attacks ignore elemental resistances"): the value channel is pinned to 1,
        /// nothing rolls it and no multiplier touches it (sharpening and ascension leave it alone). Legal on
        /// context lines only — parameter math has no meaning for it.
        /// </summary>
        Flag
    }
}
