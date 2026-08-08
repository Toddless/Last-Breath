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
    /// A plan for the allocation that has not been paid for yet.
    ///
    /// <para>It is a PROJECTION and not a list of intentions: it holds a second
    /// <see cref="AllocationState"/> carrying what the allocation WOULD BE once the plan is applied, and
    /// marking a node is that node's own operation performed on the projection. So the rules of the tree
    /// are read from one place for a plan and for a purchase — a node reachable only through another
    /// marked node passes without a second body of rules to say so, and giving back the middle of a
    /// branch is refused by the same reachability walk that refuses it for real.</para>
    ///
    /// <para>The marks live HERE and never in the service, which is the whole safety of the thing.
    /// Nothing downstream of the allocation can see them: the parametric contribution, the pipeline
    /// knobs, the ability book, the socket board and the save participant all feed on
    /// <see cref="IPassiveTreeService"/>, and this object neither writes to it nor is reachable from it.
    /// A marked node therefore gives no bonus, opens no slot, costs no point and is written to no file
    /// until the plan is applied — not by agreement, but because there is no channel.</para>
    ///
    /// <para>Owned by the screen that shows it and dies with it. Anything that changes the real
    /// allocation from outside — a direct purchase, the console, a loaded file, a new session, the tree
    /// document being reloaded — arrives as <see cref="IPassiveTreeService.AllocationChanged"/> and the
    /// plan is replayed on top of the new truth, dropping whatever no longer holds.</para>
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

        /// <summary>
        /// Whether the node can be marked, and why not when it cannot — the tree's own vocabulary, asked
        /// of the projection. Already-marked nodes answer for free: in <see cref="DraftMode.Take"/> the
        /// projection already holds one, so it comes back <see cref="AllocationResult.AlreadyTaken"/>,
        /// and in <see cref="DraftMode.Refund"/> it no longer does, so it comes back
        /// <see cref="AllocationResult.NotTaken"/>.
        /// </summary>
        public AllocationResult CanMark(string nodeId) => _mode == DraftMode.Take
            ? _projection.CheckTake(_tree.Tree, nodeId, _tree.TotalPoints)
            : _projection.CheckRefundAll(_tree.Tree, [nodeId]);

        /// <summary>
        /// Puts the node in the plan, or names the reason it cannot go in. A return that would strand a
        /// branch behind it is REFUSED rather than quietly widened to take the branch with it: every node
        /// of a respec is paid for, and a mark that tripled the bill without being asked would be a price
        /// nobody agreed to.
        /// </summary>
        public AllocationResult Mark(string nodeId)
        {
            AllocationResult result = Project(nodeId);
            if (result != AllocationResult.Success) return result;

            _marked.Add(nodeId);
            Changed?.Invoke();
            return result;
        }

        /// <summary>
        /// Marks a whole route at once — what a click on a node several steps away means. All-or-nothing:
        /// a route half marked is not the route the player pointed at, and the steps already in the plan
        /// are not in the route, so it is priced from where the plan stands rather than from where the
        /// character does.
        /// </summary>
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

        /// <summary>
        /// Takes the node out of the plan. Not a deletion but a replay without it: what stood only on
        /// that node stops standing on anything, and the same rule that let those marks in decides they
        /// are out. Whoever asked keeps the list afterwards and can say what else went.
        /// </summary>
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

        /// <summary>
        /// Buys the planned nodes. The mark order is the purchase order — every mark was adjacent to
        /// what the marks before it reached — so the service's own all-or-nothing route purchase is the
        /// whole of it, and no second walk of the graph is needed to put the set in a legal sequence.
        /// <para>A plan that buys nothing, a return among them, prices at the empty route and comes back
        /// <see cref="AllocationResult.NotConnected"/> — the same answer the allocation gives for
        /// anything there is no way to reach.</para>
        /// <para>Only the buying half is applied here. Giving nodes back spends gold as well as points
        /// and belongs to a gate that settles both, so a screen sends <see cref="PendingRefunds"/> there
        /// instead of asking this.</para>
        /// </summary>
        public AllocationResult ApplyTakes() => _tree.TakePath(_mode == DraftMode.Take ? _marked : []);

        public void Dispose() => _tree.AllocationChanged -= Revalidate;

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

        /// <summary>
        /// Rebuilds the projection from the real allocation and replays the marks onto it in the order
        /// they were made, keeping those that still hold. A mark that lost its route, its budget or its
        /// point in existing simply does not survive the replay — one rule for dropping a mark, and it is
        /// the same rule that admitted it.
        /// </summary>
        private void Reproject()
        {
            List<string> replayed = [.. _marked];

            // Raised before the document is even read: asking for it can adopt a reloaded catalog, which
            // announces an allocation change of its own, and a replay reacting to its own reading would
            // rebuild the plan from underneath itself.
            _replaying = true;

            _projection.Restore(_tree.Tree, _tree.TakenNodes);
            _marked.Clear();

            foreach (string id in replayed)
                if (Project(id) == AllocationResult.Success)
                    _marked.Add(id);

            _replaying = false;
            Changed?.Invoke();
        }

        /// <summary>
        /// The marks the plan and the allocation actually disagree about, in the order they were made.
        /// Read off both states rather than off the mode, so a mark that the last replay quietly settled
        /// — a node bought outside the plan, a node the tree no longer holds — is not reported as
        /// something still to do.
        /// </summary>
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
