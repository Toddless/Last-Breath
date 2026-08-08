namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;
    using Context;
    using Entity.Components;
    using Modifiers.Conditions;
    using Session;

    /// <summary>
    /// Owns the allocation and keeps both contribution channels — parameters and pipeline knobs — in step
    /// with it. The document is read lazily from the provider on every access: the data catalog is loaded
    /// after the container is built, and a reload hands out a fresh document that the allocation has to be
    /// re-checked against.
    /// The service is a singleton and outlives the scene, so it also owns the two ways a playthrough
    /// ends: a new game resets it, and a loaded file replaces the allocation wholesale.
    /// </summary>
    public sealed class PassiveTreeService : IPassiveTreeService, ISessionResettable
    {
        private readonly AllocationState _allocation = new();
        private readonly IPassiveTreeProvider _provider;
        private readonly PassiveTreeParameterSource _source;
        private readonly PassiveTreeContextSource _context;

        private PassiveTreeDocument? _synced;

        /// <summary>Only one channel is ever handed a fighter — the pipeline knobs are pushed into his
        /// handler while the parameters are pulled from a registered source — and the predicates of both
        /// read the state of that same fighter. The sighting is passed on here so the pulled channel does
        /// not need a second wiring of its own on every fighter that carries a tree.</summary>
        public PassiveTreeService(IPassiveTreeProvider provider, IConditionProvider conditions)
        {
            _provider = provider;
            _source = new PassiveTreeParameterSource(conditions);
            _context = new PassiveTreeContextSource(conditions);
            _context.OwnerChanged += _source.Follow;
        }

        public event Action? AllocationChanged;

        public PassiveTreeDocument Tree => SyncedTree();

        public IParameterModifierSource ParameterSource => _source;

        public IPassiveTreeContextSource ContextSource => _context;

        public IReadOnlyCollection<string> TakenNodes => SyncedAllocation().Taken;

        public int TotalPoints { get; private set; }

        public int SpentPoints => SyncedAllocation().Spent;

        public int AvailablePoints => Math.Max(0, TotalPoints - SpentPoints);

        /// <summary>A total below what is already spent leaves the allocation alone — nodes are never
        /// taken away behind the player's back, the next purchase simply has nothing to spend.</summary>
        public void SetTotalPoints(int points) => TotalPoints = Math.Max(0, points);

        public bool IsTaken(string nodeId) => SyncedAllocation().IsTaken(nodeId);

        public AllocationResult CheckTake(string nodeId) => _allocation.CheckTake(SyncedTree(), nodeId, TotalPoints);

        public AllocationResult CheckRefund(string nodeId) => _allocation.CheckRefund(SyncedTree(), nodeId);

        public IReadOnlyList<string> PathTo(string nodeId) => _allocation.PathTo(SyncedTree(), nodeId);

        public AllocationResult Take(string nodeId)
        {
            PassiveTreeDocument tree = SyncedTree();
            AllocationResult result = _allocation.TryTake(tree, nodeId, TotalPoints);
            if (result == AllocationResult.Success) Publish(tree);

            return result;
        }

        public AllocationResult TakePath(IReadOnlyList<string> route)
        {
            PassiveTreeDocument tree = SyncedTree();
            AllocationResult result = _allocation.TryTakePath(tree, route, TotalPoints);
            if (result == AllocationResult.Success) Publish(tree);

            return result;
        }

        public AllocationResult Refund(string nodeId)
        {
            PassiveTreeDocument tree = SyncedTree();
            AllocationResult result = _allocation.TryRefund(tree, nodeId);
            if (result == AllocationResult.Success) Publish(tree);

            return result;
        }

        public AllocationResult CheckRefundSet(IReadOnlyCollection<string> nodeIds) =>
            _allocation.CheckRefundAll(SyncedTree(), nodeIds);

        public AllocationResult RefundSet(IReadOnlyCollection<string> nodeIds)
        {
            PassiveTreeDocument tree = SyncedTree();
            AllocationResult result = _allocation.TryRefundAll(tree, nodeIds);
            if (result == AllocationResult.Success) Publish(tree);

            return result;
        }

        public void Respec()
        {
            PassiveTreeDocument tree = SyncedTree();
            _allocation.Reset(tree);
            Publish(tree);
        }

        public void RestoreState(IReadOnlyCollection<string> takenNodes)
        {
            PassiveTreeDocument tree = SyncedTree();
            if (tree.IsEmpty)
            {
                // Nothing to check the set against. It is held as it stands rather than dropped, so
                // the next capture writes back what the file carried instead of an empty section —
                // a tree that failed to load must not turn one bad launch into a permanent loss.
                // The check happens on the first document that does load.
                _allocation.Adopt(takenNodes);
                Publish(tree);
                return;
            }

            _allocation.Restore(tree, takenNodes);
            Publish(tree);
        }

        /// <summary>Back to the state a fresh character is in: no points granted, nothing taken but
        /// the free seeds, and the contribution of the old allocation gone from every fighter it
        /// reached. Mastery zeroes its own total in the same pass, so an allocation left standing
        /// here would be one the character can neither pay for nor undo.</summary>
        public void ResetSession()
        {
            SetTotalPoints(0);
            Respec();
        }

        /// <summary>The allocation after the document it is measured against has been adopted. Every
        /// read goes through here rather than touching the field: the catalog loads after the container
        /// is built, so an accessor that skipped the check would answer from a set that was never
        /// matched to the tree — an empty one, for a caller that never asked for <see cref="Tree"/>.</summary>
        private AllocationState SyncedAllocation()
        {
            SyncedTree();
            return _allocation;
        }

        /// <summary>
        /// Adopts the document the provider currently holds. Allocation survives a reload where it
        /// still makes sense and is dropped where it does not.
        /// A document without nodes is never adopted: that is what the reader hands out when the file
        /// fails to parse, and what the provider holds before the catalog is read at all. Adopting it
        /// would measure the allocation against nothing and wipe every node — so the last document
        /// that did load stays in force, and a broken file costs the player the tree's content for
        /// that launch instead of his allocation forever.
        /// </summary>
        private PassiveTreeDocument SyncedTree()
        {
            PassiveTreeDocument tree = _provider.Tree;
            if (ReferenceEquals(tree, _synced)) return tree;
            if (tree.IsEmpty) return _synced ?? tree;

            _synced = tree;
            _allocation.Resync(tree);
            Publish(tree);
            return tree;
        }

        /// <summary>The one place the allocation turns into a contribution. Both channels are rebuilt from
        /// the taken set together, so neither can be left holding what the other has already given up.</summary>
        private void Publish(PassiveTreeDocument tree)
        {
            _source.Rebuild(tree, _allocation.Taken);
            _context.Rebuild(tree, _allocation.Taken);
            AllocationChanged?.Invoke();
        }
    }
}
