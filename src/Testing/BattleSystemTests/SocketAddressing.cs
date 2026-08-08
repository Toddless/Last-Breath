namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;

    /// <summary>
    /// Turns the node id a walk names into the address the board keys by. The walks speak of nodes
    /// because that is what a passive tree hands out; the board keys by the whole signature, so that a
    /// slot a refunded node left holding an augment can sit beside the live slot the same node opens
    /// after being repointed.
    /// </summary>
    internal static class SocketAddressing
    {
        /// <summary>The address of the slot that node opened. A node the board knows nothing about
        /// answers with the id itself, which is an address no slot has — exactly what a walk asserting
        /// that no such slot exists is asking about.</summary>
        internal static string At(this IAbilitySocketBoard board, string socketId) =>
            board.Sockets.FirstOrDefault(socket => string.Equals(socket.SocketId, socketId, StringComparison.Ordinal))
                ?.Address ?? socketId;
    }
}
