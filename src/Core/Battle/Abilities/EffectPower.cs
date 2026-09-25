namespace Core.Battle.Abilities
{
    /// <summary>
    /// How firmly an effect holds against dispelling. A dispel takes everything at or below its own
    /// strength, so a weak dispel takes only weak effects and a strong one takes strong and weak alike;
    /// absolute effects (seals) are never taken. Unrelated to <c>Core.Enums.Priority</c>, which orders
    /// parameter decorators.
    /// </summary>
    public enum EffectPower : byte
    {
        /// <summary>What an effect is unless the canon says otherwise.</summary>
        Weak = 0,
        Strong,

        /// <summary>Beyond any dispel.</summary>
        Absolute
    }
}
