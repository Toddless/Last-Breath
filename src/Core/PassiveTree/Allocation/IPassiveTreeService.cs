namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;
    using Context;
    using Entity.Components;

    /// <summary>
    /// The character's passive-tree allocation: which nodes are taken, what they cost, and the
    /// parametric contribution that follows from them. Points arrive from outside — the service
    /// accounts for them, it does not award them.
    /// </summary>
    public interface IPassiveTreeService
    {
        /// <summary>Raised after the taken set changed by any route: a purchase, a refund, a respec
        /// or the tree document being (re)loaded underneath.</summary>
        event Action? AllocationChanged;

        /// <summary>The tree the allocation is measured against; empty until the catalog is loaded.</summary>
        PassiveTreeDocument Tree { get; }

        /// <summary>The contribution of the taken nodes, ready to be registered on a fighter's
        /// modifier component. Registering it is the only channel — nothing writes tree lines into
        /// the entity's own modifier list.</summary>
        IParameterModifierSource ParameterSource { get; }

        /// <summary>The other half of the contribution: the pipeline knobs the taken nodes tune, folded to
        /// one modifier per knob. Reaches a fighter by being attached to it rather than by being registered
        /// — context modifiers live in the fighter's own handler — so whoever attaches it detaches it.</summary>
        IPassiveTreeContextSource ContextSource { get; }

        IReadOnlyCollection<string> TakenNodes { get; }

        /// <summary>Points granted to the character so far.</summary>
        int TotalPoints { get; }

        int SpentPoints { get; }

        int AvailablePoints { get; }

        /// <summary>Accepts the point total granted from outside (mastery levels).</summary>
        void SetTotalPoints(int points);

        bool IsTaken(string nodeId);

        /// <summary>The answer <see cref="Take"/> would give, without buying anything — what a canvas
        /// or a console asks to show whether a node is available and why it is not.</summary>
        AllocationResult CheckTake(string nodeId);

        /// <summary>The answer <see cref="Refund"/> would give, without giving anything back — the
        /// mirror of <see cref="CheckTake"/>, and what a canvas asks to grey out a button and name the
        /// reason before the click rather than after it.</summary>
        AllocationResult CheckRefund(string nodeId);

        /// <summary>The cheapest route from the current allocation to a node, the nodes still to be
        /// bought and in the order they would be bought. Empty when the node is already taken or
        /// nothing reaches it. Its length is what reaching the node costs — the only place a price
        /// bigger than one point exists, since a single node always costs one.</summary>
        IReadOnlyList<string> PathTo(string nodeId);

        AllocationResult Take(string nodeId);

        /// <summary>Buys a whole route at once, all-or-nothing, naming the first refusal. What
        /// <see cref="PathTo"/> hands back goes straight in.</summary>
        AllocationResult TakePath(IReadOnlyList<string> route);

        AllocationResult Refund(string nodeId);

        /// <summary>
        /// The answer <see cref="RefundSet"/> would give, without giving anything back. Asked of the
        /// whole set rather than of each node, because those are different questions: on a chain
        /// seed—A—B—C the pair {B, C} is legal while B on its own strands C, so a set checked node by
        /// node would be refused for a break it repairs itself.
        /// </summary>
        AllocationResult CheckRefundSet(IReadOnlyCollection<string> nodeIds);

        /// <summary>Gives a whole set back at once, all-or-nothing. The one road for a planned respec:
        /// the price of one is charged for the set, so a set half given back would leave the character
        /// paid up and still holding nodes.</summary>
        AllocationResult RefundSet(IReadOnlyCollection<string> nodeIds);

        /// <summary>Drops the whole allocation back to the granted seeds.</summary>
        void Respec();

        /// <summary>Replaces the whole allocation with a saved one. Wholesale rather than node by
        /// node: a set replayed through purchases would depend on the order it was written in. The
        /// set is re-checked against the current tree, so nodes that stopped existing or stopped
        /// being connected do not come back — unless there is no tree to check against
        /// (<see cref="PassiveTreeDocument.IsEmpty"/>), in which case the set is held as it stands
        /// until a document loads. Points are not part of it — the granted total belongs to mastery
        /// and is restored before this.</summary>
        void RestoreState(IReadOnlyCollection<string> takenNodes);
    }
}
