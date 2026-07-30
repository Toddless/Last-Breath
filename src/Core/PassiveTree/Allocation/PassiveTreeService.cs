namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;
    using Entity.Components;

    /// <summary>
    /// Owns the allocation and keeps the modifier source in step with it. The document is read lazily
    /// from the provider on every access: the data catalog is loaded after the container is built, and
    /// a reload hands out a fresh document that the allocation has to be re-checked against.
    /// </summary>
    public sealed class PassiveTreeService(IPassiveTreeProvider provider) : IPassiveTreeService
    {
        private readonly AllocationState _allocation = new();
        private readonly PassiveTreeParameterSource _source = new();

        private PassiveTreeDocument? _synced;

        public event Action? AllocationChanged;

        public PassiveTreeDocument Tree => SyncedTree();

        public IParameterModifierSource ParameterSource => _source;

        public IReadOnlyCollection<string> TakenNodes => SyncedAllocation().Taken;

        public int TotalPoints { get; private set; }

        public int SpentPoints => SyncedAllocation().Spent;

        public int AvailablePoints => Math.Max(0, TotalPoints - SpentPoints);

        /// <summary>A total below what is already spent leaves the allocation alone — nodes are never
        /// taken away behind the player's back, the next purchase simply has nothing to spend.</summary>
        public void SetTotalPoints(int points) => TotalPoints = Math.Max(0, points);

        public bool IsTaken(string nodeId) => SyncedAllocation().IsTaken(nodeId);

        public AllocationResult CheckTake(string nodeId) => _allocation.CheckTake(SyncedTree(), nodeId, TotalPoints);

        public AllocationResult Take(string nodeId)
        {
            PassiveTreeDocument tree = SyncedTree();
            AllocationResult result = _allocation.TryTake(tree, nodeId, TotalPoints);
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

        public void Respec()
        {
            PassiveTreeDocument tree = SyncedTree();
            _allocation.Reset(tree);
            Publish(tree);
        }

        public void RestoreState(IReadOnlyCollection<string> takenNodes)
        {
            PassiveTreeDocument tree = SyncedTree();
            _allocation.Restore(tree, takenNodes);
            Publish(tree);
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

        /// <summary>Adopts the document the provider currently holds. Allocation survives a reload
        /// where it still makes sense and is dropped where it does not.</summary>
        private PassiveTreeDocument SyncedTree()
        {
            PassiveTreeDocument tree = provider.Tree;
            if (ReferenceEquals(tree, _synced)) return tree;

            _synced = tree;
            _allocation.Resync(tree);
            Publish(tree);
            return tree;
        }

        private void Publish(PassiveTreeDocument tree)
        {
            _source.Rebuild(tree, _allocation.Taken);
            AllocationChanged?.Invoke();
        }
    }
}
