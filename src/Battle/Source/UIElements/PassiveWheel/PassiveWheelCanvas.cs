namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Localization;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.View;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The wheel itself: the only node of the whole screen the engine treats as a mouse target, and the
    /// only place that decides what a point on it means.
    ///
    /// <para>Everything inside is drawn in DOCUMENT coordinates within one <see cref="Node2D"/> frame.
    /// PANNING writes one field of that frame and nothing else — no layer is redrawn, no view is moved
    /// — which is why the tree can be dragged around at any size without touching the scene tree.
    /// ZOOMING writes the frame too, but the frame only scales what has already been drawn, and a
    /// picture scaled is not a picture drawn: glyphs keep the size they were rasterised at, a screen
    /// floor stops being the size it promised and a width authored in screen pixels stops being that
    /// many. So a wheel click also re-derives everything that is measured in pixels and hands it down
    /// (<see cref="ApplyScale"/>) — five redraws and a scale per view, at a click of the wheel and at
    /// the two other things that take the same road (a reframe and an allocation pass), never per
    /// frame.</para>
    ///
    /// <para>The frame is a MIRROR of <see cref="CanvasTransform"/>, written by <see cref="ApplyTransform"/>
    /// and by nothing else. Picking reads the transform, the GPU reads the frame, and the two agree only
    /// because there is one writer: an animation that flew the view to a node by tweening the frame
    /// would silently move every click somewhere else for as long as it ran.</para>
    ///
    /// <para>Hit testing is one rule for the whole surface (<see cref="NodeGeometry.At"/>, nearest centre
    /// wins). The engine's own picking is not used and cannot be: the node scenes are Node2D with every
    /// control under them locked out of the mouse pass, because the authored layout has around a hundred
    /// and fifty pairs of overlapping shapes and the GUI walk would resolve them by the order of lines
    /// in a file that is rewritten daily. Dropping an augment lands on this node too and resolves the
    /// target the same way, so there is one rule and not two.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveWheelCanvas : Control, IRequireServices
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://cqv8mz4rt6nd3";

        [Export] private Node2D? _rig;
        [Export] private WheelBackdropLayer? _backdrop;
        [Export] private WheelEdgeLayer? _idleEdges;
        [Export] private WheelEdgeLayer? _takenEdges;
        [Export] private WheelFieldLayer? _field;
        [Export] private WheelCursorLayer? _cursor;
        [Export] private Node2D? _nodeViews;

        /// <summary>Above the views and empty today: a one-off effect belongs over the node it happens
        /// to, and a layer added later would have to be slid in under everything already sitting here.</summary>
        [Export] private Node2D? _effects;

        [Export] private PackedScene? _nodeViewScene;
        [Export] private PassiveWheelStyle? _style;

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
        private readonly Stack<PassiveNodeView> _pool = new();
        private readonly HashSet<string> _carriers = new(StringComparer.Ordinal);
        private readonly HashSet<string> _frontier = new(StringComparer.Ordinal);

        private NodeGeometry? _geometry;
        private IPassiveTreeService? _tree;
        private IAbilitySocketBoard? _board;
        private IAugmentCellHost? _sockets;
        private IUiElementsManager? _windows;
        private ModifierFormatter? _modifiers;
        private ContextModifierFormatter? _knobs;
        private ILocalizationProvider? _localization;
        private HoverTooltipHandle? _tooltip;

        private PassiveTreeDocument? _document;
        private IReadOnlyList<string> _path = [];
        private string? _hovered;
        private string? _selected;

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

        /// <summary>The node under the cursor, or null when the pointer left every node.</summary>
        public event Action<PassiveNode?>? HoveredChanged;

        public event Action<PassiveNode?>? SelectionChanged;

        /// <summary>One line for the window to print: the price of a route, or why the last gesture was
        /// refused. Said synchronously, where the player is looking.</summary>
        public event Action<string>? StatusChanged;

        public PassiveNode? Selected => _selected == null ? null : _document?.Find(_selected);

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
            _tooltip = HoverTooltip.Follow(this, ShowNodeTooltip);
            Rebuild();
        }

        /// <summary>Where an augment seated from the wheel goes: the socket panel beside it, which is
        /// already the one road from a cell to the install gate. The wheel writes no request of its own
        /// — a second copy of that road would be a second reading of the same rule.</summary>
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

        /// <summary>Buys the selected node through the allocation service — the same road a click on it
        /// takes, so the card's button and the wheel cannot disagree.</summary>
        public void TakeSelected()
        {
            PassiveNode? node = Selected;
            if (node != null) Take(node);
        }

        public void RefundSelected()
        {
            PassiveNode? node = Selected;
            if (node != null) Refund(node);
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Lights up every socket node that would take the copy the moment a drag starts, and
        /// clears the lot when it ends — one pass per drag rather than one per mouse move.</summary>
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
                    Announce(string.Empty);
                    if (_reconcilePending) OnAllocationChanged();
                    break;
            }
        }

        /// <summary>The drop check, answered from the same gate the install goes through and about the
        /// node the same rule says is under the cursor.</summary>
        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (_sockets == null || !DragPayloadReader.TryReadInstance(data, out string instanceId)) return false;

            PassiveNode? node = NodeAt(atPosition);
            string? address = AddressOf(node);
            if (node == null || address == null) return false;

            AugmentInstallResult verdict = _sockets.Judge(address, instanceId);
            if (_views.TryGetValue(node.Id, out PassiveNodeView? view)) view.SetDropTarget(verdict.Installed);
            Announce(verdict.Installed ? string.Empty : Localization.Localize(AugmentRefusalText.KeyFor(verdict)));

            return verdict.Installed;
        }

        /// <summary>Selects first, then installs: the panel beside the wheel shows the selected node's
        /// ability, so the asynchronous half of the answer lands under the right one.</summary>
        public override void _DropData(Vector2 atPosition, Variant data)
        {
            if (_sockets == null || !DragPayloadReader.TryReadInstance(data, out string instanceId)) return;

            PassiveNode? node = NodeAt(atPosition);
            string? address = AddressOf(node);
            if (node == null || address == null) return;

            Select(node);
            _sockets.Install(address, instanceId);
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
                    else EndPress(button.ButtonIndex, button.Position);
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

        /// <summary>A press that did not travel is that button's click; one that did was a pan and
        /// buys, sells and selects nothing.</summary>
        private void EndPress(MouseButton button, Vector2 position)
        {
            if (_pressed != button) return;

            _pressed = MouseButton.None;
            if (_panning)
            {
                PanEnded(position);
                return;
            }

            PassiveNode? node = NodeAt(position);
            if (button == MouseButton.Right) OnRightClick(node);
            else OnLeftClick(node);
        }

        /// <summary>The wheel moved under a hand that has come to rest, so what the pointer is on has
        /// changed with no motion event to say so. Read once when the drag lets go — during it the
        /// pointer is holding the view, not asking about a node.</summary>
        private void PanEnded(Vector2 position)
        {
            _panning = false;
            Hover(NodeAt(position));
        }

        private void OnLeftClick(PassiveNode? node)
        {
            if (node == null)
            {
                Select(null);
                return;
            }

            Select(node);
            Take(node);
        }

        private void OnRightClick(PassiveNode? node)
        {
            if (node == null)
            {
                Select(null);
                return;
            }

            Refund(node);
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

            Hover(NodeAt(motion.Position));
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
        /// floor under an edge width, a ring measured off a node's screen size, the length of a dash and
        /// the point size of a caption. Scaling that picture instead of drawing it again is what turned
        /// captions into blur and rings into halos, so a wheel click redraws the lot.</para>
        /// <para>The layers are handed the transform's two readings of scale and never the transform, so
        /// none of them can reach the pan and start culling. Whether a caption is shown still hangs on
        /// the ZOOM alone: it is a question about how small the glyphs would be, and the spread makes
        /// nothing smaller.</para>
        /// <para>Deliberately unconditional, and reached from a reframe and from an allocation pass as
        /// well as from the wheel — those two hand the layers a scale they already had and pay the same
        /// price for it. All three are rare, deliberate acts; five layer redraws and one scale per node
        /// scene is what an honest picture costs, and none of it is work per frame: none of these
        /// classes has a <c>_Process</c>.</para>
        /// </summary>
        private void ApplyScale()
        {
            if (_geometry == null || _style == null) return;

            bool labels = _view.Zoom >= _style.LabelZoomThreshold;
            float frameScale = _view.Frame().Scale;

            _backdrop?.SetScale(_view, labels);
            _idleEdges?.SetScale(_view);
            _takenEdges?.SetScale(_view);
            _field?.SetScale(_view);
            _cursor?.SetScale(_view);

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
            {
                PassiveNode? node = _document?.Find(entry.Key);
                if (node == null) continue;

                // The scene ends up on screen at frameScale x bodyScale, so that product is what a
                // caption inside it has to undo to land at the point size it was rasterised at.
                float bodyScale = _geometry.ViewScale(node.Kind, _view);
                float onScreen = frameScale * bodyScale;
                entry.Value.ApplyZoom(bodyScale, onScreen <= 0f ? 1f : 1f / onScreen, labels);
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
            _selected = null;
            _path = [];
            _tooltip?.Cancel();

            _idleEdges?.SetDocument(_document);
            _takenEdges?.SetDocument(_document);
            _field?.SetDocument(_document);
            _field?.SetGeometry(_geometry);
            _cursor?.SetDocument(_document);
            _cursor?.SetGeometry(_geometry);
            _cursor?.SetHovered(null);
            _cursor?.SetPath([]);

            ReconcileAllocation();
            HoveredChanged?.Invoke(null);
            SelectionChanged?.Invoke(null);
            FrameAll();
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

            RefreshSocketMarks();
        }

        /// <summary>
        /// One pass over the allocation for the whole wheel. The taken set is read ONCE and answers both
        /// halves of the same question — which nodes get a scene and which are left to the mass layer —
        /// so a node cannot end up drawn twice or not at all. A second road into the view set would be
        /// the one way to break that.
        /// </summary>
        private void ReconcileAllocation()
        {
            if (_document == null) return;

            IReadOnlyCollection<string> taken = _tree?.TakenNodes ?? [];

            if (CanCarryViews()) NodeCarriers.Collect(_document, taken, HasOwnView, _carriers);
            else _carriers.Clear();

            PromoteAndDemote();

            _field?.SetCarriers(_carriers);
            _takenEdges?.SetTaken(taken);
            _cursor?.SetTaken(taken);

            RefreshFrontier();
            _cursor?.SetFrontier(_frontier);

            UpdatePathPreview();
            RefreshSocketMarks();
            ApplyScale();
        }

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

                view.SetNode(node, _style);
                _views[id] = view;
            }

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
            {
                entry.Value.SetState(StateOf(entry.Key));
                entry.Value.SetSelected(entry.Key == _selected);
            }
        }

        private PassiveNodeVisualState StateOf(string id)
        {
            if (_tree?.IsTaken(id) == true) return PassiveNodeVisualState.Taken;

            foreach (string step in _path)
                if (string.Equals(step, id, StringComparison.Ordinal))
                    return PassiveNodeVisualState.OnPath;

            return PassiveNodeVisualState.Idle;
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
            view.SetSelected(false);
            view.SetSocketMark(null);
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
        /// What can be bought right now, asked of the very check a click goes through. Deliberately not
        /// the allocation's own frontier walk: that one tests the budget once before the loop and knows
        /// nothing about the classes that cost no point, so the ring it draws could offer a node the
        /// click then refuses.
        /// </summary>
        private void RefreshFrontier()
        {
            _frontier.Clear();
            if (_document == null || _tree == null) return;

            foreach (PassiveNode node in _document.Nodes)
                if (_tree.CheckTake(node.Id) == AllocationResult.Success)
                    _frontier.Add(node.Id);
        }

        private void RefreshSocketMarks()
        {
            if (_document == null || _board == null || _style == null) return;

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
            {
                PassiveNode? node = _document.Find(entry.Key);
                string? address = AddressOf(node);
                bool occupied = address != null && _board.Find(address) is { IsEmpty: false };
                entry.Value.SetSocketMark(occupied ? _style.SocketMark : null);
            }
        }

        /// <summary>
        /// The slot a socket node opens, asked of the board rather than assembled here. The signature of
        /// a slot is built in one place, and asking for it also answers for free whether the slot exists
        /// at all — it exists only while the node is taken.
        /// </summary>
        private string? AddressOf(PassiveNode? node)
        {
            if (node == null || _board == null) return null;
            if (NodeKindRules.SocketTier(node.Kind) == NodeKindRules.NoSocket) return null;
            if (string.IsNullOrWhiteSpace(node.AbilityId)) return null;

            foreach (AbilitySocket socket in _board.SocketsOf(node.AbilityId))
                if (string.Equals(socket.SocketId, node.Id, StringComparison.Ordinal))
                    return socket.Address;

            return null;
        }

        private void PreviewDrag()
        {
            if (_sockets == null || GetViewport()?.GuiGetDragData() is not { } data) return;
            if (!DragPayloadReader.TryReadInstance(data, out string instanceId)) return;

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
            {
                string? address = AddressOf(_document?.Find(entry.Key));
                if (address == null) continue;

                entry.Value.SetDropTarget(_sockets.Judge(address, instanceId).Installed);
            }
        }

        private PassiveNode? NodeAt(Vector2 position) =>
            _document == null || _geometry == null ? null : _geometry.At(_document, _view, position.X, position.Y);

        private void ClearHover() => Hover(null);

        /// <summary>The one reading of "what is under the cursor". The views never hear about it: hover
        /// happens to nodes with a scene and to nodes without one alike, so the ring is drawn by the one
        /// layer that serves both.</summary>
        private void Hover(PassiveNode? node)
        {
            if (node?.Id == _hovered) return;

            _hovered = node?.Id;
            UpdatePathPreview();

            _cursor?.SetHovered(node);
            _tooltip?.Target(_hovered);
            HoveredChanged?.Invoke(node);
        }

        /// <summary>The cheapest route to what the cursor is on, and its length is what reaching it
        /// costs. The service walks the graph — the window never grows a search of its own.</summary>
        private void UpdatePathPreview()
        {
            _path = _hovered == null || _tree == null ? [] : _tree.PathTo(_hovered);
            _cursor?.SetPath(_path);

            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
                entry.Value.SetState(StateOf(entry.Key));
        }

        private void Select(PassiveNode? node)
        {
            if (node?.Id == _selected) return;

            _selected = node?.Id;
            foreach (KeyValuePair<string, PassiveNodeView> entry in _views)
                entry.Value.SetSelected(entry.Key == _selected);

            SelectionChanged?.Invoke(node);
        }

        /// <summary>Buying goes straight to the service: it is already the single gate, its answer is
        /// needed synchronously to say why a refusal happened, and the picture that follows comes from
        /// the allocation event rather than from this call — which is what keeps this window and the
        /// mastery window showing the same numbers.</summary>
        private void Take(PassiveNode node)
        {
            if (_tree == null || _document == null) return;

            AllocationResult result = _tree.Take(node.Id);
            Announce(result == AllocationResult.Success
                ? string.Empty
                : Localization.Localize(PassiveTreeRefusalText.TakeKey(_document, node.Id, result)));
        }

        private void Refund(PassiveNode node)
        {
            if (_tree == null) return;

            AllocationResult result = _tree.Refund(node.Id);
            Announce(result == AllocationResult.Success
                ? string.Empty
                : Localization.Localize(PassiveTreeRefusalText.RefundKey(result)));
        }

        private void Announce(string text) => StatusChanged?.Invoke(text);

        private IPopup? ShowNodeTooltip(object? key)
        {
            if (_windows == null || _document == null || key is not string id) return null;

            PassiveNode? node = _document.Find(id);
            if (node == null) return null;
            if (_windows.ShowPopup(typeof(TextTooltipPopup)) is not TextTooltipPopup popup) return null;

            List<PassiveNodeLine> lines = PassiveNodeLines.Of(node, _modifiers, _knobs, _localization, TextFormat.Rich);
            var body = new System.Text.StringBuilder();
            foreach (PassiveNodeLine line in lines)
            {
                if (body.Length > 0) body.Append('\n');
                body.Append(line.Text);
            }

            popup.Show(PassiveNodeLines.TitleOf(node, _localization),
                Localization.Localize($"{PassiveWheelText.KindPrefix}{node.Kind}"), body.ToString());

            return popup;
        }

        /// <summary>The control has no size until the layout has run, and framing a tree into nothing
        /// would leave the wheel off screen. Framed once, on the first size it actually gets.</summary>
        private void OnResized()
        {
            if (_framed) return;

            FrameAll();
        }
    }
}
