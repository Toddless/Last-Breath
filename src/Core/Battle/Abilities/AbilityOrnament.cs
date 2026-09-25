namespace Core.Battle.Abilities
{
    /// <summary>
    /// One ornament worn by one ability: the fourth socket. Where the tree's slots are re-read from the
    /// allocation on every load, this is a decision of the player's that nothing else can reproduce —
    /// which is why the board owns it beside the occupants rather than being handed it with the
    /// placements.
    /// </summary>
    /// <param name="OrnamentId">The ornament worn. It is also the granted slot's
    /// <see cref="AbilitySocketPlacement.SocketId"/>: a slot is named after whatever opened it, and here
    /// that is a thing rather than a node.</param>
    /// <param name="AbilityId">The ability wearing it. At most one ornament to an ability — the rule
    /// that keeps the fourth socket a fourth socket instead of a sixth.</param>
    /// <param name="Tier">Which tier the granted socket accepts, off the ornament's record. Carried
    /// rather than looked up, so the board never has to read data to know what it is holding.</param>
    public readonly record struct AbilityOrnament(string OrnamentId, string AbilityId, int Tier)
    {
        /// <summary>The slot the ornament grants, said the way every other slot is said. An honest
        /// address and not a special case: two ornaments cannot name one slot because two ornaments
        /// cannot be one id, and no ornament can collide with a tree node unless a node is named after
        /// one.</summary>
        public AbilitySocketPlacement Slot => new(OrnamentId, AbilityId, Tier);
    }
}
