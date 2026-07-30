namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;
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

        IReadOnlyCollection<string> TakenNodes { get; }

        /// <summary>Points granted to the character so far.</summary>
        int TotalPoints { get; }

        int SpentPoints { get; }

        int AvailablePoints { get; }

        /// <summary>Accepts the point total granted from outside (mastery levels).</summary>
        void SetTotalPoints(int points);

        bool IsTaken(string nodeId);

        AllocationResult Take(string nodeId);

        AllocationResult Refund(string nodeId);

        /// <summary>Drops the whole allocation back to the granted seeds.</summary>
        void Respec();
    }
}
