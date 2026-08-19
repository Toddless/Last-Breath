namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;
    using Context;
    using Entity.Components;
    using Modifiers.Conditions;
    using Session;

    /// <summary>Owns the allocation and keeps both contribution channels (parameters, pipeline knobs) in
    /// step with it. Reads the document lazily on every access, since the catalog loads after the
    /// container is built; a reload re-checks the allocation against the fresh document. A singleton
    /// outliving the scene, so it also owns session reset (new game) and wholesale load-file replace.</summary>
    public sealed class PassiveTreeService : IPassiveTreeService, ISessionResettable
    {
        private readonly AllocationState _allocation = new();
        private readonly IPassiveTreeProvider _provider;
        private readonly PassiveTreeParameterSource _source;
        private readonly PassiveTreeContextSource _context;

        private PassiveTreeDocument? _synced;

        /// <summary>Pipeline knobs are pushed into the fighter's handler while parameters are pulled from a
        /// registered source, but both channels' predicates read the same fighter — this relay spares the
        /// pulled channel a second wiring on every fighter that carries a tree.</summary>
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
                // Nothing to check against; held as-is so a failed load doesn't become a permanent loss.
                // Checked against the first document that does load.
                _allocation.Adopt(takenNodes);
                Publish(tree);
                return;
            }

            _allocation.Restore(tree, takenNodes);
            Publish(tree);
        }

        /// <summary>Back to a fresh character: no points, nothing but free seeds, old contribution gone
        /// from every fighter it reached. Mastery zeroes its own total in the same pass, so an allocation
        /// left standing here would be one the character can neither pay for nor undo.</summary>
        public void ResetSession()
        {
            SetTotalPoints(0);
            Respec();
        }

        /// <summary>The allocation after its document has been adopted; every read goes through here
        /// rather than the field, since a skipped check could answer from a set never matched to the tree.</summary>
        private AllocationState SyncedAllocation()
        {
            SyncedTree();
            return _allocation;
        }

        /// <summary>Adopts the provider's current document. An empty document (failed parse, or catalog
        /// not read yet) is never adopted — that would wipe the allocation against nothing — so the last
        /// document that did load stays in force; a broken file costs that launch's tree, not the allocation.</summary>
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

        /// <summary>The one place allocation turns into contribution; both channels rebuild together so
        /// neither is left holding what the other gave up.</summary>
        private void Publish(PassiveTreeDocument tree)
        {
            _source.Rebuild(tree, _allocation.Taken);
            _context.Rebuild(tree, _allocation.Taken);
            AllocationChanged?.Invoke();
        }
    }
}
