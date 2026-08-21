namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Localization;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.View;
    using Core.Views.UI;
    using Godot;

    /// <summary>What a LEFT click on the wheel turns out to be about. One point can belong to two things
    /// at once — a node, and a pip of that node's ring — and there is exactly one place that says which
    /// of them the button meant.</summary>
    public enum WheelClickTarget
    {
        /// <summary>The node the point belongs to: a mark in the plan, or a purchase with Ctrl.</summary>
        Node,

        /// <summary>The slot the pip stands for: the bag is asked what would go into it.</summary>
        Slot
    }

    /// <summary>
    /// What a point on the wheel is, and what the left button on it is about — the two readings the
    /// canvas makes of every gesture, kept out here in plain C# so both can be walked without an engine.
    ///
    /// <para>A pip is asked about first and WITHOUT any slack of its own, so its priority holds exactly
    /// inside its own small circle; everything wider is the node rule's business, which keeps the slack
    /// that makes a dot aimable with the whole tree on screen.</para>
    /// </summary>
    public static class WheelHit
    {
        /// <summary>The pip a screen point lands on, or null when it lands on none. Pip centres are
        /// document coordinates and pip radii are pixels, so the comparison is made in SCREEN space —
        /// against the very radius the ring was drawn with, rather than against a second spelling of
        /// it.</summary>
        public static PassiveSocketPip? PipAt(
            IReadOnlyList<PassiveSocketPip> pips, CanvasTransform view, float x, float y)
        {
            foreach (PassiveSocketPip pip in pips)
            {
                float deltaX = view.ScreenX(pip.X) - x;
                float deltaY = view.ScreenY(pip.Y) - y;
                if (deltaX * deltaX + deltaY * deltaY <= pip.ScreenRadius * pip.ScreenRadius) return pip;
            }

            return null;
        }

        /// <summary>
        /// Which of the two the left button meant. ONLY the pip of an open, empty slot is a target of its
        /// own: it is the one state with a question to answer — what of mine would go in here — and the
        /// click that asks it must not also reach the node underneath, or asking would plan to give the
        /// ability back every time the player asks while a return is being drawn up.
        /// <para>Every other pip leaves the click to its node, and nothing about those states is
        /// invented here: a filled one gives its augment back to the RIGHT button, a closed one is
        /// remove-only, and a pip whose node nobody has bought is the picture of a slot that does not
        /// exist yet. For all three the ring stays part of its own node's presence, which is what a point
        /// off the pips has always been.</para>
        /// </summary>
        public static WheelClickTarget LeftClickOn(PassiveSocketPip? pip) =>
            pip is { State: SocketSlotState.Open } ? WheelClickTarget.Slot : WheelClickTarget.Node;
    }

    /// <summary>
    /// The wheel itself: the only node of the whole screen the engine treats as a mouse target, and the
    /// only place that decides what a point on it means.
    ///
    /// <para>Everything inside is drawn in DOCUMENT coordinates within one <see cref="Node2D"/> frame.
    /// PANNING writes one field of that frame and nothing else — no layer is redrawn, no view is moved
    /// — which is why the tree can be dragged around at any size without touching the scene tree.
    /// ZOOMING writes the frame too, but the frame only scales what has already been drawn, and a
    /// picture scaled is not a picture drawn: a screen floor stops being the size it promised and a width
    /// authored in screen pixels stops being that many. So a wheel click also re-derives everything that
    /// is measured in pixels and hands it down (<see cref="ApplyScale"/>) — a handful of layer redraws
    /// and a scale per view, at a click of the wheel and at the two other things that take the same road
    /// (a reframe and an allocation pass), never per frame.</para>
    ///
    /// <para>The frame is a MIRROR of <see cref="CanvasTransform"/>, written by <see cref="ApplyTransform"/>
    /// and by nothing else. Picking reads the transform, the GPU reads the frame, and the two agree only
    /// because there is one writer: an animation that flew the view to a node by tweening the frame
    /// would silently move every click somewhere else for as long as it ran.</para>
    ///
    /// <para>Hit testing is ONE rule for the whole surface (<see cref="Resolve"/>), and it answers a pair:
    /// the node under the pointer and the socket pip under it, if any. The engine's own picking is not
    /// used and cannot be: the node scenes are Node2D with every control under them locked out of the
    /// mouse pass, because the authored layout has around a hundred and fifty pairs of overlapping shapes
    /// and the GUI walk would resolve them by the order of lines in a file that is rewritten daily.
    /// Dropping an augment lands on this node too and resolves the target the same way.</para>
    ///
    /// <para>Nothing here spends anything by itself. A click MARKS, and marks live in the draft the window
    /// owns; the two roads that actually move the character are a Ctrl-click, which buys through the
    /// service, and the confirmation button, which is the window's. The one click that is not about the
    /// plan at all is a click on the pip of an OPEN slot: it asks the courier what the bag holds for that
    /// address, and the courier is the sheet's own — the wheel assembles no list and seats nothing.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveWheelCanvas : Control, IRequireServices
    {
        /// <summary>The uid the scene file declares in its own header — the authority, so a copy that
        /// drifts from it resolves to nothing at all in the game project.</summary>
        private const string UID = "uid://cqv8m04rt6nd3";

        [Export] private Node2D? _rig;
        [Export] private WheelBackdropLayer? _backdrop;
        [Export] private WheelEdgeLayer? _idleEdges;
        [Export] private WheelEdgeLayer? _takenEdges;
        [Export] private WheelFieldLayer? _field;

        /// <summary>Under the node scenes: the previewed route, the frontier and the outline of the
        /// plan.</summary>
        [Export] private WheelCursorLayer? _route;

        /// <summary>Over the node scenes: the gold rim of whatever the pointer is on. It follows the edge
        /// of the node's own body, and under the scene the body's own sprite would hide it.</summary>
        [Export] private WheelCursorLayer? _rim;

        [Export] private WheelSocketLayer? _socketRings;
        [Export] private Node2D? _nodeViews;

        /// <summary>Above the views and empty today: a one-off effect belongs over the node it happens
        /// to, and a layer added later would have to be slid in under everything already sitting here.</summary>
        [Export] private Node2D? _effects;

        [Export] private PackedScene? _nodeViewScene;
        [Export] private PassiveWheelStyle? _style;

        /// <summary>Art authored per node and per class, and the only place it is assigned: the layers and
        /// the node scenes are handed the lookup built from it, so one file dragged in here reaches all of
        /// them. Left empty the wheel draws itself out of the style alone, which is the wheel that
        /// shipped.</summary>
        [Export] private PassiveNodeVisualLibrary? _visualLibrary;

        /// <summary>How far the pointer may travel with the button down before the gesture stops being
        /// a click and becomes a pan.</summary>
        [Export] private float _dragThresholdPixels = 4f;

        [Export] private float _framePadding = 160f;

        /// <summary>Zoom, pan and spread, and the only arithmetic that turns a document coordinate into
        /// a pixel. The spread is read off the document the tree was authored at, so the wheel is drawn
        /// with the gaps its author laid it out with; the frame carries it together with the zoom, and
        /// every size drawn inside the frame is converted back through the same transform.</summary>
        private readonly CanvasTransform _view = new();

        private readonly Dictionary<string, PassiveNodeView> _views = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<SocketRingSlot>> _rings = new(StringComparer.Ordinal);
        private readonly Stack<PassiveNodeView> _pool = new();
        private readonly HashSet<string> _carriers = new(StringComparer.Ordinal);
        private readonly HashSet<string> _frontier = new(StringComparer.Ordinal);

        /// <summary>Reused across allocation passes: the union of what the character holds and what the
        /// plan would hold, which is what gets a scene. A fresh set per pass would make every purchase an
        /// allocation.</summary>
        private readonly HashSet<string> _lit = new(StringComparer.Ordinal);

        private NodeGeometry? _geometry;
        private SocketRingGeometry? _ringGeometry;

        /// <summary>The library read through one lookup, built once. A library never changes under a live
        /// window, so nothing rebuilds it.</summary>
        private PassiveNodeVisualIndex<PassiveNodeVisualConfig>? _visuals;

        private IPassiveTreeService? _tree;
        private IAbilitySocketBoard? _board;
        private IAugmentCellHost? _sockets;
        private IUiElementsManager? _windows;
        private ModifierFormatter? _modifiers;
        private ContextModifierFormatter? _knobs;
        private ILocalizationProvider? _localization;
        private HoverTooltipHandle? _tooltip;

        private PassiveTreeDraft? _draft;
        private PassiveRespecQuotes? _quotes;
        private PassiveTreeDocument? _document;
        private IReadOnlyList<string> _path = [];
        private string? _hovered;

        private bool _panning;
        private bool _framed;
        private bool _reportedMissingScene;

        /// <summary>A drag is in the air. An allocation pass while it is would free the view under the
        /// cursor mid-gesture — seating an augment can move the allocation, and the drag-end
        /// notification and the bus answer arrive in no fixed order.</summary>
        private bool _dragging;

        private bool _reconcilePending;

        /// <summary>The button holding the surface, or <see cref="MouseButton.None"/>. One gesture at a
        /// time: a second button pressed while one is down is not a second gesture.</summary>
        private MouseButton _pressed = MouseButton.None;

        private Vector2 _pressPosition;

        /// <summary>One line for the window to print: why the last gesture was refused, or what it did
        /// besides what was asked. Said synchronously, where the player is looking.</summary>
        public event Action<string>? StatusChanged;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Stop;
            ClipContents = true;
            FocusMode = FocusModeEnum.All;

            MouseExited += ClearHover;
            Resized += OnResized;
        }

        public override void _ExitTree()
        {
            if (_tree != null) _tree.AllocationChanged -= OnAllocationChanged;
            if (_board != null) _board.Changed -= OnBoardChanged;
            if (_draft != null) _draft.Changed -= OnDraftChanged;
            _tooltip?.Cancel();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            // Optional throughout: the battle sandbox composes no passive tree and no bag, and a screen
            // that took the scene down there would be worse than one showing an empty wheel.
            _tree = provider.Optional<IPassiveTreeService>();
            _board = provider.Optional<IAbilitySocketBoard>();
            _windows = provider.Optional<IUiElementsManager>();
            _modifiers = provider.Optional<ModifierFormatter>();
            _knobs = provider.Optional<ContextModifierFormatter>();
            _localization = provider.Optional<ILocalizationProvider>();

            if (_tree != null) _tree.AllocationChanged += OnAllocationChanged;
            if (_board != null) _board.Changed += OnBoardChanged;

            BuildGeometry();
            _visuals = _visualLibrary?.Index();
            _socketRings?.SetBoard(_board);
            _tooltip = HoverTooltip.Follow(this, ShowNodeTooltip);
            Rebuild();
        }

        /// <summary>The plan the wheel draws and marks into. Owned by the window and never registered
        /// anywhere: the canvas reads it and writes marks into it, and nothing downstream of the
        /// allocation can see either.</summary>
        public void UseDraft(PassiveTreeDraft? draft)
        {
            if (_draft != null) _draft.Changed -= OnDraftChanged;

            _draft = draft;
            if (_draft != null) _draft.Changed += OnDraftChanged;

            ReconcileAllocation();
        }

        /// <summary>Where the price of a planned return comes from — the same reading the button and the
        /// close guard print, so the popup under the cursor cannot quote a different number.</summary>
        public void UseQuotes(PassiveRespecQuotes? quotes) => _quotes = quotes;

        /// <summary>Where an augment dropped on the wheel goes. The wheel writes no request of its own —
        /// a second copy of that road would be a second reading of the same rule.</summary>
        public void UseSocketHost(IAugmentCellHost? host) => _sockets = host;

        /// <summary>Fits the whole tree on screen — the recovery hatch when panning has taken the view
        /// away from the content.</summary>
        public void FrameAll()
        {
            if (Size.X <= 1f || Size.Y <= 1f) return;

            _framed = true;

            if (_document == null || _document.Nodes.Count == 0)
            {
                _view.ResetZoom(Size.X, Size.Y);
                ApplyTransform();
                ApplyScale();
                return;
            }

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (PassiveNode node in _document.Nodes)
            {
                minX = MathF.Min(minX, node.X);
                minY = MathF.Min(minY, node.Y);
                maxX = MathF.Max(maxX, node.X);
                maxY = MathF.Max(maxY, node.Y);
            }

            _view.Fit(minX, minY, maxX, maxY, Size.X, Size.Y, _framePadding);
            ApplyTransform();
            ApplyScale();
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Lights up every slot that would take the copy the moment a drag starts, and clears
        /// the lot when it ends — one pass per drag rather than one per mouse move.</summary>
        public override void _Notification(int what)
        {
            switch ((long)what)
            {
                case NotificationDragBegin:
                    _dragging = true;
                    PreviewDrag();
                    break;
                case NotificationDragEnd:
                    _dragging = false;
                    foreach (PassiveNodeView view in _views.Values) view.SetDropTarget(null);
                    _socketRings?.SetDropPreview(null);
                    Announce(string.Empty);
                    if (_reconcilePending) OnAllocationChanged();
                    break;
            }
        }

        /// <summary>The drop check, answered from the same gate the install goes through and about the
        /// slot the same rule says is under the cursor.</summary>
        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (_sockets == null || !DragPayloadReader.TryReadInstance(data, out string instanceId)) return false;

            WheelTarget target = Resolve(atPosition);
            if (SlotAt(target) is not { } slot) return false;

            string? refusal = OwnRefusal(slot.OpenerId);
            AugmentInstallResult verdict = _sockets.Judge(slot.Address, instanceId);
            bool accepted = refusal == null && verdict.Installed;

            if (target.Node != null && _views.TryGetValue(target.Node.Id, out PassiveNodeView? view))
                view.SetDropTarget(accepted);

            Announce(accepted
                ? string.Empty
                : Localization.Localize(refusal ?? AugmentRefusalText.KeyFor(verdict)));

            return accepted;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            if (_sockets == null || !DragPayloadReader.TryReadInstance(data, out string instanceId)) return;
            if (SlotAt(Resolve(atPosition)) is not { } slot || OwnRefusal(slot.OpenerId) != null) return;

            _sockets.Install(slot.Address, instanceId);
        }

        public override void _GuiInput(InputEvent @event)
        {
            switch (@event)
            {
                case InputEventMouseButton button:
                    HandleMouseButton(button);
                    break;
                case InputEventMouseMotion motion:
                    HandleMouseMotion(motion);
                    break;
                case InputEventKey { Pressed: true, Keycode: Key.F }:
                    FrameAll();
                    AcceptEvent();
                    break;
            }
        }

        private void HandleMouseButton(InputEventMouseButton button)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.WheelUp when button.Pressed:
                    Zoom(CanvasTransform.ZoomStep, button.Position);
                    AcceptEvent();
                    return;
                case MouseButton.WheelDown when button.Pressed:
                    Zoom(1f / CanvasTransform.ZoomStep, button.Position);
                    AcceptEvent();
                    return;
                case MouseButton.Middle:
                    // Nothing a middle click could mean, so there is no gesture to tell it apart from:
                    // it pans from the press itself rather than from the distance travelled.
                    if (button.Pressed) _panning = true;
                    else PanEnded(button.Position);
                    AcceptEvent();
                    return;
                case MouseButton.Left:
                case MouseButton.Right:
                    if (button.Pressed) BeginPress(button.ButtonIndex, button.Position);
                    else EndPress(button.ButtonIndex, button.Position, button.CtrlPressed);
                    AcceptEvent();
                    return;
            }
        }

        /// <summary>Both buttons that mean something on the wheel start the same way: a press that may
        /// still turn out to be a drag of the view.</summary>
        private void BeginPress(MouseButton button, Vector2 position)
        {
            if (_pressed != MouseButton.None) return;

            GrabFocus();
            _pressed = button;
            _panning = false;
            _pressPosition = position;
        }

        /// <summary>A press that did not travel is that button's click; one that did was a pan and marks,
        /// buys and extracts nothing.</summary>
        private void EndPress(MouseButton button, Vector2 position, bool immediate)
        {
            if (_pressed != button) return;

            _pressed = MouseButton.None;
            if (_panning)
            {
                PanEnded(position);
                return;
            }

            WheelTarget target = Resolve(position);
            if (button == MouseButton.Right) OnRightClick(target);
            else OnLeftClick(target, immediate);
        }

        /// <summary>The wheel moved under a hand that has come to rest, so what the pointer is on has
        /// changed with no motion event to say so. Read once when the drag lets go — during it the
        /// pointer is holding the view, not asking about a node.</summary>
        private void PanEnded(Vector2 position)
        {
            _panning = false;
            Hover(Resolve(position).Node);
        }

        /// <summary>
        /// The pip of an OPEN slot is a target of its own and the only one: clicking it asks the bag what
        /// would go into that slot, and the node under it is left alone — see
        /// <see cref="WheelHit.LeftClickOn"/> for why the other three states are not. Everywhere else the
        /// ring is part of its own node's presence and a click means what it always meant.
        /// <para>A composition with no courier (the battle sandbox) claims nothing: a pip that swallowed
        /// the click and then had nowhere to send it would be a dead spot on the wheel.</para>
        /// <para>With Ctrl held it buys immediately, and only in the buying mode: the single road to the
        /// purse is a button with the price written on it, and one slipped modifier must not be able to
        /// spend gold without a question. A pip stands outside that: a ring is only ever drawn around a
        /// node the character already HOLDS, so there is no route to buy under one.</para>
        /// </summary>
        private void OnLeftClick(WheelTarget target, bool immediate)
        {
            if (_sockets != null && target.Pip is { } pip && WheelHit.LeftClickOn(pip) == WheelClickTarget.Slot)
            {
                OfferSlot(pip);
                return;
            }

            if (target.Node is not { } node || _draft == null || _document == null) return;

            if (_draft.Mode == DraftMode.Refund)
            {
                if (immediate) Announce(Localization.Localize(PassiveWheelText.RefundNeedsButton));
                MarkReturn(node);
                return;
            }

            if (immediate)
            {
                TakeNow(node);
                return;
            }

            if (_draft.IsMarked(node.Id))
            {
                DropMark(node.Id);
                return;
            }

            IReadOnlyList<string> route = _draft.PathTo(node.Id);
            AllocationResult result = route.Count == 0 ? _draft.CanMark(node.Id) : _draft.MarkPath(route);
            if (result != AllocationResult.Success)
                Announce(Localization.Localize(PassiveTreeRefusalText.TakeKey(_document, node.Id, result)));
        }

        /// <summary>
        /// Right-click resolves in this order and no other: a pip holding something gives its augment
        /// back, then a node the plan touches loses its mark, then nothing happens. A pip sits outside the
        /// body of its node but inside the picking slack of small neighbours, so asking about the node
        /// first would sometimes drop a stranger's mark instead of emptying the slot aimed at.
        /// </summary>
        private void OnRightClick(WheelTarget target)
        {
            if (target.Pip is { State: SocketSlotState.Filled or SocketSlotState.Held } pip)
            {
                _sockets?.Extract(pip.Address);
                return;
            }

            if (target.Node is not { } node || _draft == null) return;

            if (_draft.IsMarked(node.Id) || PlannedReturn(node.Id)) DropMark(node.Id);
        }

        /// <summary>
        /// Asks the bag what would go into that slot and seats whatever the player picks — the courier's
        /// job from here on, and the same courier the socket sheet hands its cells, so the list, its order
        /// and the install are one road with two doors.
        /// <para>The wheel's OWN refusals are answered first and out of the same reading the drop check
        /// goes through: a slot whose node the plan means to give back would take the augment and then
        /// close around it. It is the only one of the two that can be said here — a pip reading as open
        /// has a socket on the board, and the board holds one exactly while its node is taken.</para>
        /// <para>An empty answer is the courier's to say, and it says it on this window's hint line rather
        /// than as an empty list: the player asked a question where he is looking.</para>
        /// </summary>
        private void OfferSlot(PassiveSocketPip pip)
        {
            if (OwnRefusal(pip.OpenerId) is { } refusal)
            {
                Announce(Localization.Localize(refusal));
                return;
            }

            _sockets?.Pick(pip.Address);
        }

        /// <summary>Buys the cheapest route to the node through the service — the one place a click
        /// spends a point. The picture that follows comes from the allocation event rather than from this
        /// call, which is what keeps this window and the mastery window showing the same numbers.</summary>
        private void TakeNow(PassiveNode node)
        {
            if (_tree == null || _document == null) return;

            AllocationResult result = _tree.TakePath(_tree.PathTo(node.Id));
            if (result != AllocationResult.Success)
                Announce(Localization.Localize(PassiveTreeRefusalText.TakeKey(_document, node.Id, result)));
        }

        /// <summary>Plans the node's return together with everything that would be left hanging behind
        /// it. The size and the price of that tail were already in the popup before the click.</summary>
        private void MarkReturn(PassiveNode node)
        {
            if (_draft == null) return;

            if (PlannedReturn(node.Id))
            {
                DropMark(node.Id);
                return;
            }

            AllocationResult result = _draft.Mark(node.Id);
            if (result != AllocationResult.Success)
                Announce(Localization.Localize(PassiveTreeRefusalText.RefundKey(result)));
        }

        /// <summary>Takes a mark out of the plan and says how much else went with it. Dropping a mark is a
        /// replay without it, so whatever stood only on that mark stops standing — and the player has to
        /// be told, or a dozen marks vanish without a word.</summary>
        private void DropMark(string nodeId)
        {
            if (_draft == null) return;

            int before = _draft.Marked.Count;
            _draft.Unmark(nodeId);
            int also = before - _draft.Marked.Count - 1;

            Announce(also <= 0
                ? string.Empty
                : Localization.Render(PassiveWheelText.UnmarkedAlso,
                    new Dictionary<string, object?> { [PassiveWheelText.CountValue] = also }));
        }

        private void HandleMouseMotion(InputEventMouseMotion motion)
        {
            if (_panning)
            {
                _view.MovePan(motion.Relative.X, motion.Relative.Y);
                ApplyTransform();
                return;
            }

            if (_pressed != MouseButton.None && motion.Position.DistanceTo(_pressPosition) > _dragThresholdPixels)
            {
                _panning = true;
                return;
            }

            Hover(Resolve(motion.Position).Node);
        }

        private void Zoom(float factor, Vector2 pivot)
        {
            _view.ZoomBy(factor, pivot.X, pivot.Y);
            ApplyTransform();
            ApplyScale();
        }

        /// <summary>The single writer of the frame, and it writes nothing else: a pan is one assignment
        /// and no layer or view hears about it at all. What a change of SCALE additionally owes the
        /// drawing is <see cref="ApplyScale"/>'s business — keeping the two apart is what keeps dragging
        /// the wheel around free.
        /// <para>Nothing is computed here: the placement arrives as one value from the transform, so the
        /// picker and the GPU are reading the same arithmetic rather than two spellings of it.</para></summary>
        private void ApplyTransform()
        {
            if (_rig == null) return;

            CanvasFrame frame = _view.Frame();
            _rig.Position = new Vector2(frame.X, frame.Y);
            _rig.Scale = new Vector2(frame.Scale, frame.Scale);
        }

        /// <summary>
        /// The part of a scale the frame cannot carry, handed to everything that draws.
        /// <para>Every layer draws in the frame's units and is blown up by it, so anything MEASURED IN
        /// PIXELS is only right for the scale it was drawn at: the screen floor under a node radius, the
        /// floor under an edge width, a ring measured off a node's screen size, the length of a dash.
        /// Scaling that picture instead of drawing it again is what turned rings into halos, so a wheel
        /// click redraws the lot.</para>
        /// <para>The layers are handed the transform's two readings of scale and never the transform, so
        /// none of them can reach the pan and start culling.</para>
        /// <para>Deliberately unconditional, and reached from three places. A wheel click and a REFRAME
        /// are both real zoom steps — a reframe computes a new zoom through <see cref="CanvasTransform.Fit"/>
        /// — and have earned the redraw. Only the allocation pass pays for a redraw it does not need, and
        /// it pays deliberately, so there is one road down to the drawing instead of two. None of it is
        /// work per frame: no class here has a <c>_Process</c>.</para>
        /// </summary>
        private void ApplyScale()
        {
            if (_geometry == null || _style == null) return;

            _backdrop?.SetScale(_view);
            _idleEdges?.SetScale(_view);
            _takenEdges?.SetScale(_view);
            _field?.SetScale(_view);
            _route?.SetScale(_view);
            _rim?.SetScale(_view);
            _socketRings?.SetScale(_view);

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
            {
                PassiveNode? node = _document?.Find(entry.Key);
                if (node == null) continue;

                entry.Value.ApplyZoom(_geometry.ViewScale(node.Kind, _view));
            }
        }

        private void BuildGeometry()
        {
            if (_style == null)
            {
                Tracker.TrackNotFound("Passive wheel canvas has no style resource", this);
                return;
            }

            _geometry = new NodeGeometry(_style.Radii(), _style.MinNodeScreenRadius, _style.PickScreenSlack);
            _ringGeometry = _style.RingGeometry(_geometry);
        }

        /// <summary>Everything pointing into the previous document is dropped rather than re-matched: a
        /// catalog reload hands out a different instance, and views holding nodes of a dead document
        /// draw a tree nobody can click.</summary>
        private void Rebuild()
        {
            _document = _tree?.Tree;

            // The spread the tree was laid out at, before anything is framed: framing multiplies the
            // extent by it, so reading it afterwards would fit the wheel to a layout it is not drawn at.
            // Clamping and snapping are the transform's own business and happen nowhere else.
            if (_document != null) _view.SetSpread(_document.Spread, Size.X * 0.5f, Size.Y * 0.5f);

            ReleaseAllViews();
            _hovered = null;
            _path = [];
            _tooltip?.Cancel();

            _rings.Clear();
            if (_document != null) SocketRings.Collect(_document, _rings);

            _idleEdges?.SetDocument(_document);
            _takenEdges?.SetDocument(_document);
            _field?.SetDocument(_document);
            _field?.SetGeometry(_geometry);
            _field?.SetVisuals(_visuals);

            ResetCursorLayer(_route);
            ResetCursorLayer(_rim);

            _socketRings?.SetDocument(_document);
            _socketRings?.SetGeometry(_geometry, _ringGeometry);
            _socketRings?.SetRings(_rings);
            _socketRings?.SetVisuals(_visuals);

            ReconcileAllocation();
            FrameAll();
        }

        /// <summary>Both cursor layers are told the same things and each ignores the half that is not its
        /// role, so a new document reaches them through one call rather than through two spellings of
        /// it.</summary>
        private void ResetCursorLayer(WheelCursorLayer? layer)
        {
            layer?.SetDocument(_document);
            layer?.SetGeometry(_geometry);
            layer?.SetHovered(null);
            layer?.SetPath([]);
        }

        private void OnAllocationChanged()
        {
            if (_dragging)
            {
                _reconcilePending = true;
                return;
            }

            _reconcilePending = false;

            // Reference equality on purpose: a reload hands out a fresh document with the same content,
            // and everything holding a node of the old one has to be thrown away rather than matched up.
            if (!ReferenceEquals(_tree?.Tree, _document))
            {
                Rebuild();
                return;
            }

            ReconcileAllocation();
        }

        private void OnBoardChanged()
        {
            if (_dragging || _document == null) return;

            _socketRings?.SetBoard(_board);
            RefreshSocketMarks();
        }

        /// <summary>The plan moved. It costs the same pass a purchase does: the two produce the same
        /// picture and a cheaper road for one of them would be a second reading of what a node looks
        /// like.</summary>
        private void OnDraftChanged() => ReconcileAllocation();

        /// <summary>
        /// One pass over the allocation for the whole wheel. What the character holds and what the plan
        /// would hold are read ONCE into a single set, and that set answers both halves of the same
        /// question — which nodes get a scene and which are left to the mass layer — so a node cannot end
        /// up drawn twice or not at all.
        /// <para>The union rather than the projection alone: a node marked to go BACK drops out of the
        /// projection, and carrying only the projection would leave it with no scene to be painted
        /// with — the plan to give it up would be invisible.</para>
        /// </summary>
        private void ReconcileAllocation()
        {
            if (_document == null) return;

            IReadOnlyCollection<string> taken = _tree?.TakenNodes ?? [];

            _lit.Clear();
            foreach (string id in taken) _lit.Add(id);
            if (_draft != null)
                foreach (string id in _draft.Projected)
                    _lit.Add(id);

            if (CanCarryViews()) NodeCarriers.Collect(_document, _lit, HasOwnView, _carriers);
            else _carriers.Clear();

            PromoteAndDemote();

            _field?.SetCarriers(_carriers);
            _takenEdges?.SetTaken(taken);
            _route?.SetTaken(taken);
            _socketRings?.SetTaken(taken);

            RefreshFrontier();
            _route?.SetFrontier(_frontier);
            _route?.SetPlan(PlannedNodes(), _draft?.Mode == DraftMode.Refund);

            UpdatePathPreview();
            RefreshSocketMarks();
            ApplyScale();
        }

        private IReadOnlyCollection<string> PlannedNodes() => _draft == null
            ? []
            : _draft.Mode == DraftMode.Refund ? _draft.PendingRefunds : _draft.PendingTakes;

        private bool HasOwnView(PassiveNodeKind kind) => _style?.Visual(kind).HasView ?? false;

        /// <summary>Without a node scene there is nothing to promote a node into, so every node falls to
        /// the mass layer and the wheel is still readable — which is what it looks like before the
        /// scenes are built.</summary>
        private bool CanCarryViews()
        {
            if (_nodeViewScene != null && _nodeViews != null && _style != null) return true;
            if (_reportedMissingScene) return false;

            _reportedMissingScene = true;
            Tracker.TrackNotFound("Passive wheel canvas has no node view scene — every node falls to the field layer", this);
            return false;
        }

        private void PromoteAndDemote()
        {
            if (_document == null || _style == null) return;

            List<string>? gone = null;
            foreach (string id in _views.Keys)
            {
                if (_carriers.Contains(id)) continue;

                gone ??= [];
                gone.Add(id);
            }

            if (gone != null)
                foreach (string id in gone)
                    Release(id);

            foreach (string id in _carriers)
            {
                if (_views.ContainsKey(id)) continue;

                PassiveNode? node = _document.Find(id);
                if (node == null) continue;

                PassiveNodeView? view = Rent();
                if (view == null) continue;

                view.SetNode(node, _style, _visuals?.For(node));
                _views[id] = view;
            }

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
                entry.Value.SetState(StateOf(entry.Key));
        }

        /// <summary>What the node is to the character and to his plan, asked of the one place that
        /// answers it — the same reading the layer that marks the whole field goes through, so a node
        /// cannot be one thing to its own scene and another to the drawing over it.</summary>
        private PassiveNodeVisualState StateOf(string id)
        {
            bool taken = _tree?.IsTaken(id) == true;
            return PassiveNodeStates.Of(taken, _draft?.IsProjected(id) ?? taken, OnPath(id));
        }

        private bool OnPath(string id)
        {
            foreach (string step in _path)
                if (string.Equals(step, id, StringComparison.Ordinal))
                    return true;

            return false;
        }

        /// <summary>Nodes taken from the pool rather than built: after the first open an allocation
        /// change costs no instantiation at all, and the pool dies with the window so it never becomes a
        /// second cache of the tree.</summary>
        private PassiveNodeView? Rent()
        {
            if (_pool.Count > 0)
            {
                PassiveNodeView pooled = _pool.Pop();
                pooled.Visible = true;
                return pooled;
            }

            if (_nodeViewScene == null || _nodeViews == null) return null;

            var fresh = _nodeViewScene.Instantiate<PassiveNodeView>();
            _nodeViews.AddChild(fresh);
            return fresh;
        }

        private void Release(string id)
        {
            if (!_views.Remove(id, out PassiveNodeView? view)) return;

            view.SetDropTarget(null);
            view.SetSocketMark(null);

            // A pooled view is hidden, not freed, and a hidden decoration goes on animating: a respec puts
            // dozens away at once, so the effect is dropped here rather than at the next Rent.
            view.ClearEffect();
            view.Visible = false;
            _pool.Push(view);
        }

        private void ReleaseAllViews()
        {
            List<string> ids = [.. _views.Keys];
            foreach (string id in ids) Release(id);
            _carriers.Clear();
        }

        /// <summary>
        /// What a click could mark right now, asked of the very check the click goes through — the
        /// draft's, so a node reachable only through something already marked is offered. Nothing is
        /// marked in the giving-back mode: everything held can go back, and a ring on every taken node is
        /// noise rather than an answer.
        /// </summary>
        private void RefreshFrontier()
        {
            _frontier.Clear();
            if (_document == null || _draft == null || _draft.Mode == DraftMode.Refund) return;

            foreach (PassiveNode node in _document.Nodes)
                if (_draft.CanMark(node.Id) == AllocationResult.Success)
                    _frontier.Add(node.Id);
        }

        private void RefreshSocketMarks()
        {
            if (_document == null || _board == null || _style == null) return;

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
            {
                string? address = AddressOf(_document.Find(entry.Key));
                bool occupied = address != null && _board.Find(address) is { IsEmpty: false };
                entry.Value.SetSocketMark(occupied ? _style.SocketMark : null);
            }
        }

        /// <summary>
        /// The slot a socket node stands for, built from the DOCUMENT with the board's own constructor.
        /// Not asked of the board: a slot exists there only while its node is taken, and a drop aimed at a
        /// node nobody has bought has to be refused for the reason it was refused for — "you have not
        /// bought it" — rather than for "there is no such slot".
        /// </summary>
        private static string? AddressOf(PassiveNode? node)
        {
            if (node == null) return null;

            int tier = NodeKindRules.SocketTier(node.Kind);
            if (tier == NodeKindRules.NoSocket || string.IsNullOrWhiteSpace(node.AbilityId)) return null;

            return new AbilitySocketPlacement(node.Id, node.AbilityId, tier).Address;
        }

        /// <summary>The slot a point on the wheel aims at: a pip if the pointer is on one, otherwise the
        /// socket node under it.</summary>
        private DropSlot? SlotAt(WheelTarget target)
        {
            if (target.Pip is { } pip) return new DropSlot(pip.Address, pip.OpenerId);
            if (target.Node is not { } node || AddressOf(node) is not { } address) return null;

            return new DropSlot(address, node.Id);
        }

        /// <summary>The wheel's own refusals, answered before the install gate is ever asked and read off
        /// the allocation and the plan — see <see cref="PassiveSocketRefusalText"/>.</summary>
        private string? OwnRefusal(string openerId) =>
            PassiveSocketRefusalText.KeyFor(_tree?.IsTaken(openerId) == true, PlannedReturn(openerId));

        /// <summary>The node is held and the plan means to give it back.</summary>
        private bool PlannedReturn(string nodeId) =>
            _tree?.IsTaken(nodeId) == true && _draft?.IsProjected(nodeId) == false;

        private void PreviewDrag()
        {
            if (_sockets == null || GetViewport()?.GuiGetDragData() is not { } data) return;
            if (!DragPayloadReader.TryReadInstance(data, out string instanceId)) return;

            HashSet<string> accepting = new(StringComparer.Ordinal);

            foreach (PassiveSocketPip pip in _socketRings?.Pips ?? [])
                if (OwnRefusal(pip.OpenerId) == null && _sockets.Judge(pip.Address, instanceId).Installed)
                    accepting.Add(pip.Address);

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
            {
                string? address = AddressOf(_document?.Find(entry.Key));
                if (address == null) continue;

                bool accepted = OwnRefusal(entry.Key) == null && _sockets.Judge(address, instanceId).Installed;
                entry.Value.SetDropTarget(accepted);
                if (accepted) accepting.Add(address);
            }

            _socketRings?.SetDropPreview(accepting);
        }

        /// <summary>
        /// The one reading of "what is under the cursor", and it answers a PAIR. A pip is asked about
        /// first and without any slack of its own, so its priority holds exactly inside its own small
        /// circle; everything else falls through to the node rule, which has the slack that keeps a dot
        /// aimable when the whole tree is on screen.
        /// <para>A pip always resolves to the node whose ring it belongs to as well, so hovering one opens
        /// that node's popup and every gesture still has a node to reach for. WHICH half of the pair a
        /// button is about is asked once, of <see cref="WheelHit.LeftClickOn"/> and of
        /// <see cref="OnRightClick"/>, and never here — this reads the surface and decides nothing.</para>
        /// </summary>
        private WheelTarget Resolve(Vector2 position)
        {
            if (_document == null || _geometry == null) return default;

            if (WheelHit.PipAt(_socketRings?.Pips ?? [], _view, position.X, position.Y) is { } pip)
                return new WheelTarget(_document.Find(pip.NodeId), pip);

            return new WheelTarget(_geometry.At(_document, _view, position.X, position.Y), null);
        }

        private void ClearHover() => Hover(null);

        /// <summary>The one reading of "what is hovered". The views never hear about it: hover happens to
        /// nodes with a scene and to nodes without one alike, so the rim is drawn by the one layer that
        /// serves both.</summary>
        private void Hover(PassiveNode? node)
        {
            if (node?.Id == _hovered) return;

            _hovered = node?.Id;
            UpdatePathPreview();

            _route?.SetHovered(node);
            _rim?.SetHovered(node);
            _tooltip?.Target(_hovered);
        }

        /// <summary>The cheapest route to what the cursor is on, priced FROM THE PLAN, and its length is
        /// what reaching it costs. The draft walks the graph — the window never grows a search of its
        /// own. Nothing is previewed while a return is being planned: there is no route to draw, and what
        /// the click would take is said in the popup instead.</summary>
        private void UpdatePathPreview()
        {
            _path = _hovered == null || _draft == null || _draft.Mode == DraftMode.Refund
                ? []
                : _draft.PathTo(_hovered);

            _route?.SetPath(_path);

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
                entry.Value.SetState(StateOf(entry.Key));
        }

        private void Announce(string text) => StatusChanged?.Invoke(text);

        /// <summary>
        /// Everything the node says, in one popup: its name once, its class, its lines, what reaching it
        /// would cost, what its ability's slots hold, and — while a return is being planned — how much
        /// the click would take back and what that would cost in gold.
        /// </summary>
        private IPopup? ShowNodeTooltip(object? key)
        {
            if (_windows == null || _document == null || key is not string id) return null;

            PassiveNode? node = _document.Find(id);
            if (node == null) return null;
            if (_windows.ShowPopup(typeof(TextTooltipPopup)) is not TextTooltipPopup popup) return null;

            var body = new StringBuilder();
            foreach (PassiveNodeLine line in PassiveNodeLines.Of(node, _modifiers, _knobs, _localization, TextFormat.Rich))
                Append(body, line.Text);

            AppendCost(body, node);
            AppendSlots(body, node);
            AppendReturn(body, node);

            popup.Show(PassiveNodeLines.TitleOf(node, _localization),
                Localization.Localize($"{PassiveWheelText.KindPrefix}{node.Kind}"), body.ToString());

            return popup;
        }

        /// <summary>What reaching the node would cost in points, priced from the plan so a step already
        /// marked is not charged for twice. Said only where it says something the wheel does not — see
        /// <see cref="PassiveNodeLines.PricesTheRoute"/>.</summary>
        private void AppendCost(StringBuilder body, PassiveNode node)
        {
            if (_draft == null || _draft.Mode == DraftMode.Refund) return;

            int cost = _draft.PathTo(node.Id).Count;
            if (!PassiveNodeLines.PricesTheRoute(cost)) return;

            Append(body, Localization.Render(PassiveWheelText.PathCost,
                new Dictionary<string, object?> { [PassiveWheelText.PointsValue] = cost }));
        }

        /// <summary>The ability's slots in words. The ring beside the node says the same thing in colour,
        /// and an icon inside a pip eight pixels across would be unreadable — so what is IN a slot is
        /// printed here and shown nowhere else.</summary>
        private void AppendSlots(StringBuilder body, PassiveNode node)
        {
            if (!_rings.TryGetValue(node.Id, out List<SocketRingSlot>? slots)) return;

            Append(body, Localization.Localize(PassiveWheelText.SlotsCaption));
            foreach (SocketRingSlot slot in slots)
            {
                AbilitySocket? socket = _board?.Find(slot.Address);
                string key = SocketRings.StateOf(socket) switch
                {
                    SocketSlotState.Held => PassiveWheelText.SlotHeld,
                    SocketSlotState.Open => PassiveWheelText.SlotOpen,
                    SocketSlotState.Filled => PassiveWheelText.SlotFilled,
                    _ => PassiveWheelText.SlotUnopened
                };

                Append(body, Localization.Render(key, new Dictionary<string, object?>
                {
                    [PassiveWheelText.TierValue] = slot.Tier,
                    [PassiveWheelText.NameValue] = socket?.Augment is { } augment
                        ? Localization.Localize(augment.AugmentId)
                        : string.Empty
                }));
            }
        }

        /// <summary>
        /// What clicking would give back and what it would cost — said BEFORE the click, and beside the
        /// node rather than at the cursor, because the popup is already following the cursor and a second
        /// number chasing it would only flicker.
        /// <para>The price printed is the price of the WHOLE plan once this node joins it, not of the tail
        /// on its own: the pricing has a ceiling, so a tail priced separately would stop adding up to the
        /// figure on the button the moment the plan reached it.</para>
        /// </summary>
        private void AppendReturn(StringBuilder body, PassiveNode node)
        {
            if (_draft == null || _draft.Mode != DraftMode.Refund || _tree?.IsTaken(node.Id) != true) return;
            if (PlannedReturn(node.Id)) return;

            IReadOnlyList<string> tail = _draft.RefundTailOf(node.Id);
            Append(body, Localization.Render(PassiveWheelText.RefundTail,
                new Dictionary<string, object?> { [PassiveWheelText.CountValue] = tail.Count }));

            if (_quotes is { CanCharge: true })
            {
                RespecQuote quote = _quotes.Quote(_draft.PendingRefunds.Count + tail.Count);
                string gold = Localization.Render(PassiveWheelText.RefundPrice,
                    new Dictionary<string, object?> { [PassiveWheelText.GoldValue] = quote.Gold });

                Append(body, TextPalette.Colorize(gold, quote.Affordable ? TextPalette.Number : TextPalette.Debuff));
            }

            int stranded = StrandedAugments(tail);
            if (stranded > 0)
                Append(body, Localization.Render(PassiveWheelText.StrandedAugments,
                    new Dictionary<string, object?> { [PassiveWheelText.CountValue] = stranded }));
        }

        /// <summary>How many augments the return would leave sitting in closed slots. The warning the
        /// respec dialog used to carry, and it has to outlive the dialog: property of the player's ends up
        /// somewhere he has to go and fetch it from.</summary>
        private int StrandedAugments(IReadOnlyList<string> tail)
        {
            if (_board == null) return 0;

            int stranded = 0;
            foreach (AbilitySocket socket in _board.Sockets)
                if (!socket.IsEmpty && Contains(tail, socket.SocketId))
                    stranded++;

            return stranded;
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            foreach (string candidate in ids)
                if (string.Equals(candidate, id, StringComparison.Ordinal))
                    return true;

            return false;
        }

        private static void Append(StringBuilder body, string line)
        {
            if (line.Length == 0) return;
            if (body.Length > 0) body.Append('\n');

            body.Append(line);
        }

        /// <summary>The control has no size until the layout has run, and framing a tree into nothing
        /// would leave the wheel off screen. Framed once, on the first size it actually gets.</summary>
        private void OnResized()
        {
            if (_framed) return;

            FrameAll();
        }

        /// <summary>What a point on the wheel means: the node it belongs to, and the pip of that node's
        /// socket ring if the point landed on one.</summary>
        private readonly record struct WheelTarget(PassiveNode? Node, PassiveSocketPip? Pip);

        /// <summary>A slot a drop could go into, and the node that has to be bought for it to exist.</summary>
        private readonly record struct DropSlot(string Address, string OpenerId);
    }
}
