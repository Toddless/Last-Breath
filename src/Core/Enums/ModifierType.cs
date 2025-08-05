namespace Core.Enums
{
    public enum ModifierType
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
        /// Percent values should be 1.3, 1.1 etc.
        /// </summary>
        Multiplicative
    }
}
