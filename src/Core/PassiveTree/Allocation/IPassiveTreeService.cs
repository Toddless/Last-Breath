namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;
    using Context;
    using Entity.Components;

    /// <summary>The character's passive-tree allocation: which nodes are taken, what they cost, and the
    /// parametric contribution that follows. Points arrive from outside — this accounts for them, not awards them.</summary>
    public interface IPassiveTreeService
    {
        /// <summary>Raised after the taken set changed by any route: a purchase, a refund, a respec
        /// or the tree document being (re)loaded underneath.</summary>
        event Action? AllocationChanged;

        /// <summary>The tree the allocation is measured against; empty until the catalog is loaded.</summary>
        PassiveTreeDocument Tree { get; }

        /// <summary>The contribution of the taken nodes, registered on a fighter's modifier component —
        /// the only channel; nothing writes tree lines into the entity's own modifier list.</summary>
        IParameterModifierSource ParameterSource { get; }

        /// <summary>The other half: pipeline knobs the taken nodes tune, folded to one modifier per knob.
        /// Reaches a fighter by being attached, not registered — whoever attaches it detaches it.</summary>
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

        /// <summary>The answer <see cref="Refund"/> would give, without giving anything back — mirror
        /// of <see cref="CheckTake"/>, for greying out a button and naming the reason before the click.</summary>
        AllocationResult CheckRefund(string nodeId);

        /// <summary>Cheapest route to a node, in buy order; empty when already taken or unreachable.
        /// Its length is the only place a price bigger than one point exists.</summary>
        IReadOnlyList<string> PathTo(string nodeId);

        AllocationResult Take(string nodeId);

        /// <summary>Buys a whole route at once, all-or-nothing, naming the first refusal. What
        /// <see cref="PathTo"/> hands back goes straight in.</summary>
        AllocationResult TakePath(IReadOnlyList<string> route);

        AllocationResult Refund(string nodeId);

        /// <summary>The answer <see cref="RefundSet"/> would give, without giving anything back — asked
        /// of the whole set, not node by node: on seed—A—B—C, {B,C} is legal while B alone strands C.</summary>
        AllocationResult CheckRefundSet(IReadOnlyCollection<string> nodeIds);

        /// <summary>Gives a whole set back at once, all-or-nothing — the road for a planned respec, so a
        /// half-refund never leaves the character paid up and still holding nodes.</summary>
        AllocationResult RefundSet(IReadOnlyCollection<string> nodeIds);

        /// <summary>Drops the whole allocation back to the granted seeds.</summary>
        void Respec();

        /// <summary>Replaces the whole allocation with a saved one, wholesale rather than replayed as
        /// purchases (order-dependent). Re-checked against the current tree, so dead/disconnected nodes
        /// drop out — unless the tree is empty (<see cref="PassiveTreeDocument.IsEmpty"/>), where the set
        /// is held as-is until one loads. Points are not part of it; mastery restores its total first.</summary>
        void RestoreState(IReadOnlyCollection<string> takenNodes);
    }
}
