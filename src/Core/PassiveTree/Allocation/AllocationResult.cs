namespace Core.PassiveTree.Allocation
{
    /// <summary>
    /// Outcome of one allocation request. A refusal names its own reason instead of collapsing into a
    /// false, so the caller can say what went wrong — an unreachable node and an empty purse are not
    /// the same problem to the player.
    /// </summary>
    public enum AllocationResult
    {
        Success,

        /// <summary>No node with that id in the current tree.</summary>
        UnknownNode,

        AlreadyTaken,

        NotTaken,

        /// <summary>Seeds come with the character: they are never bought and never given back.</summary>
        Granted,

        NotEnoughPoints,

        /// <summary>Nothing already taken touches the node — a node with no links at all lands here.</summary>
        NotConnected,

        /// <summary>Giving the node back would leave the branch behind it hanging in the air.</summary>
        WouldOrphan
    }
}
