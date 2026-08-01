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
    public readonly record struct AbilitySocketPlacement(string SocketId, string AbilityId, int Tier);
}
