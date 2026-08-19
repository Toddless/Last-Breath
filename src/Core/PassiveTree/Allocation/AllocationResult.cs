namespace Core.PassiveTree.Allocation
{
    /// <summary>Outcome of one allocation request. A refusal names its reason instead of collapsing into
    /// a bare false, so the caller can distinguish an unreachable node from an empty purse.</summary>
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
