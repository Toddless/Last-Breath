namespace Core.PassiveTree.View
{
    /// <summary>What a node is to the character and to the plan he has not paid for yet. Hover and
    /// selection are deliberately not here: they belong to the cursor, they move on every mouse motion,
    /// and they are drawn by one layer for every node instead of being pushed into each of them.</summary>
    public enum PassiveNodeVisualState
    {
        Idle,

        /// <summary>On the previewed route to the node under the cursor.</summary>
        OnPath,

        /// <summary>Marked to be bought once the plan is confirmed. Not taken: it pays out nothing, opens
        /// no slot and costs no point until the button is pressed — this is the picture of a plan.</summary>
        Pending,

        /// <summary>Held, and marked to be given back once the plan is confirmed.</summary>
        PendingRefund,

        Taken
    }

    /// <summary>
    /// The ring the wheel draws around a node because the PLAN touches it — the whole of what "marked"
    /// looks like, and the only thing that says so.
    /// <para>Measured off what the node itself measures on screen rather than as a gap in pixels: a node
    /// size has a screen floor under it and a constant gap does not, so a fixed gap leaves the dot
    /// standing at the floor while the mark keeps its distance, and the mark ends up several times the
    /// thing it is about.</para>
    /// </summary>
    /// <param name="RingScale">How much wider than the node the ring is drawn. 0 means the state wears
    /// no mark at all.</param>
    /// <param name="ScreenWidth">How heavy the ring is, in SCREEN pixels: a mark on what the plan would
    /// change is an affordance and not part of the map, so it keeps its weight however deep the wheel is
    /// zoomed.</param>
    public readonly record struct PassiveNodeMark(float RingScale, float ScreenWidth)
    {
        public bool IsDrawn => RingScale > 0f && ScreenWidth > 0f;
    }

    /// <summary>
    /// Where a node stands between what the character holds and what his plan would hold, and what that
    /// makes it look like. Both answers live here and nowhere else.
    ///
    /// <para>One reading, because the picture is assembled by more than one mechanism: the nodes that
    /// carry a scene of their own are painted by that scene, and everything else is drawn by a layer over
    /// the whole field. A rule written once per layer is the one way a node marked to be bought ends up
    /// looking exactly like a node nobody has touched — the player clicks and nothing happens on
    /// screen.</para>
    ///
    /// <para>The two plan marks differ in shape and not only in hue, because they are opposite acts:
    /// a purchase reaches OUTWARD, clear of the node, and a return is a heavy band struck across the
    /// node's own edge. Confusing them means paying for the wrong thing.</para>
    /// </summary>
    public static class PassiveNodeStates
    {
        /// <summary>Nothing of the plan's own. What is held says so with its own glow, and what is idle
        /// has nothing to say.</summary>
        private static readonly PassiveNodeMark s_unmarked = new(0f, 0f);

        /// <summary>A purchase: a ring set clear of the node, reaching for something not held yet.</summary>
        private static readonly PassiveNodeMark s_take = new(1.7f, 2.5f);

        /// <summary>A return: a heavy band on the node's own edge, striking off something held.</summary>
        private static readonly PassiveNodeMark s_giveBack = new(1.15f, 4f);

        /// <summary>
        /// What the node is, from the two allocations and the previewed route. Held and projected is
        /// owned; projected without being held is a purchase waiting to be paid for; held without being
        /// projected is a return waiting to be paid for.
        /// </summary>
        /// <param name="taken">The character holds the node.</param>
        /// <param name="projected">The allocation the plan would leave behind holds the node.</param>
        /// <param name="onPath">The node is a step of the route previewed to whatever is under the
        /// cursor.</param>
        public static PassiveNodeVisualState Of(bool taken, bool projected, bool onPath)
        {
            if (taken && projected) return PassiveNodeVisualState.Taken;
            if (projected) return PassiveNodeVisualState.Pending;
            if (taken) return PassiveNodeVisualState.PendingRefund;

            return onPath ? PassiveNodeVisualState.OnPath : PassiveNodeVisualState.Idle;
        }

        /// <summary>What every node of a plan pointing one way is. A draft holds one kind of mark at a
        /// time, so the direction alone settles the state of everything in it.</summary>
        public static PassiveNodeVisualState OfPlanned(bool givesBack) =>
            Of(taken: givesBack, projected: !givesBack, onPath: false);

        /// <summary>The mark the state wears. The states that wear none answer with a ring nothing is
        /// drawn for, so a caller has one shape to handle rather than a null and a value.</summary>
        public static PassiveNodeMark MarkOf(PassiveNodeVisualState state) => state switch
        {
            PassiveNodeVisualState.Pending => s_take,
            PassiveNodeVisualState.PendingRefund => s_giveBack,
            _ => s_unmarked
        };
    }
}
