namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;

    /// <summary>Which nodes are taken, what that costs, and what may still be taken or refunded — sole
    /// enforcer that a node must touch something already taken and a refund may not orphan anything.</summary>
    public sealed class AllocationState
    {
        private readonly HashSet<string> _taken = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Taken => _taken;

        /// <summary>Points actually spent: the seeds are granted, so they never count.</summary>
        public int Spent { get; private set; }

        public bool IsTaken(string id) => _taken.Contains(id);

        /// <summary>Drops allocation back to the free seeds. Also the way to recover after the tree
        /// itself changed under a running simulation.</summary>
        public void Reset(PassiveTreeDocument document)
        {
            _taken.Clear();
            Spent = 0;

            foreach (PassiveNode node in document.Nodes)
                if (node.Kind == PassiveNodeKind.Start)
                    _taken.Add(node.Id);
        }

        /// <summary>Replaces the whole set from a save file, wholesale rather than node by node, then
        /// re-checks it like an edited tree: unknown ids and anything with no route to a seed drop out.</summary>
        public void Restore(PassiveTreeDocument document, IEnumerable<string> nodes)
        {
            _taken.Clear();
            _taken.UnionWith(nodes);
            Resync(document);
        }

        /// <summary>Takes the set as-is when no tree exists yet to check it against (a restore that beat
        /// the catalog to load) — nothing dropped, spend left stale until the next <see cref="Resync"/>.</summary>
        public void Adopt(IEnumerable<string> nodes)
        {
            _taken.Clear();
            _taken.UnionWith(nodes);
        }

        /// <summary>Drops anything that stopped existing or stopped being reachable after an edit, and
        /// recounts the spend from what is left — what a dropped node cost is no longer owed.</summary>
        public void Resync(PassiveTreeDocument document)
        {
            foreach (string id in new List<string>(_taken))
                if (!document.Contains(id))
                    _taken.Remove(id);

            foreach (PassiveNode node in document.Nodes)
                if (node.Kind == PassiveNodeKind.Start)
                    _taken.Add(node.Id);

            _taken.IntersectWith(Reachable(document, _taken));

            Recount(document);
        }

        /// <summary>Whether the node can be bought right now, and why not when it cannot.</summary>
        public AllocationResult CheckTake(PassiveTreeDocument document, string id, int budget)
        {
            PassiveNode? node = document.Find(id);
            if (node is null) return AllocationResult.UnknownNode;
            if (_taken.Contains(id)) return AllocationResult.AlreadyTaken;
            if (!NodeKindRules.CostsPoint(node.Kind)) return AllocationResult.Granted;
            if (Spent >= budget) return AllocationResult.NotEnoughPoints;

            foreach (string neighbour in document.Neighbours(id))
                if (_taken.Contains(neighbour))
                    return AllocationResult.Success;

            return AllocationResult.NotConnected;
        }

        /// <summary>Buys the node when the rules allow it; returns the same reason <see cref="CheckTake"/> would.</summary>
        public AllocationResult TryTake(PassiveTreeDocument document, string id, int budget)
        {
            AllocationResult result = CheckTake(document, id, budget);
            if (result != AllocationResult.Success) return result;

            _taken.Add(id);

            // Unconditional because CheckTake already refused every class that costs no point.
            Spent++;
            return result;
        }

        public bool Take(PassiveTreeDocument document, string id, int budget) =>
            TryTake(document, id, budget) == AllocationResult.Success;

        /// <summary>Whether the node can go back, and why not: legal only if every still-taken node
        /// still reaches a seed once it's gone — unwinding happens from the ends.</summary>
        public AllocationResult CheckRefund(PassiveTreeDocument document, string id)
        {
            PassiveNode? node = document.Find(id);
            if (node is null) return AllocationResult.UnknownNode;
            if (!_taken.Contains(id)) return AllocationResult.NotTaken;
            if (!NodeKindRules.CostsPoint(node.Kind)) return AllocationResult.Granted;

            var remaining = new HashSet<string>(_taken, StringComparer.Ordinal);
            remaining.Remove(id);

            return Reachable(document, remaining).Count == remaining.Count
                ? AllocationResult.Success
                : AllocationResult.WouldOrphan;
        }

        /// <summary>Whether the whole set can go back AT ONCE — not decomposable into per-node checks
        /// (on seed—A—B—C, {B,C} is legal while B alone strands C). Checks the state that would be LEFT,
        /// not each removal in turn. Empty set is <see cref="AllocationResult.NotTaken"/>, never a free success.</summary>
        public AllocationResult CheckRefundAll(PassiveTreeDocument document, IReadOnlyCollection<string> ids)
        {
            if (ids.Count == 0) return AllocationResult.NotTaken;

            foreach (string id in ids)
            {
                PassiveNode? node = document.Find(id);
                if (node is null) return AllocationResult.UnknownNode;
                if (!_taken.Contains(id)) return AllocationResult.NotTaken;
                if (!NodeKindRules.CostsPoint(node.Kind)) return AllocationResult.Granted;
            }

            var remaining = new HashSet<string>(_taken, StringComparer.Ordinal);
            remaining.ExceptWith(ids);

            return Reachable(document, remaining).Count == remaining.Count
                ? AllocationResult.Success
                : AllocationResult.WouldOrphan;
        }

        /// <summary>Gives the whole set back when allowed, same reason as <see cref="CheckRefundAll"/>.
        /// All-or-nothing with no rollback needed — the only possible refusal is checked before anything is removed.</summary>
        public AllocationResult TryRefundAll(PassiveTreeDocument document, IReadOnlyCollection<string> ids)
        {
            AllocationResult result = CheckRefundAll(document, ids);
            if (result != AllocationResult.Success) return result;

            int before = _taken.Count;
            _taken.ExceptWith(ids);

            // What left the set is what was paid for: every id was verified to be taken and to cost a
            // point, and counting the difference rather than the argument survives a set listing one
            // node twice.
            Spent -= before - _taken.Count;
            return result;
        }

        /// <summary>The node plus everything that would be left hanging without it, ordered leaves-inward
        /// so the list is also a safe removal order. An unknown/not-taken/granted node comes back alone —
        /// <see cref="CheckRefundAll"/> gives the verdict on that, since an empty list would misreport a seed as NotTaken.</summary>
        public List<string> RefundClosure(PassiveTreeDocument document, string id)
        {
            PassiveNode? node = document.Find(id);
            if (node is null || !_taken.Contains(id) || !NodeKindRules.CostsPoint(node.Kind)) return [id];

            var remaining = new HashSet<string>(_taken, StringComparer.Ordinal);
            remaining.Remove(id);
            HashSet<string> standing = Reachable(document, remaining);

            List<string> closure = [id];
            foreach (string held in remaining)
                if (!standing.Contains(held))
                    closure.Add(held);

            // Deepest first: whatever depended on a node stands further from a seed than it does, so the
            // deepest node left is never anybody's only road.
            Dictionary<string, int> depth = Depths(document, _taken);
            closure.Sort((first, second) => depth.GetValueOrDefault(second) - depth.GetValueOrDefault(first));

            return closure;
        }

        /// <summary>Gives the node back when the rules allow it; returns the same reason <see cref="CheckRefund"/> would.</summary>
        public AllocationResult TryRefund(PassiveTreeDocument document, string id)
        {
            AllocationResult result = CheckRefund(document, id);
            if (result != AllocationResult.Success) return result;

            _taken.Remove(id);

            // Unconditional because CheckRefund already refused every class that costs no point.
            Spent--;
            return result;
        }

        public bool Refund(PassiveTreeDocument document, string id) =>
            TryRefund(document, id) == AllocationResult.Success;

        /// <summary>Cheapest route to a node (BFS from every taken node at once), excluding what's
        /// already taken. Empty when the node is already taken or unreachable.</summary>
        public List<string> PathTo(PassiveTreeDocument document, string target)
        {
            if (!document.Contains(target) || _taken.Contains(target)) return [];

            var seen = new HashSet<string>(_taken, StringComparer.Ordinal);
            var previous = new Dictionary<string, string>(StringComparer.Ordinal);
            var queue = new Queue<string>(_taken);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();

                foreach (string neighbour in document.Neighbours(current))
                {
                    if (!seen.Add(neighbour)) continue;

                    previous[neighbour] = current;
                    if (neighbour != target)
                    {
                        queue.Enqueue(neighbour);
                        continue;
                    }

                    var route = new List<string>();
                    for (string step = target; !_taken.Contains(step); step = previous[step]) route.Insert(0, step);
                    return route;
                }
            }

            return [];
        }

        /// <summary>Takes a whole route at once. All-or-nothing: a route that does not fit the budget
        /// is not taken partially, because a half-bought path is never what was asked for.</summary>
        public bool TakePath(PassiveTreeDocument document, IReadOnlyList<string> route, int budget) =>
            TryTakePath(document, route, budget) == AllocationResult.Success;

        /// <summary>Same purchase as <see cref="TakePath"/>, naming the first refusal instead of a bare false.
        /// Empty route lands on <see cref="AllocationResult.NotConnected"/>, matching what <see cref="PathTo"/> returns for an unreachable node.</summary>
        public AllocationResult TryTakePath(PassiveTreeDocument document, IReadOnlyList<string> route, int budget)
        {
            if (route.Count == 0) return AllocationResult.NotConnected;
            if (Spent + route.Count > budget) return AllocationResult.NotEnoughPoints;

            // The budget pre-check is not the only way a step can be refused — adjacency can fail on a
            // route that did not come from PathTo — so the promise is kept by rollback, not by luck.
            var takenBefore = new HashSet<string>(_taken, StringComparer.Ordinal);
            int spentBefore = Spent;

            foreach (string id in route)
            {
                AllocationResult step = TryTake(document, id, budget);
                if (step == AllocationResult.Success) continue;

                _taken.Clear();
                _taken.UnionWith(takenBefore);
                Spent = spentBefore;
                return step;
            }

            return AllocationResult.Success;
        }

        /// <summary>Nodes adjacent to the allocation — what a canvas highlights as "next".</summary>
        public HashSet<string> Frontier(PassiveTreeDocument document, int budget)
        {
            var frontier = new HashSet<string>(StringComparer.Ordinal);
            if (Spent >= budget) return frontier;

            foreach (string taken in _taken)
                foreach (string neighbour in document.Neighbours(taken))
                    if (!_taken.Contains(neighbour))
                        frontier.Add(neighbour);

            return frontier;
        }

        private void Recount(PassiveTreeDocument document)
        {
            Spent = 0;
            foreach (string id in _taken)
            {
                PassiveNode? node = document.Find(id);
                if (node is not null && NodeKindRules.CostsPoint(node.Kind)) Spent++;
            }
        }

        /// <summary>Breadth-first walk from every seed, stepping only through the given set.</summary>
        private static HashSet<string> Reachable(PassiveTreeDocument document, HashSet<string> allowed) =>
            new(Depths(document, allowed).Keys, StringComparer.Ordinal);

        /// <summary>Same walk, keeping each node's step-distance from a seed — one traversal backs both
        /// reachability and distance, so they can't disagree.</summary>
        private static Dictionary<string, int> Depths(PassiveTreeDocument document, HashSet<string> allowed)
        {
            var depths = new Dictionary<string, int>(StringComparer.Ordinal);
            var queue = new Queue<string>();

            foreach (PassiveNode node in document.Nodes)
                if (node.Kind == PassiveNodeKind.Start && allowed.Contains(node.Id) && depths.TryAdd(node.Id, 0))
                    queue.Enqueue(node.Id);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                int next = depths[current] + 1;

                foreach (string neighbour in document.Neighbours(current))
                    if (allowed.Contains(neighbour) && depths.TryAdd(neighbour, next))
                        queue.Enqueue(neighbour);
            }

            return depths;
        }
    }
}
