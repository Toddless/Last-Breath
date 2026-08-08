namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Which nodes are taken, what that costs, and what may still be taken or given back. The two
    /// rules that shape it — a node must touch something already taken, and a refund may not orphan
    /// anything — are enforced here and nowhere else, so the game service and the authoring tool
    /// cannot drift apart on what a legal allocation is.
    /// </summary>
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

        /// <summary>
        /// Replaces the whole set with a restored one — a save file, wholesale rather than node by
        /// node. What arrives is not trusted: it is put through the same re-check an edited tree goes
        /// through, so ids the tree no longer knows and anything that lost its route to a seed drop
        /// out instead of becoming an allocation the take/refund rules could never have produced.
        /// </summary>
        public void Restore(PassiveTreeDocument document, IEnumerable<string> nodes)
        {
            _taken.Clear();
            _taken.UnionWith(nodes);
            Resync(document);
        }

        /// <summary>
        /// Takes the set as it stands, with no tree to measure it against — a restore that arrived
        /// before the catalog did. Nothing is checked and nothing is dropped, because a check against
        /// a tree that is not there would drop everything. What the set costs stays as it was: the
        /// price of a node cannot be read without the node, and the next <see cref="Resync"/> settles
        /// both the set and the spend.
        /// </summary>
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

        /// <summary>
        /// Whether the node can go back, and why not when it cannot. It may go back only if nothing is
        /// left hanging in the air — unwinding happens from the ends. Formally: with the node gone,
        /// every still-taken node must reach a seed through taken nodes.
        /// </summary>
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

        /// <summary>
        /// Cheapest route from the current allocation to a node, excluding what is already taken.
        /// Breadth-first from every taken node at once, so the answer is the fewest points that would
        /// buy the target. Empty when the node is already taken or nothing connects to it.
        /// </summary>
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

        /// <summary>
        /// The same purchase as <see cref="TakePath"/>, naming the first refusal instead of collapsing
        /// it into a false — what an interface needs to say why a route it offered cannot be bought.
        /// An empty route lands on <see cref="AllocationResult.NotConnected"/>: that is what
        /// <see cref="PathTo"/> hands back for a node nothing reaches.
        /// </summary>
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
        private static HashSet<string> Reachable(PassiveTreeDocument document, HashSet<string> allowed)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();

            foreach (PassiveNode node in document.Nodes)
                if (node.Kind == PassiveNodeKind.Start && allowed.Contains(node.Id) && visited.Add(node.Id))
                    queue.Enqueue(node.Id);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (string neighbour in document.Neighbours(current))
                    if (allowed.Contains(neighbour) && visited.Add(neighbour))
                        queue.Enqueue(neighbour);
            }

            return visited;
        }
    }
}
