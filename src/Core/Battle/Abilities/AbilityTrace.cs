namespace Core.Battle.Abilities
{
    using System;

    /// <summary>
    /// Which cast of which ability put a thing where it is. Stamped on an effect when it lands, so that
    /// a record offering to prolong or strengthen what its ability laid can tell that payload from the
    /// same payload laid by somebody else.
    ///
    /// <para><b>Three names, because "mine" is three questions.</b> <paramref name="AbilityId"/> is what
    /// the data and the design talk in and the only half worth printing. <paramref name="CastId"/> is
    /// the single firing, the vocabulary the replay already groups by. <paramref name="AbilityInstanceId"/>
    /// is WHOSE ability it was — an ability instance belongs to one fighter, so it is the half that keeps
    /// a poisoner from prolonging the poison of the other poisoner in the room, which the record id alone
    /// would not.</para>
    ///
    /// <para>Everything null is the honest answer for a passive, an item grant or a boss stage: nothing
    /// laid it, so nothing owns it, and it belongs to no ability's "mine". Domain mechanics — detonating,
    /// executing, spreading, transferring — deliberately ignore this and read the whole payload on the
    /// target; the trace is for prolonging and strengthening.</para>
    /// </summary>
    public readonly record struct AbilityTrace(string? AbilityId, string? CastId, string? AbilityInstanceId)
    {
        /// <summary>No ability behind it.</summary>
        public static readonly AbilityTrace None = default;

        /// <summary>Whether an ability is behind this at all.</summary>
        public bool IsFromAbility => !string.IsNullOrEmpty(AbilityInstanceId);

        /// <summary>Whether this came from that very ability — the one question the "only what is mine"
        /// rule asks. An untraced payload answers no to everything.</summary>
        public bool IsFrom(IAbility ability) =>
            IsFromAbility && string.Equals(AbilityInstanceId, ability.InstanceId, StringComparison.Ordinal);

        /// <summary>Whether both came out of the same firing of one ability.</summary>
        public bool IsFromSameCastAs(AbilityTrace other) =>
            !string.IsNullOrEmpty(CastId) && string.Equals(CastId, other.CastId, StringComparison.Ordinal);
    }
}
