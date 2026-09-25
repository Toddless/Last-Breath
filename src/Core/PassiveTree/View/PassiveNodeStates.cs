namespace Core.PassiveTree.View
{
    /// <summary>What a node is to the character and to the unpaid plan. Hover/selection are deliberately
    /// not here — they belong to the cursor and are drawn per-frame by one layer, not pushed into each
    /// node.</summary>
    public enum PassiveNodeVisualState
    {
        Idle,

        /// <summary>On the previewed route to the node under the cursor.</summary>
        OnPath,

        /// <summary>Marked to be bought once the plan is confirmed — pays out nothing, opens no slot,
        /// costs no point until confirmed.</summary>
        Pending,

        /// <summary>Held, and marked to be given back once the plan is confirmed.</summary>
        PendingRefund,

        Taken
    }

    /// <summary>
    /// The ring drawn around a node because the plan touches it — the entirety of what "marked" looks
    /// like. Measured off the node's own on-screen size rather than a pixel gap, since node size has a
    /// screen floor and a fixed gap does not (see <see cref="NodeGeometry"/>).
    /// </summary>
    /// <param name="RingScale">How much wider than the node the ring is. 0 means no mark at all.</param>
    /// <param name="ScreenWidth">Ring weight in SCREEN pixels — an affordance, so it keeps its weight
    /// regardless of zoom.</param>
    public readonly record struct PassiveNodeMark(float RingScale, float ScreenWidth)
    {
        public bool IsDrawn => RingScale > 0f && ScreenWidth > 0f;
    }

    /// <summary>
    /// Where a node stands between what the character holds and what his plan would hold, and what that
    /// looks like. One reading for both, since the picture is assembled by more than one mechanism (nodes
    /// with their own scene paint themselves; everything else is drawn by an overlay layer) — a rule
    /// written once per layer is what keeps a marked node from ever looking untouched.
    /// <para>The two plan marks differ in shape, not just hue: a purchase reaches OUTWARD (clear of the
    /// node) and a return is a heavy band struck across the node's own edge — opposite acts, and confusing
    /// them means paying for the wrong thing.</para>
    /// </summary>
    public static class PassiveNodeStates
    {
        /// <summary>Nothing of the plan's own: what is held says so with its own glow, and what is idle
        /// has nothing to say.</summary>
        private static readonly PassiveNodeMark s_unmarked = new(0f, 0f);

        /// <summary>A purchase: a ring set clear of the node, reaching for something not held yet.</summary>
        private static readonly PassiveNodeMark s_take = new(1.7f, 2.5f);

        /// <summary>A return: a heavy band on the node's own edge, striking off something held.</summary>
        private static readonly PassiveNodeMark s_giveBack = new(1.15f, 4f);

        /// <summary>Held and projected = owned; projected only = purchase pending; held only = return
        /// pending.</summary>
        /// <param name="taken">The character holds the node.</param>
        /// <param name="projected">The plan's allocation would hold the node.</param>
        /// <param name="onPath">The node is a step of the route previewed to the cursor.</param>
        public static PassiveNodeVisualState Of(bool taken, bool projected, bool onPath)
        {
            if (taken && projected) return PassiveNodeVisualState.Taken;
            if (projected) return PassiveNodeVisualState.Pending;
            if (taken) return PassiveNodeVisualState.PendingRefund;

            return onPath ? PassiveNodeVisualState.OnPath : PassiveNodeVisualState.Idle;
        }

        /// <summary>State for every node in a plan pointing one way — a draft holds one kind of mark at a
        /// time, so direction alone settles it.</summary>
        public static PassiveNodeVisualState OfPlanned(bool givesBack) =>
            Of(taken: givesBack, projected: !givesBack, onPath: false);

        /// <summary>The mark a state wears; unmarked states answer with a ring of nothing so callers
        /// handle one shape instead of a null and a value.</summary>
        public static PassiveNodeMark MarkOf(PassiveNodeVisualState state) => state switch
        {
            PassiveNodeVisualState.Pending => s_take,
            PassiveNodeVisualState.PendingRefund => s_giveBack,
            _ => s_unmarked
        };
    }
}
