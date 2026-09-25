namespace Core.Battle.Abilities
{
    /// <summary>
    /// One socket the character's allocation currently opens: where it lives and what it accepts.
    /// The description of a slot, not the slot itself — <see cref="IAbilitySocketBoard.Sync"/> turns
    /// a set of these into the live sockets.
    /// </summary>
    /// <param name="SocketId">Identity of the slot; the id of the node that opened it.</param>
    /// <param name="AbilityId">The ability the slot belongs to.</param>
    /// <param name="Tier">Which tier of augment the slot accepts.</param>
    public readonly record struct AbilitySocketPlacement(string SocketId, string AbilityId, int Tier)
    {
        private const char Separator = '|';

        /// <summary>
        /// The whole signature as one string — how a slot is named everywhere outside the board: in a
        /// request, in a view, in the console. A node id alone would not do it: a build is free to point
        /// that node at another ability or another tier, and the slot it used to open may still be
        /// holding an augment chosen under the old signature while the new one stands beside it. Two
        /// signatures are two slots, so two addresses are two keys.
        /// Built here and only here, and never taken apart again: whoever holds one carries it back to
        /// the board unchanged, which is what keeps the shape of it the board's own business.
        /// </summary>
        public string Address => $"{SocketId}{Separator}{AbilityId}{Separator}{Tier}";
    }
}
