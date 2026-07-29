namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Model;
    using Simulation;

    /// <summary>
    /// The tree surface: draws the graph in immediate mode and owns navigation, picking and editing
    /// gestures. Both drawing and hit-testing go through the document's spatial grid, so their cost
    /// follows what is on screen rather than how big the tree is.
    /// </summary>
    public partial class TreeCanvas : Control
    {
        private const float MinZoom = 0.08f;
        private const float MaxZoom = 4f;
        private const float ZoomStep = 1.15f;

        /// <summary>World-space slack added to the visible box: covers the largest node radius plus a
        /// grid cell, so a node being dragged never blinks out at the edge.</summary>
        private const float CullMargin = 220f;

        /// <summary>Turns a square's half-extent into the circumradius its polygon vertices sit on.</summary>
        private const float SquareCircumradius = 1.41421356f;

        private const float PickScreenSlack = 6f;
        private const float FineSnap = 1f;
        private const float CoarseSnap = 25f;

        private readonly List<PassiveNode> _drawCandidates = [];
        private readonly List<PassiveNode> _pickCandidates = [];
        private readonly HashSet<string> _selected = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector2> _dragOrigins = new(StringComparer.Ordinal);

        /// <summary>Last node created of each class — the pattern a new node of that class copies
        /// when nothing is selected to continue from.</summary>
        private readonly Dictionary<PassiveNodeKind, PassiveNode> _lastCreated = new();

        private PassiveTreeDocument _document = new();
        private AllocationState _allocation = new();
        private HashSet<string> _frontier = new(StringComparer.Ordinal);
        private List<string> _path = [];
        private HashSet<string> _pathSet = new(StringComparer.Ordinal);

        private PopupMenu _kindMenu = null!;
        private PassiveNodeKind? _lastKind;
        private Vector2 _pendingCreateWorld;
        private EditorMode _mode = EditorMode.Select;
        private Vector2 _panOffset = new(700f, 450f);
        private float _zoom = 1f;
        private string? _hovered;
        private string? _linkFrom;
        private bool _panning;
        private bool _draggingNodes;
        private bool _boxSelecting;
        private bool _dragMoved;
        private Vector2 _dragStartWorld;
        private Vector2 _boxStartWorld;
        private Vector2 _boxEndWorld;

        public event Action? SelectionChanged;

        public event Action? DocumentChanged;

        public event Action? AllocationChanged;

        public event Action<string>? StatusChanged;

        /// <summary>The node under the cursor, or null when the cursor left every node.</summary>
        public event Action<PassiveNode?>? HoveredChanged;

        public EditorMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                ClearPathPreview();
            }
        }

        /// <summary>The wheel's rings and sectors from the draft, drawn behind the tree as layout
        /// guides. They are geometry to aim at, never something nodes snap to.</summary>
        public bool ShowGuides { get; set; } = true;

        public PassiveTreeDocument Document => _document;

        public AllocationState Allocation => _allocation;

        public IReadOnlyCollection<string> Selection => _selected;

        public PassiveNode? SingleSelection =>
            _selected.Count == 1 ? _document.Find(FirstSelected()) : null;

        public override void _Ready()
        {
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            ClipContents = true;

            _kindMenu = new PopupMenu();
            _kindMenu.IdPressed += id => CreateNode((PassiveNodeKind)id, _pendingCreateWorld);
            AddChild(_kindMenu);

            MouseExited += ClearHover;
        }

        private void ClearHover()
        {
            if (_hovered is null) return;

            _hovered = null;
            UpdatePathPreview();
            HoveredChanged?.Invoke(null);
            QueueRedraw();
        }

        public void SetDocument(PassiveTreeDocument document, AllocationState allocation)
        {
            _document = document;
            _allocation = allocation;
            _selected.Clear();
            _linkFrom = null;
            ClearHover();
            ClearPathPreview();
            RefreshFrontier();
            SelectionChanged?.Invoke();
            QueueRedraw();
        }

        public void RefreshFrontier()
        {
            _frontier = _allocation.Frontier(_document, _document.Budget);
            QueueRedraw();
        }

        public void SelectOnly(string id)
        {
            _selected.Clear();
            if (_document.Contains(id)) _selected.Add(id);
            SelectionChanged?.Invoke();
            QueueRedraw();
        }

        /// <summary>Fits the whole tree on screen. Also the recovery hatch when panning has taken the
        /// view somewhere far away from the content.</summary>
        public void FrameAll()
        {
            if (_document.Nodes.Count == 0)
            {
                _panOffset = Size / 2f;
                _zoom = 1f;
                QueueRedraw();
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

            var extent = new Vector2(MathF.Max(maxX - minX, 1f), MathF.Max(maxY - minY, 1f));
            Vector2 available = Size - new Vector2(160f, 160f);
            _zoom = Mathf.Clamp(MathF.Min(available.X / extent.X, available.Y / extent.Y), MinZoom, MaxZoom);

            var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            _panOffset = Size / 2f - center * _zoom;
            QueueRedraw();
        }

        private Vector2 ToScreen(float worldX, float worldY) => new(worldX * _zoom + _panOffset.X, worldY * _zoom + _panOffset.Y);

        private Vector2 ToWorld(Vector2 screen) => (screen - _panOffset) / _zoom;

        private string FirstSelected()
        {
            foreach (string id in _selected) return id;
            return string.Empty;
        }

        // ── drawing ────────────────────────────────────────────────────────────────────────────

        public override void _Draw()
        {
            DrawRect(new Rect2(Vector2.Zero, Size), CanvasStyle.Void);
            if (ShowGuides) DrawGuides();

            Vector2 topLeft = ToWorld(Vector2.Zero);
            Vector2 bottomRight = ToWorld(Size);
            float minX = topLeft.X - CullMargin;
            float minY = topLeft.Y - CullMargin;
            float maxX = bottomRight.X + CullMargin;
            float maxY = bottomRight.Y + CullMargin;

            DrawEdges(minX, minY, maxX, maxY);
            DrawPendingLink();
            DrawNodes(minX, minY, maxX, maxY);
            DrawSelectionBox();
        }

        /// <summary>The wheel behind the tree: six 60° sectors, the four ring radii and the core glow.
        /// Pure decoration for the eye and a target for the hand — it never touches node data.</summary>
        private void DrawGuides()
        {
            Vector2 origin = ToScreen(0f, 0f);

            DrawSectors(origin);

            foreach (float ring in CanvasStyle.RingRadii) DrawDashedRing(origin, ring * _zoom);

            DrawCoreGlow(origin);

            if (_zoom >= CanvasStyle.LabelZoomThreshold) DrawGuideLabels(origin);
        }

        private void DrawSectors(Vector2 origin)
        {
            const int segments = 14;
            float radius = CanvasStyle.SectorRadius * _zoom;
            if (radius < 8f) return;

            foreach ((float angle, Color color, string _) in CanvasStyle.Sectors)
            {
                var points = new Vector2[segments + 2];
                points[0] = origin;

                for (int step = 0; step <= segments; step++)
                {
                    float degrees = angle - CanvasStyle.SectorHalfAngle
                                    + 2f * CanvasStyle.SectorHalfAngle * step / segments;
                    float radians = Mathf.DegToRad(degrees);
                    points[step + 1] = origin + new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * radius;
                }

                DrawColoredPolygon(points, new Color(color, 0.07f));
            }
        }

        /// <summary>Godot draws solid arcs, so the mockup's dashed rings are every other segment of a
        /// coarse circle — cheap and visually identical at these radii.</summary>
        private void DrawDashedRing(Vector2 origin, float radius)
        {
            if (radius < 6f) return;

            const int segments = 144;
            for (int step = 0; step < segments; step += 2)
            {
                float from = Mathf.Tau * step / segments;
                float to = Mathf.Tau * (step + 1) / segments;
                DrawArc(origin, radius, from, to, 2, CanvasStyle.RingLine, 1f, true);
            }
        }

        /// <summary>Stand-in for the mockup's radial gradient: a few nested discs of falling alpha.</summary>
        private void DrawCoreGlow(Vector2 origin)
        {
            const int layers = 7;
            float radius = CanvasStyle.CoreGlowRadius * _zoom;
            if (radius < 4f) return;

            for (int layer = layers; layer > 0; layer--)
            {
                float factor = (float)layer / layers;
                DrawCircle(origin, radius * factor, new Color(CanvasStyle.CoreGlow, 0.10f * (1f - factor) + 0.04f));
            }
        }

        private void DrawGuideLabels(Vector2 origin)
        {
            Font font = GetThemeDefaultFont();
            int fontSize = LabelSize();

            for (int ring = 0; ring < CanvasStyle.RingRadii.Length; ring++)
            {
                float radius = CanvasStyle.RingRadii[ring] * _zoom;
                DrawString(font, new Vector2(origin.X + 6f, origin.Y - radius + fontSize * 0.4f), $"R{ring}",
                    HorizontalAlignment.Left, -1f, fontSize, CanvasStyle.RingLabel);
            }

            float labelRadius = (CanvasStyle.SectorRadius + 28f) * _zoom;
            foreach ((float angle, Color color, string label) in CanvasStyle.Sectors)
            {
                float radians = Mathf.DegToRad(angle);
                Vector2 at = origin + new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * labelRadius;
                DrawString(font, new Vector2(at.X - 120f, at.Y), label,
                    HorizontalAlignment.Center, 240f, fontSize, new Color(color, 0.85f));
            }
        }

        private int LabelSize() => (int)Mathf.Clamp(13f * _zoom, 9f, 20f);

        private void DrawEdges(float minX, float minY, float maxX, float maxY)
        {
            float idleWidth = MathF.Max(1f, 1.6f * _zoom);
            float liveWidth = MathF.Max(1.5f, 3f * _zoom);

            foreach (NodeLink link in _document.Links)
            {
                PassiveNode? first = _document.Find(link.A);
                PassiveNode? second = _document.Find(link.B);
                if (first is null || second is null) continue;

                // Segment bounding box against the visible box — enough to skip everything off-screen
                // without building a second index for edges.
                if (MathF.Max(first.X, second.X) < minX || MathF.Min(first.X, second.X) > maxX) continue;
                if (MathF.Max(first.Y, second.Y) < minY || MathF.Min(first.Y, second.Y) > maxY) continue;

                Vector2 from = ToScreen(first.X, first.Y);
                Vector2 to = ToScreen(second.X, second.Y);

                bool firstTaken = _allocation.IsTaken(first.Id);
                bool secondTaken = _allocation.IsTaken(second.Id);

                // An edge belongs to the previewed route when it is the step that reaches a path node.
                if (_pathSet.Contains(first.Id) && (_pathSet.Contains(second.Id) || secondTaken)
                    || _pathSet.Contains(second.Id) && firstTaken)
                {
                    DrawDashedLine(from, to, CanvasStyle.EdgePath, liveWidth, MathF.Max(3f, 7f * _zoom));
                    continue;
                }

                if (firstTaken && secondTaken)
                {
                    DrawLine(from, to, new Color(CanvasStyle.NodeColor(first), 0.8f), liveWidth);
                    continue;
                }

                DrawLine(from, to, CanvasStyle.EdgeIdle, idleWidth);
            }
        }

        private void DrawPendingLink()
        {
            if (_linkFrom is null) return;

            PassiveNode? from = _document.Find(_linkFrom);
            if (from is null) return;

            DrawDashedLine(ToScreen(from.X, from.Y), GetLocalMousePosition(), CanvasStyle.Gold1,
                MathF.Max(1.5f, 2f * _zoom), 8f);
        }

        private void DrawNodes(float minX, float minY, float maxX, float maxY)
        {
            _drawCandidates.Clear();
            _document.Index.Query(minX, minY, maxX, maxY, _drawCandidates);

            bool showLabels = _zoom >= CanvasStyle.LabelZoomThreshold;
            Font font = GetThemeDefaultFont();
            int fontSize = LabelSize();

            foreach (PassiveNode node in _drawCandidates)
            {
                NodeVisual visual = CanvasStyle.Visual(node.Kind);
                Vector2 center = ToScreen(node.X, node.Y);
                float radius = visual.Radius * _zoom;
                if (radius < 1.5f) radius = 1.5f;

                bool taken = _allocation.IsTaken(node.Id);
                NodeState state = taken ? NodeState.Taken
                    : _pathSet.Contains(node.Id) ? NodeState.OnPath
                    : NodeState.Idle;

                Color accent = CanvasStyle.NodeColor(node);
                NodeLook look = CanvasStyle.Look(node.Kind, accent, state);

                bool hovered = _hovered == node.Id;
                Color outline = hovered ? CanvasStyle.Gold1 : look.Outline;

                DrawNodeShape(visual.Shape, center, radius, look.Fill, outline, look.OutlineWidth * MathF.Max(0.6f, _zoom));

                // The frontier ring is editor-only feedback: it says "this one is legal next".
                if (Mode == EditorMode.Simulate && !taken && state == NodeState.Idle && _frontier.Contains(node.Id))
                    DrawArc(center, radius + 3f * _zoom, 0f, Mathf.Tau, 24, new Color(CanvasStyle.Gold2, 0.7f),
                        MathF.Max(1f, 1.2f * _zoom), true);

                if (_selected.Contains(node.Id))
                    DrawArc(center, radius + 5f * _zoom, 0f, Mathf.Tau, 28, CanvasStyle.SelectionStroke,
                        MathF.Max(1.5f, 2f * _zoom), true);

                if (!showLabels || !visual.Labelled) continue;

                string text = CanvasStyle.ShortLabel(node);
                if (text.Length == 0) continue;

                Color textColor = node.Kind == PassiveNodeKind.Keystone
                    ? taken ? CanvasStyle.Gold1 : CanvasStyle.Gold3
                    : taken ? CanvasStyle.Ink1 : CanvasStyle.Ink3;

                DrawString(font, new Vector2(center.X - 130f, center.Y + radius + fontSize + 2f), text,
                    HorizontalAlignment.Center, 260f, fontSize, textColor);
            }
        }

        /// <summary>The mockup's shapes, drawn from the shape alone: which class wears which is the
        /// visual table's business, so a new class that reuses a shape costs nothing here.</summary>
        private void DrawNodeShape(NodeShape shape, Vector2 center, float radius, Color fill, Color outline, float outlineWidth)
        {
            float width = MathF.Max(1f, outlineWidth);

            switch (shape)
            {
                case NodeShape.Hexagon:
                    DrawPolygonShape(center, radius, 6, 0f, fill, outline, width);
                    break;
                case NodeShape.RotatedHexagon:
                    DrawPolygonShape(center, radius, 6, Mathf.DegToRad(CanvasStyle.KeystoneRotationDegrees), fill, outline, width);
                    break;
                case NodeShape.Square:
                    // Half-extent to circumradius: an upright square through four polygon vertices.
                    DrawPolygonShape(center, radius * SquareCircumradius, 4, Mathf.Tau / 8f, fill, outline, width);
                    break;
                case NodeShape.Diamond:
                    DrawPolygonShape(center, radius * SquareCircumradius, 4, 0f, fill, outline, width);
                    break;
                case NodeShape.Circle:
                default:
                    DrawCircle(center, radius, fill);
                    DrawArc(center, radius, 0f, Mathf.Tau, 28, outline, width, true);
                    break;
            }
        }

        private void DrawPolygonShape(Vector2 center, float radius, int sides, float rotation, Color fill, Color outline, float outlineWidth)
        {
            var points = new Vector2[sides];
            for (int index = 0; index < sides; index++)
            {
                float angle = rotation + Mathf.Tau * index / sides;
                points[index] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            }

            DrawColoredPolygon(points, fill);

            var closed = new Vector2[sides + 1];
            points.CopyTo(closed, 0);
            closed[sides] = points[0];
            DrawPolyline(closed, outline, outlineWidth);
        }

        private void DrawSelectionBox()
        {
            if (!_boxSelecting) return;

            Vector2 first = ToScreen(_boxStartWorld.X, _boxStartWorld.Y);
            Vector2 second = ToScreen(_boxEndWorld.X, _boxEndWorld.Y);
            var box = new Rect2(
                MathF.Min(first.X, second.X),
                MathF.Min(first.Y, second.Y),
                MathF.Abs(second.X - first.X),
                MathF.Abs(second.Y - first.Y));

            DrawRect(box, CanvasStyle.BoxSelect);
            DrawRect(box, CanvasStyle.BoxSelectBorder, false, 1f);
        }

        // ── input ──────────────────────────────────────────────────────────────────────────────

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
                case InputEventKey { Pressed: true } key:
                    HandleKey(key);
                    break;
            }
        }

        private void HandleMouseButton(InputEventMouseButton button)
        {
            if (button.Pressed && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                Zoom(button.ButtonIndex == MouseButton.WheelUp ? ZoomStep : 1f / ZoomStep, button.Position);
                AcceptEvent();
                return;
            }

            if (button.ButtonIndex is MouseButton.Middle or MouseButton.Right)
            {
                if (button.Pressed && _linkFrom is not null && button.ButtonIndex == MouseButton.Right)
                {
                    _linkFrom = null;
                    QueueRedraw();
                }

                _panning = button.Pressed;
                AcceptEvent();
                return;
            }

            if (button.ButtonIndex != MouseButton.Left) return;

            GrabFocus();

            if (button.Pressed) OnLeftPressed(button);
            else OnLeftReleased();

            AcceptEvent();
        }

        private void OnLeftPressed(InputEventMouseButton button)
        {
            Vector2 world = ToWorld(button.Position);
            PassiveNode? node = NodeAt(world);

            switch (Mode)
            {
                case EditorMode.Add:
                    // Shift skips the picker and repeats the last class: laying a run of small nodes
                    // is one click each again, while the picker stays the way a class is chosen.
                    if (button.ShiftPressed && _lastKind is not null) CreateNode(_lastKind.Value, world);
                    else ShowKindMenu(world);
                    return;

                case EditorMode.Link:
                    if (node is not null) HandleLinkClick(node);
                    return;

                case EditorMode.Simulate:
                    if (node is not null) ToggleAllocation(node, button.CtrlPressed);
                    return;

                case EditorMode.Select:
                default:
                    BeginSelectGesture(node, world, button);
                    return;
            }
        }

        private void BeginSelectGesture(PassiveNode? node, Vector2 world, InputEventMouseButton button)
        {
            bool additive = button.ShiftPressed || button.CtrlPressed;

            if (node is null)
            {
                if (!additive) _selected.Clear();
                _boxSelecting = true;
                _boxStartWorld = world;
                _boxEndWorld = world;
                SelectionChanged?.Invoke();
                QueueRedraw();
                return;
            }

            if (additive)
            {
                if (!_selected.Add(node.Id)) _selected.Remove(node.Id);
            }
            else if (!_selected.Contains(node.Id))
            {
                _selected.Clear();
                _selected.Add(node.Id);
            }

            _draggingNodes = true;
            _dragMoved = false;
            _dragStartWorld = world;
            _dragOrigins.Clear();
            foreach (string id in _selected)
            {
                PassiveNode? selected = _document.Find(id);
                if (selected is not null) _dragOrigins[id] = new Vector2(selected.X, selected.Y);
            }

            SelectionChanged?.Invoke();
            QueueRedraw();
        }

        private void OnLeftReleased()
        {
            if (_boxSelecting)
            {
                ApplyBoxSelection();
                _boxSelecting = false;
                SelectionChanged?.Invoke();
            }

            if (_draggingNodes)
            {
                _draggingNodes = false;
                if (_dragMoved)
                {
                    _document.Reindex();
                    DocumentChanged?.Invoke();
                }
            }

            QueueRedraw();
        }

        private void HandleMouseMotion(InputEventMouseMotion motion)
        {
            if (_panning)
            {
                _panOffset += motion.Relative;
                QueueRedraw();
                return;
            }

            if (_draggingNodes)
            {
                DragSelection(ToWorld(motion.Position), motion.CtrlPressed);
                return;
            }

            if (_boxSelecting)
            {
                _boxEndWorld = ToWorld(motion.Position);
                QueueRedraw();
                return;
            }

            PassiveNode? under = NodeAt(ToWorld(motion.Position));
            if (under?.Id != _hovered)
            {
                _hovered = under?.Id;
                UpdatePathPreview();
                HoveredChanged?.Invoke(under);
                QueueRedraw();
            }
            else if (_linkFrom is not null)
            {
                QueueRedraw();
            }
        }

        /// <summary>
        /// In Simulate mode, hovering an unallocated node shows the cheapest route to it. It answers
        /// the question the designer actually asks of a wheel — "what does reaching this cost me" —
        /// without spending anything.
        /// </summary>
        private void UpdatePathPreview()
        {
            List<string> path = Mode == EditorMode.Simulate && _hovered is not null
                ? _allocation.PathTo(_document, _hovered)
                : [];

            if (path.Count == _path.Count && path.Count == 0) return;

            _path = path;
            _pathSet = new HashSet<string>(path, StringComparer.Ordinal);

            if (path.Count > 0)
                StatusChanged?.Invoke($"{_hovered}: {path.Count} point(s) from here — ctrl+click takes the whole path");
        }

        private void ClearPathPreview()
        {
            if (_path.Count == 0) return;

            _path = [];
            _pathSet.Clear();
        }

        private void HandleKey(InputEventKey key)
        {
            switch (key.Keycode)
            {
                case Key.Delete:
                    DeleteSelection();
                    break;
                case Key.Escape:
                    _linkFrom = null;
                    _selected.Clear();
                    SelectionChanged?.Invoke();
                    QueueRedraw();
                    break;
                case Key.F:
                    FrameAll();
                    break;
            }
        }

        private void Zoom(float factor, Vector2 pivotScreen)
        {
            Vector2 pivotWorld = ToWorld(pivotScreen);
            _zoom = Mathf.Clamp(_zoom * factor, MinZoom, MaxZoom);
            _panOffset = pivotScreen - pivotWorld * _zoom;
            QueueRedraw();
        }

        private void DragSelection(Vector2 world, bool coarse)
        {
            Vector2 delta = world - _dragStartWorld;
            float snap = coarse ? CoarseSnap : FineSnap;

            foreach (KeyValuePair<string, Vector2> origin in _dragOrigins)
            {
                PassiveNode? node = _document.Find(origin.Key);
                if (node is null) continue;

                node.X = MathF.Round((origin.Value.X + delta.X) / snap) * snap;
                node.Y = MathF.Round((origin.Value.Y + delta.Y) / snap) * snap;
            }

            _dragMoved = true;
            QueueRedraw();
        }

        private void ApplyBoxSelection()
        {
            float minX = MathF.Min(_boxStartWorld.X, _boxEndWorld.X);
            float maxX = MathF.Max(_boxStartWorld.X, _boxEndWorld.X);
            float minY = MathF.Min(_boxStartWorld.Y, _boxEndWorld.Y);
            float maxY = MathF.Max(_boxStartWorld.Y, _boxEndWorld.Y);

            _pickCandidates.Clear();
            _document.Index.Query(minX, minY, maxX, maxY, _pickCandidates);

            foreach (PassiveNode node in _pickCandidates)
                if (node.X >= minX && node.X <= maxX && node.Y >= minY && node.Y <= maxY)
                    _selected.Add(node.Id);
        }

        private PassiveNode? NodeAt(Vector2 world)
        {
            // Small nodes shrink to a couple of pixels when the whole tree is on screen; the slack
            // keeps them clickable without making overlapping picks ambiguous when zoomed in.
            float slack = PickScreenSlack / _zoom;
            float reach = CanvasStyle.MaxRadius + slack;

            _pickCandidates.Clear();
            _document.Index.Query(world.X - reach, world.Y - reach, world.X + reach, world.Y + reach, _pickCandidates);

            PassiveNode? best = null;
            float bestDistance = float.MaxValue;

            foreach (PassiveNode node in _pickCandidates)
            {
                float radius = CanvasStyle.Visual(node.Kind).Radius + slack;
                float deltaX = node.X - world.X;
                float deltaY = node.Y - world.Y;
                float distance = deltaX * deltaX + deltaY * deltaY;

                if (distance <= radius * radius && distance < bestDistance)
                {
                    best = node;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>The class picker, opened at the cursor by a click in Add mode. Each entry shows
        /// the id the node would get, so the naming series is visible before committing to it.</summary>
        private void ShowKindMenu(Vector2 world)
        {
            _pendingCreateWorld = world;
            _kindMenu.Clear();

            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
            {
                PassiveNode? template = TemplateFor(kind);
                string label = template is null
                    ? $"{kind}   {_document.NextId(kind)}"
                    : $"{kind}   {_document.NextIdFrom(template.Id)}";

                _kindMenu.AddItem(label, (int)kind);
            }

            _kindMenu.ResetSize();
            _kindMenu.PopupOnParent(new Rect2I((Vector2I)GetGlobalMousePosition(), Vector2I.Zero));
        }

        /// <summary>
        /// The node a new one of this class copies from: whatever is selected if it matches the class
        /// — "continue from this one" — otherwise the last node of that class created here. A node
        /// that has since been deleted stops being a template.
        /// </summary>
        private PassiveNode? TemplateFor(PassiveNodeKind kind)
        {
            if (_selected.Count == 1)
            {
                PassiveNode? selected = _document.Find(FirstSelected());
                if (selected is not null && selected.Kind == kind) return selected;
            }

            if (_lastCreated.TryGetValue(kind, out PassiveNode? last) && _document.Contains(last.Id)) return last;

            // Nothing created this session — a freshly loaded tree. Fall back to the last node of the
            // class in the document so naming series survive a reload instead of restarting at _1.
            PassiveNode? fallback = null;
            foreach (PassiveNode node in _document.Nodes)
                if (node.Kind == kind)
                    fallback = node;

            return fallback;
        }

        private void CreateNode(PassiveNodeKind kind, Vector2 world)
        {
            PassiveNode? selected = _selected.Count == 1 ? _document.Find(FirstSelected()) : null;
            PassiveNode? template = TemplateFor(kind);

            var node = new PassiveNode
            {
                Kind = kind,
                X = MathF.Round(world.X),
                Y = MathF.Round(world.Y)
            };

            if (template is not null)
            {
                // A cluster is a run of near-identical nodes, so the payload rides along and the id
                // continues the series. Editing the template first is what makes the run cheap.
                node.Id = _document.NextIdFrom(template.Id);
                node.Stance = template.Stance;
                node.HybridStance = template.HybridStance;

                // What identifies one particular node does not ride along. An ability reference is
                // unique per node, and an inherited one points at the wrong ability without looking
                // wrong — Check can report an empty field, it cannot report a plausible lie.
                if (!NodeKindRules.For(kind).RequiresAbility)
                {
                    node.Title = template.Title;
                    node.Description = template.Description;

                    foreach (ModifierLine line in template.Modifiers) node.Modifiers.Add(line.Copy());
                }
            }
            else
            {
                node.Id = _document.NextId(kind);
                // No pattern for this class yet — still inherit the ray being worked on.
                node.Stance = selected?.Stance;
                node.HybridStance = selected?.HybridStance;

                for (int line = 0; line < NodeKindRules.For(kind).MinModifiers; line++)
                    node.Modifiers.Add(new ModifierLine());
            }

            if (!_document.AddNode(node)) return;

            // Chaining: with exactly one node selected the fresh node links to it, so a run of nodes
            // is laid out by repeated clicks instead of switching to Link mode after every one.
            if (selected is not null) _document.Link(selected.Id, node.Id);

            _lastCreated[kind] = node;
            _lastKind = kind;
            _selected.Clear();
            _selected.Add(node.Id);

            SelectionChanged?.Invoke();
            DocumentChanged?.Invoke();
            StatusChanged?.Invoke(template is null ? $"added {node.Id}" : $"added {node.Id} from {template.Id}");
            QueueRedraw();
        }

        private void HandleLinkClick(PassiveNode node)
        {
            if (_linkFrom is null)
            {
                _linkFrom = node.Id;
                StatusChanged?.Invoke($"linking from {node.Id} — click the other end, right click to cancel");
                QueueRedraw();
                return;
            }

            if (_linkFrom == node.Id)
            {
                _linkFrom = null;
                QueueRedraw();
                return;
            }

            string from = _linkFrom;
            _linkFrom = null;

            if (_document.AreLinked(from, node.Id))
            {
                _document.Unlink(from, node.Id);
                StatusChanged?.Invoke($"unlinked {from} — {node.Id}");
            }
            else
            {
                _document.Link(from, node.Id);
                StatusChanged?.Invoke($"linked {from} — {node.Id}");
            }

            DocumentChanged?.Invoke();
            QueueRedraw();
        }

        private void ToggleAllocation(PassiveNode node, bool takeWholePath)
        {
            if (_allocation.IsTaken(node.Id))
            {
                if (_allocation.Refund(_document, node.Id)) StatusChanged?.Invoke($"refunded {node.Id}");
                else StatusChanged?.Invoke($"{node.Id} cannot be refunded — something further out depends on it");
            }
            else if (takeWholePath && _path.Count > 0)
            {
                if (_allocation.TakePath(_document, _path, _document.Budget))
                    StatusChanged?.Invoke($"took {_path.Count} node(s) up to {node.Id}");
                else
                    StatusChanged?.Invoke($"the path to {node.Id} needs {_path.Count} point(s), {_document.Budget - _allocation.Spent} left");
            }
            else if (_allocation.Take(_document, node.Id, _document.Budget))
            {
                StatusChanged?.Invoke($"took {node.Id}");
            }
            else if (_allocation.Spent >= _document.Budget)
            {
                StatusChanged?.Invoke($"out of points ({_allocation.Spent}/{_document.Budget})");
            }
            else
            {
                StatusChanged?.Invoke($"{node.Id} is not adjacent to anything taken");
            }

            ClearPathPreview();
            UpdatePathPreview();
            RefreshFrontier();
            AllocationChanged?.Invoke();
        }

        private void DeleteSelection()
        {
            if (_selected.Count == 0) return;

            int removed = 0;
            foreach (string id in new List<string>(_selected))
                if (_document.RemoveNode(id))
                    removed++;

            _selected.Clear();
            ClearHover();
            _allocation.Resync(_document);
            RefreshFrontier();
            SelectionChanged?.Invoke();
            DocumentChanged?.Invoke();
            AllocationChanged?.Invoke();
            StatusChanged?.Invoke($"deleted {removed} node(s)");
            QueueRedraw();
        }
    }
}
