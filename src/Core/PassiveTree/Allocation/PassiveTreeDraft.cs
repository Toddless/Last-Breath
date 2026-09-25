namespace Core.PassiveTree.Allocation
{
    using System;
    using System.Collections.Generic;

    /// <summary>Which way a draft is pointing. The two are exclusive: one set of marks and one
    /// confirmation button, so a plan can never mean "buy these and sell those" and leave the price of
    /// the second half unanswered.</summary>
    public enum DraftMode
    {
        /// <summary>Marks are nodes to be bought.</summary>
        Take,

        /// <summary>Marks are nodes to be given back — the mode a respec is planned in.</summary>
        Refund
    }

    /// <summary>
    /// A plan for the allocation that has not been paid for yet. It is a PROJECTION, not a list of
    /// intentions: it holds a second <see cref="AllocationState"/> carrying what the allocation WOULD BE,
    /// and marking a node is that node's own operation performed on it — so a plan and a purchase read the
    /// tree's rules from one place, with no second body of reachability or budget rules.
    /// <para>The marks live HERE and never in the service, which is the whole safety of the thing: everything
    /// downstream (parametric contribution, pipeline knobs, ability book, socket board, save participant)
    /// feeds on <see cref="IPassiveTreeService"/>, and this object neither writes to it nor is reachable
    /// from it. A marked node gives no bonus, opens no slot, costs no point and reaches no file until the
    /// plan is applied — because there is no channel, not by agreement.</para>
    /// <para>Owned by the screen that shows it and dies with it. Any outside change to the real allocation
    /// arrives as <see cref="IPassiveTreeService.AllocationChanged"/> and the plan is replayed on top of the
    /// new truth, dropping whatever no longer holds.</para>
    /// </summary>
    public sealed class PassiveTreeDraft : IDisposable
    {
        private readonly IPassiveTreeService _tree;
        private readonly AllocationState _projection = new();

        /// <summary>Ordered, and the order is load-bearing: every mark was legal against the projection
        /// as it stood when it was made, so the list is also a legal purchase order — each step touches
        /// something the steps before it already reached.</summary>
        private readonly List<string> _marked = [];

        private DraftMode _mode = DraftMode.Take;

        /// <summary>A replay is running and the marks are being rebuilt, so the allocation event it may
        /// raise on the way is the plan's own doing and not news from outside.</summary>
        private bool _replaying;

        public PassiveTreeDraft(IPassiveTreeService tree)
        {
            _tree = tree;
            _tree.AllocationChanged += Revalidate;
            Reproject();
        }

        /// <summary>The plan moved: a mark was added or dropped, the mode changed, or the allocation
        /// underneath it did and the plan was replayed on top of the new one.</summary>
        public event Action? Changed;

        /// <summary>Changing it clears the plan. One set of marks means one confirmation, and a set
        /// holding both purchases and returns would be a single button with two prices.</summary>
        public DraftMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;

                _mode = value;
                _marked.Clear();
                Reproject();
            }
        }

        /// <summary>The marks in the order they were made. What is dropped when the plan no longer fits
        /// comes off the end, so the player loses the last thing he asked for and not the first.</summary>
        public IReadOnlyList<string> Marked => _marked;

        /// <summary>What the allocation would be. Read by the drawing so a marked node can be lit up;
        /// this is the only place a marked node is deliberately treated as taken, and it is a picture.</summary>
        public IReadOnlyCollection<string> Projected => _projection.Taken;

        public bool IsEmpty => _marked.Count == 0;

        /// <summary>Points the plan would have spent, against the total the character actually has.</summary>
        public int ProjectedSpent => _projection.Spent;

        public int ProjectedAvailable => Math.Max(0, _tree.TotalPoints - _projection.Spent);

        /// <summary>Nodes the plan would buy: marks the projection holds and the character does not.
        /// Derived rather than kept, so it cannot fall out of step with the projection it describes, and
        /// walked in mark order, so the list is also the order they would be bought in.</summary>
        public IReadOnlyList<string> PendingTakes => Pending(inProjection: true);

        /// <summary>Nodes the plan would give back — the set a respec is priced and charged for.</summary>
        public IReadOnlyList<string> PendingRefunds => Pending(inProjection: false);

        public bool IsMarked(string nodeId) => _marked.Contains(nodeId);

        /// <summary>Whether the plan would hold the node once applied. What the drawing asks to tell a
        /// planned node from a bought one.</summary>
        public bool IsProjected(string nodeId) => _projection.IsTaken(nodeId);

        /// <summary>What marking the node for return would take with it: the node and everything that
        /// would be left hanging behind it. Counted FROM THE PLAN, so a tail already marked is not counted
        /// a second time and the number the player is shown is the one he is charged for.</summary>
        public IReadOnlyList<string> RefundTailOf(string nodeId) => _projection.RefundClosure(_tree.Tree, nodeId);

        /// <summary>Whether the node can be marked and why not, in the tree's own vocabulary, asked of the
        /// projection. In <see cref="DraftMode.Refund"/> the question is about the whole tail
        /// (<see cref="RefundTailOf"/>), not the node alone. An already-marked node answers out of the
        /// projection itself: <see cref="AllocationResult.AlreadyTaken"/> when taking,
        /// <see cref="AllocationResult.NotTaken"/> when refunding.</summary>
        public AllocationResult CanMark(string nodeId) => _mode == DraftMode.Take
            ? _projection.CheckTake(_tree.Tree, nodeId, _tree.TotalPoints)
            : _projection.CheckRefundAll(_tree.Tree, RefundTailOf(nodeId));

        /// <summary>Puts the node in the plan, or names the reason it cannot go in. A return takes the node
        /// TOGETHER WITH whatever would hang behind it, marked from the leaves inward so no step strands
        /// anything; the tail's size and price are named before the click.</summary>
        public AllocationResult Mark(string nodeId) =>
            _mode == DraftMode.Take ? MarkOne(nodeId) : MarkPath(RefundTailOf(nodeId));

        /// <summary>Marks a whole route at once — what a click on a node several steps away means.
        /// All-or-nothing, and priced from where the plan stands rather than from where the character does,
        /// since steps already marked are not in the route.</summary>
        public AllocationResult MarkPath(IReadOnlyList<string> route)
        {
            if (route.Count == 0) return AllocationResult.NotConnected;

            int before = _marked.Count;
            foreach (string id in route)
            {
                AllocationResult step = Project(id);
                if (step == AllocationResult.Success)
                {
                    _marked.Add(id);
                    continue;
                }

                _marked.RemoveRange(before, _marked.Count - before);
                Reproject();
                return step;
            }

            Changed?.Invoke();
            return AllocationResult.Success;
        }

        /// <summary>Takes the node out of the plan — a replay without it rather than a deletion, so marks
        /// that stood only on it fall by the same rule that admitted them.</summary>
        public void Unmark(string nodeId)
        {
            if (_marked.RemoveAll(id => string.Equals(id, nodeId, StringComparison.Ordinal)) == 0) return;

            Reproject();
        }

        /// <summary>Drops the plan whole — closing the screen, or walking away from it. Nothing was
        /// bought and nothing was charged, so there is nothing to undo.</summary>
        public void Clear()
        {
            if (_marked.Count == 0) return;

            _marked.Clear();
            Reproject();
        }

        /// <summary>The cheapest route from THE PLAN to a node, so a step already marked is free and the
        /// price under the cursor is the price the player would actually be asked for.</summary>
        public IReadOnlyList<string> PathTo(string nodeId) => _projection.PathTo(_tree.Tree, nodeId);

        /// <summary>Buys the planned nodes. The mark order is already a legal purchase order, so the
        /// service's all-or-nothing route purchase is the whole of it. A plan that buys nothing — a return
        /// plan among them — prices at the empty route and comes back
        /// <see cref="AllocationResult.NotConnected"/>. Only the buying half is applied here: giving nodes
        /// back spends gold as well as points, so a screen sends <see cref="PendingRefunds"/> to the gate
        /// that settles both.</summary>
        public AllocationResult ApplyTakes() => _tree.TakePath(_mode == DraftMode.Take ? _marked : []);

        public void Dispose() => _tree.AllocationChanged -= Revalidate;

        /// <summary>One node into the plan — what a purchase mark is, and the step a tail is built out
        /// of.</summary>
        private AllocationResult MarkOne(string nodeId)
        {
            AllocationResult result = Project(nodeId);
            if (result != AllocationResult.Success) return result;

            _marked.Add(nodeId);
            Changed?.Invoke();
            return result;
        }

        /// <summary>The node's own operation, performed on the projection. One place, so a mark and the
        /// replay behind every other change of the plan cannot come to mean different things.</summary>
        private AllocationResult Project(string nodeId) => _mode == DraftMode.Take
            ? _projection.TryTake(_tree.Tree, nodeId, _tree.TotalPoints)
            : _projection.TryRefundAll(_tree.Tree, [nodeId]);

        /// <summary>The allocation moved underneath the plan. Everything that can do it — a direct
        /// purchase, the console, a loaded file, a new session, a reloaded tree — arrives here, and the
        /// plan is rebuilt on top of what is now true.</summary>
        private void Revalidate()
        {
            if (_replaying) return;

            Reproject();
        }

        /// <summary>Rebuilds the projection from the real allocation and replays the marks in order,
        /// keeping those that still hold: a mark that lost its route, its budget or its point simply does
        /// not survive — the rule that drops a mark is the rule that admitted it.</summary>
        private void Reproject()
        {
            List<string> replayed = [.. _marked];

            // Raised before the document is read: reading it can adopt a reloaded catalog, which announces
            // an allocation change of its own, and the replay would then rebuild itself from underneath.
            _replaying = true;

            _projection.Restore(_tree.Tree, _tree.TakenNodes);
            _marked.Clear();

            foreach (string id in replayed)
                if (Project(id) == AllocationResult.Success)
                    _marked.Add(id);

            _replaying = false;
            Changed?.Invoke();
        }

        /// <summary>The marks the plan and the allocation actually disagree about, in the order they were
        /// made. Read off both states rather than off the mode, so a mark the world already settled — a node
        /// bought outside the plan, a node the tree no longer holds — is not reported as still to do.</summary>
        private List<string> Pending(bool inProjection)
        {
            List<string> pending = [];

            foreach (string id in _marked)
                if (_projection.IsTaken(id) == inProjection && _tree.IsTaken(id) != inProjection)
                    pending.Add(id);

            return pending;
        }
    }
}
