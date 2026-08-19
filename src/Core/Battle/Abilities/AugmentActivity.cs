namespace Core.Battle.Abilities
{
    /// <summary>
    /// How much of what a seated augment offers is actually running. The ability answers it, because the
    /// ability is where two augments reaching for one parameter are held up against each other
    /// (<see cref="AbilityEffectIdentity"/>) — the interface only draws the answer.
    ///
    /// <para>Only what a record GIVES is counted. A record's own bill goes on being charged whether or
    /// not its gift lost, so counting the bill as a move that still runs would report a dormant augment
    /// as half-working and hide exactly the bad deal the player needs to see. Riders are not counted
    /// either: they are behaviour rather than a number, and behaviour has no rival to lose to.</para>
    /// </summary>
    public enum AugmentActivity
    {
        /// <summary>Everything it moves, it moves.</summary>
        Working,

        /// <summary>Some of its moves lost to a stronger augment on the same parameter, the rest run.</summary>
        Partly,

        /// <summary>Nothing it moves gets through: every one of its moves lost.</summary>
        Dormant
    }
}
