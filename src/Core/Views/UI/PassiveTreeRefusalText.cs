namespace Core.Views.UI
{
    using PassiveTree;
    using PassiveTree.Allocation;

    /// <summary>
    /// Why a node could not be taken or given back, in one localisation key. The interface's own
    /// table and only its own: the developer console answers the same question in English prose with
    /// the commands to try next, and the two must not share a vocabulary — what they must not have is
    /// two readings of the RULE, and neither has one. Both are handed the verdict the allocation
    /// produced.
    /// </summary>
    public static class PassiveTreeRefusalText
    {
        private const string TakePrefix = "UI_PassiveTree_Refused_";
        private const string RefundPrefix = "UI_PassiveTree_Refund_";

        /// <summary>A node nothing reaches YET and a node with no links at all are the same verdict and
        /// two different problems: the first is a matter of buying the way there, the second is content
        /// the author has not finished wiring. Told apart by the one thing that separates them.</summary>
        private const string Unlinked = "Unlinked";

        public static string TakeKey(PassiveTreeDocument document, string nodeId, AllocationResult result) =>
            result == AllocationResult.NotConnected && document.Neighbours(nodeId).Count == 0
                ? $"{TakePrefix}{Unlinked}"
                : $"{TakePrefix}{result}";

        public static string RefundKey(AllocationResult result) => $"{RefundPrefix}{result}";
    }
}
