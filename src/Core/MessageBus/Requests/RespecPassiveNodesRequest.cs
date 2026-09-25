namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using PassiveTree.Allocation;

    /// <summary>
    /// Gives a set of passive nodes back for gold, all-or-nothing. The tree and the wallet know nothing
    /// about each other and the operation has to settle both, so it is brought together here and nowhere
    /// else — a screen mirrors the answer, it never runs the order itself.
    /// </summary>
    public record RespecPassiveNodesRequest(IReadOnlyList<string> NodeIds) : IRequest<RespecResult>;

    /// <param name="Verdict">What the TREE said, and only the tree: it can be
    /// <see cref="AllocationResult.Success"/> beside an empty purse, which is the case where the rules
    /// allowed the respec and the player could not afford it.</param>
    /// <param name="NotEnoughGold">The tree would have allowed it and the purse did not. Kept apart from
    /// the verdict because <see cref="AllocationResult"/> is the vocabulary of the tree's own rules, and
    /// a wallet has no business being one of them — while the player has to be told which of the two
    /// stopped him.</param>
    /// <param name="GoldSpent">What it actually cost; zero on every refusal, since a refused respec is
    /// never paid for.</param>
    public readonly record struct RespecResult(AllocationResult Verdict, bool NotEnoughGold, int GoldSpent)
    {
        /// <summary>The nodes actually went back. Both halves have to have said yes — a rules verdict
        /// read on its own would report a respec nobody could pay for as one that happened.</summary>
        public bool Refunded => Verdict == AllocationResult.Success && !NotEnoughGold;
    }
}
