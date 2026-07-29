namespace PassiveTreeEditor.Source.Simulation
{
    using System;
    using System.Collections.Generic;
    using Model;

    /// <summary>
    /// "As in game" allocation: which nodes are taken, what that costs, and what may still be taken
    /// or given back. The two rules that shape it — a node must touch something already taken, and a
    /// refund may not orphan anything — are enforced here and nowhere else.
    /// </summary>
    public sealed class AllocationState
    {
        private readonly HashSet<string> _taken = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Taken => _taken;

        /// <summary>Points actually spent: the three seeds are granted, so they never count.</summary>
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

        /// <summary>Drops anything that stopped existing or stopped being reachable after an edit.</summary>
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

        public bool CanTake(PassiveTreeDocument document, string id, int budget)
        {
            PassiveNode? node = document.Find(id);
            if (node is null || _taken.Contains(id)) return false;
            if (!NodeKindRules.CostsPoint(node.Kind)) return false;
            if (Spent >= budget) return false;

            foreach (string neighbour in document.Neighbours(id))
                if (_taken.Contains(neighbour))
                    return true;

            return false;
        }

        public bool Take(PassiveTreeDocument document, string id, int budget)
        {
            if (!CanTake(document, id, budget)) return false;

            _taken.Add(id);

            // Unconditional because CanTake already refused every class that costs no point.
            Spent++;
            return true;
        }

        /// <summary>
        /// A node may go back only if nothing is left hanging in the air — unwinding happens from the
        /// ends. Formally: with the node gone, every still-taken node must reach a seed through taken
        /// nodes.
        /// </summary>
        public bool CanRefund(PassiveTreeDocument document, string id)
        {
            PassiveNode? node = document.Find(id);
            if (node is null || !_taken.Contains(id)) return false;
            if (!NodeKindRules.CostsPoint(node.Kind)) return false;

            var remaining = new HashSet<string>(_taken, StringComparer.Ordinal);
            remaining.Remove(id);

            return Reachable(document, remaining).Count == remaining.Count;
        }

        public bool Refund(PassiveTreeDocument document, string id)
        {
            if (!CanRefund(document, id)) return false;

            _taken.Remove(id);

            // Unconditional because CanRefund already refused every class that costs no point.
            Spent--;
            return true;
        }

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
        public bool TakePath(PassiveTreeDocument document, IReadOnlyList<string> route, int budget)
        {
            if (route.Count == 0 || Spent + route.Count > budget) return false;

            // The budget pre-check is not the only way a step can be refused — adjacency can fail on a
            // route that did not come from PathTo — so the promise is kept by rollback, not by luck.
            var takenBefore = new HashSet<string>(_taken, StringComparer.Ordinal);
            int spentBefore = Spent;

            foreach (string id in route)
            {
                if (Take(document, id, budget)) continue;

                _taken.Clear();
                _taken.UnionWith(takenBefore);
                Spent = spentBefore;
                return false;
            }

            return true;
        }

        /// <summary>Nodes adjacent to the allocation — what the canvas highlights as "next".</summary>
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
