namespace Core.Battle.Abilities
{
    /// <summary>
    /// Which way a decorator leaves the ability parameter it stands on. WHAT it does and never how
    /// much: the amount is what one augment is worth against another, so telling two of them apart by
    /// it would make every roll an effect of its own.
    /// </summary>
    public enum AbilityEffectDirection : byte
    {
        /// <summary>Leaves the parameter higher than it found it.</summary>
        Raise,

        /// <summary>Leaves the parameter lower than it found it.</summary>
        Lower,

        /// <summary>Puts another value in its place. Categorical parameters — the resource a cast is
        /// paid in — are stored as numbers but are not quantities: nothing is more or less of them.</summary>
        Replace
    }
}
