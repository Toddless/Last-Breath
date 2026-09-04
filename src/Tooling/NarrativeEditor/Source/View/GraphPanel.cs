namespace NarrativeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Json;
    using Tooling.Narrative;
    using static Tooling.Text.Format;

    /// <summary>
    /// The conversation on screen as a map: its nodes in columns, one per choice away from the rules that
    /// open it, and every route between them drawn as it is written — the second way out of a roll, the
    /// clause that may hide a choice, the way back to the greeting, and the route naming a node nobody
    /// wrote.
    /// <para>A map and not a second editor: nothing here is dragged, joined or cut. Pressing a node puts
    /// the author on it in the outline, which is where every edit of this tool is made — so the answer to
    /// "where does this go" is one press away from the field that says so.</para>
    /// <para>Redrawn from the document as it is typed rather than on a button: what the map is for is
    /// reading the shape a route has just been given.</para>
    /// </summary>
    public partial class GraphPanel : VBoxContainer
    {
        private const string Title = "graph";

        private const string NoRecordText = "open a dialogue to read it as a map";

        /// <summary>A quest is read through the conversation that offers it: its own stages are a shape of
        /// their own, and drawing them here would be answering a question with the wrong record's map.</summary>
        private const string NotADialogueText = "the record on screen is not a dialogue";

        private const string SummaryFormat = "{0} node(s) in {1} column(s), {2} route(s)";

        private const string DanglingFormat = "{0}   ·   {1} route(s) lead to no node of this dialogue";

        private const string UnreachedFormat = "{0}   ·   {1} node(s) no route reaches";

        /// <summary>How tall the pane asks to be before the divider above it is dragged.</summary>
        private const int PaneHeight = 260;

        private readonly DocumentWatch _watch = new();

        private Label _summary = null!;
        private GraphCanvas _canvas = null!;

        private CatalogRecord? _record;
        private bool _isDialogue;

        /// <summary>Where the author is standing in the outline, so the map marks the node he is editing.</summary>
        private JsonPointer? _standing;

        /// <summary>A redraw is already waiting for the end of the frame. Every keystroke of the inspector
        /// arrives here through the watch, and a map rebuilt per letter is one that flickers under the
        /// hand typing into the field it is drawing.</summary>
        private bool _pending;

        /// <summary>Everything the tool has open. Watched rather than read: the map answers for the
        /// document as it stands, and a record moved into a file laid down since is still the record on
        /// screen.</summary>
        public CatalogWorkspace? Workspace
        {
            get => _watch.Workspace;
            set => _watch.Workspace = value;
        }

        /// <summary>The stack the tool files every step on, so a step back redraws the map with it.</summary>
        public EditHistory? History
        {
            get => _watch.History;
            set => _watch.History = value;
        }

        /// <summary>The node the author pressed, addressed the way the outline addresses its rows.</summary>
        public event Action<JsonPointer>? Chose;

        public override void _Ready()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            CustomMinimumSize = new Vector2(0, PaneHeight);

            _summary = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            _canvas = new GraphCanvas();
            _canvas.Chose += pointer => Chose?.Invoke(pointer);

            // A frame that scrolls both ways: a conversation is as wide as it is deep and as tall as its
            // widest column, and neither is anything the pane can be sized for.
            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            scroll.AddChild(_canvas);

            var heading = new HBoxContainer();
            heading.AddChild(new Label { Text = Title });
            heading.AddChild(_summary);

            AddChild(heading);
            AddChild(scroll);

            _watch.Changed += Redraw;

            Redraw();
        }

        public override void _ExitTree() => _watch.Stop();

        /// <summary>Which record the tool is standing on, and where in it. Nothing is done while all three
        /// are what they were: the documents announce their own edits through the watch, and the rest of
        /// the tool refreshes this panel for keystrokes that never reached the record it is drawing.</summary>
        public void Standing(CatalogRecord? record, bool isDialogue, JsonPointer? standing)
        {
            if (Same(_record, record) && _isDialogue == isDialogue && _standing == standing) return;

            _record = record;
            _isDialogue = isDialogue;
            _standing = standing;

            Redraw();
        }

        /// <summary>Whether two rows are the same place of the same file — nothing on both sides included,
        /// which is where the panel stands while no record is open.</summary>
        private static bool Same(CatalogRecord? one, CatalogRecord? other)
        {
            if (one is null || other is null) return one is null && other is null;

            return ReferenceEquals(one.File, other.File) && one.Pointer == other.Pointer;
        }

        /// <summary>Asks for a redraw at the end of the frame, for the reason the run and the inspector
        /// defer their own: a node can go while the author is pressing it, and tearing the button down
        /// from under the press is what that costs.</summary>
        private void Redraw()
        {
            if (_summary is null || _pending) return;

            _pending = true;
            Callable.From(Rebuild).CallDeferred();
        }

        private void Rebuild()
        {
            _pending = false;

            if (_summary is null || !IsInsideTree()) return;

            DialogueGraph? graph = Map();

            _summary.Text = Summary(graph);
            _canvas.Show(graph, _standing);
        }

        /// <summary>The record on screen read as a map, or nothing while none is open and while the one
        /// that is cannot be read as a conversation.</summary>
        private DialogueGraph? Map()
        {
            if (!_isDialogue || _record is not { } record || record.Token is not { } token) return null;

            return DialogueGraph.Read(token, record.Pointer);
        }

        /// <summary>What the map comes to in one line, the two things worth naming out of it among them:
        /// a route leading out of the conversation and a node nothing opens are what an author reads a map
        /// to find, and a count of them is what says whether it is worth looking.</summary>
        private string Summary(DialogueGraph? graph)
        {
            if (graph is null) return _record is null ? NoRecordText : NotADialogueText;

            string said = Text(SummaryFormat, graph.Nodes.Count, graph.Layers.Count, graph.Edges.Count);

            int dangling = graph.Edges.Count(edge => edge.Dangling);
            int unreached = graph.Nodes.Count(node => !node.Reached);

            if (dangling > 0) said = Text(DanglingFormat, said, dangling);
            if (unreached > 0) said = Text(UnreachedFormat, said, unreached);

            return said;
        }
    }

    /// <summary>
    /// The map itself: a column per layer, a button per node and a line per route, laid out once per
    /// reading and drawn from what that laying out worked out.
    /// <para>The lines are drawn by the canvas and the nodes are its children, which is what puts every
    /// route behind the two nodes it joins: a parent draws before its children do.</para>
    /// </summary>
    public partial class GraphCanvas : Control
    {
        private const int NodeWidth = 240;
        private const int NodeHeight = 32;
        private const int ColumnGap = 110;
        private const int RowGap = 14;
        private const int Margin = 12;
        private const int HeadingHeight = 22;

        /// <summary>How far under the lowest of the two nodes a route that leads back runs.</summary>
        private const int BackLane = 26;

        /// <summary>How far a route naming no node of the dialogue reaches out of the node writing it.</summary>
        private const int StubLength = 56;

        private const float EdgeWidth = 2f;
        private const float DashLength = 6f;
        private const float ArrowLength = 9f;
        private const float ArrowWidth = 4f;
        private const float GuardRadius = 3.5f;
        private const float StandingGrow = 3f;
        private const float StandingWidth = 2f;

        private const string OpeningsTitle = "openings";
        private const string LayerFormat = "layer {0}";
        private const string UnreachedTitle = "nothing opens these";

        private const string NodeFormat = "{0}   ·   {1} line(s) / {2} option(s)";
        private const string EntryFormat = "→ {0}";
        private const string RouteFormat = "{0} → {1}";
        private const string FailedMark = "   ·   lost roll";
        private const string GuardedMark = "   ·   conditions";
        private const string DanglingMark = "   ·   no such node";
        private const string BackMark = "   ·   leads back";
        private const string EntryText = "an entry rule opens the conversation here";
        private const string UnreachedText = "no route of the dialogue reaches this node";
        private const string LeafText = "the node offers nothing: the conversation stops here";
        private const string LineBreak = "\n";

        private static readonly Color s_route = new(0.62f, 0.66f, 0.72f);
        private static readonly Color s_back = new(0.45f, 0.62f, 0.86f);
        private static readonly Color s_dangling = new(0.86f, 0.36f, 0.36f);
        private static readonly Color s_standing = new(0.95f, 0.80f, 0.35f);
        private static readonly Color s_unreached = new(0.60f, 0.60f, 0.62f);

        private readonly List<Drawn> _routes = [];

        /// <summary>Where every node is drawn, by the name the routes call it — the first of two nodes
        /// written under one name, the way the reading itself routes to the first of them.</summary>
        private readonly Dictionary<string, Rect2> _placed = new(StringComparer.Ordinal);

        /// <summary>Where the node the author is editing is drawn, or nothing while he is standing
        /// somewhere the map does not draw — the record itself, one line, one option.</summary>
        private Rect2? _standing;

        /// <summary>The node the reader pressed, addressed the way the outline addresses its rows.</summary>
        public event Action<JsonPointer>? Chose;

        /// <summary>The map itself takes no mouse: the nodes on it are buttons and everything else is a
        /// line, so the wheel belongs to the frame the map is scrolled in.</summary>
        public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

        /// <summary>Lays the map out afresh: the columns, the nodes in them, and the geometry of every
        /// route. Whole and not in part — a conversation is redrawn because its document changed, and what
        /// changed in it may be any of the three.</summary>
        public void Show(DialogueGraph? graph, JsonPointer? standing)
        {
            DryRunRows.Clear(this);

            _routes.Clear();
            _placed.Clear();
            _standing = null;
            CustomMinimumSize = Vector2.Zero;

            if (graph is not null) Lay(graph, standing);

            QueueRedraw();
        }

        public override void _Draw()
        {
            foreach (Drawn route in _routes) Line(route);

            if (_standing is { } rect) DrawRect(rect.Grow(StandingGrow), s_standing, filled: false, StandingWidth);
        }

        private void Lay(DialogueGraph graph, JsonPointer? standing)
        {
            int rows = 0;

            for (int column = 0; column < graph.Layers.Count; column++)
            {
                IReadOnlyList<DialogueGraphNode> layer = graph.Layers[column];
                float x = Margin + (column * (NodeWidth + ColumnGap));

                AddChild(Heading(Titled(layer, column), x));

                for (int row = 0; row < layer.Count; row++)
                {
                    DialogueGraphNode node = layer[row];
                    float y = Margin + HeadingHeight + (row * (NodeHeight + RowGap));
                    var rect = new Rect2(x, y, NodeWidth, NodeHeight);

                    _placed.TryAdd(node.Id, rect);
                    AddChild(Pressable(node, graph, rect));

                    if (Holds(node.Pointer, standing)) _standing = rect;
                }

                rows = Math.Max(rows, layer.Count);
            }

            foreach (DialogueGraphEdge edge in graph.Edges) Route(edge);

            CustomMinimumSize = new Vector2(
                (Margin * 2) + (graph.Layers.Count * NodeWidth) + (Math.Max(graph.Layers.Count - 1, 0) * ColumnGap)
                + StubLength,
                (Margin * 2) + HeadingHeight + (rows * (NodeHeight + RowGap)) + BackLane);
        }

        /// <summary>Whether a node holds the place the author is standing on — the node itself, or one of
        /// the lines and options written inside it. The whole node is marked either way: a line is not a
        /// place of the map, and its author reading a field of it is reading that node.</summary>
        private static bool Holds(JsonPointer node, JsonPointer? standing)
        {
            if (standing is null || standing.Segments.Count < node.Segments.Count) return false;

            for (int step = 0; step < node.Segments.Count; step++)
                if (!string.Equals(node.Segments[step], standing.Segments[step], StringComparison.Ordinal))
                    return false;

            return true;
        }

        /// <summary>What a column is called: the nodes the rules open on, the choices away from them, and
        /// the band nothing arrives at.</summary>
        private static string Titled(IReadOnlyList<DialogueGraphNode> layer, int column)
        {
            if (layer.Count > 0 && !layer[0].Reached) return UnreachedTitle;

            return column == 0 ? OpeningsTitle : Text(LayerFormat, column);
        }

        private static Label Heading(string title, float x) => new()
        {
            Text = title,
            Position = new Vector2(x, Margin),
            CustomMinimumSize = new Vector2(NodeWidth, HeadingHeight),
            Size = new Vector2(NodeWidth, HeadingHeight)
        };

        /// <summary>One node as a button: what it is called, what it says and offers, and the way to the
        /// row of the outline standing for it.</summary>
        private Button Pressable(DialogueGraphNode node, DialogueGraph graph, Rect2 rect)
        {
            var button = new Button
            {
                Text = Named(node),
                TooltipText = Told(node, graph),
                Alignment = HorizontalAlignment.Left,
                ClipText = true,
                Position = rect.Position,
                CustomMinimumSize = rect.Size,
                Size = rect.Size
            };

            if (!node.Reached) button.AddThemeColorOverride("font_color", s_unreached);

            button.Pressed += () => Chose?.Invoke(node.Pointer);

            return button;
        }

        private static string Named(DialogueGraphNode node)
        {
            string said = Text(NodeFormat, node.Id, node.Lines, node.Options);

            return node.Entry ? Text(EntryFormat, said) : said;
        }

        /// <summary>What the node says in full, which is what the button has no room for: what it is, and
        /// every route out of it with what a reader has to know about that route.</summary>
        private static string Told(DialogueGraphNode node, DialogueGraph graph)
        {
            List<string> lines = [Named(node)];

            if (node.Entry) lines.Add(EntryText);
            if (!node.Reached) lines.Add(UnreachedText);
            if (node.Leaf) lines.Add(LeafText);

            lines.AddRange(graph.Edges.Where(edge => edge.From == node.Id).Select(Said));

            return string.Join(LineBreak, lines);
        }

        private static string Said(DialogueGraphEdge edge)
        {
            string said = Text(RouteFormat, edge.Option, edge.To);

            if (edge.Kind == DialogueEdgeKind.FailNext) said += FailedMark;
            if (edge.Guarded) said += GuardedMark;
            if (edge.Dangling) said += DanglingMark;
            if (edge.Back) said += BackMark;

            return said;
        }

        /// <summary>The geometry of one route. Worked out while the map is laid out rather than while it
        /// is drawn: the drawing follows every scroll and the layout follows an edit.</summary>
        private void Route(DialogueGraphEdge edge)
        {
            if (!_placed.TryGetValue(edge.From, out Rect2 from)) return;

            bool dashed = edge.Kind == DialogueEdgeKind.FailNext;

            if (edge.Dangling)
            {
                Vector2 stub = Right(from);

                _routes.Add(new Drawn([stub, stub + new Vector2(StubLength, 0)], s_dangling, dashed, edge.Guarded));
                AddChild(Stub(edge, stub));

                return;
            }

            if (!_placed.TryGetValue(edge.To, out Rect2 to)) return;

            if (!edge.Back)
            {
                _routes.Add(new Drawn([Right(from), Left(to)], s_route, dashed, edge.Guarded));
                return;
            }

            // A route that does not advance runs under both of its nodes: drawn straight, it would lie
            // over the columns between them and read as a route of theirs.
            float lane = Math.Max(from.End.Y, to.End.Y) + BackLane;
            Vector2 start = Under(from);
            Vector2 end = Under(to);

            _routes.Add(new Drawn(
                [start, new Vector2(start.X, lane), new Vector2(end.X, lane), end], s_back, dashed, edge.Guarded));
        }

        /// <summary>The name a route leading out of the conversation says, written where the route ends:
        /// the node it means is nowhere on the map, and a line into empty space names nothing.</summary>
        private static Label Stub(DialogueGraphEdge edge, Vector2 at)
        {
            var label = new Label { Text = edge.To, Position = at + new Vector2(StubLength, -HeadingHeight) };

            label.AddThemeColorOverride("font_color", s_dangling);

            return label;
        }

        private void Line(Drawn route)
        {
            for (int step = 1; step < route.Points.Length; step++)
            {
                Vector2 from = route.Points[step - 1];
                Vector2 to = route.Points[step];

                if (route.Dashed) DrawDashedLine(from, to, route.Colour, EdgeWidth, DashLength);
                else DrawLine(from, to, route.Colour, EdgeWidth);
            }

            Arrow(route.Points[^1], route.Points[^2], route.Colour);

            if (route.Guarded) DrawCircle(Middle(route.Points), GuardRadius, route.Colour);
        }

        /// <summary>The head of a route, at the end it arrives by.</summary>
        private void Arrow(Vector2 at, Vector2 from, Color colour)
        {
            Vector2 heading = (at - from).Normalized();
            Vector2 across = new(-heading.Y, heading.X);
            Vector2 back = at - (heading * ArrowLength);

            DrawColoredPolygon([at, back + (across * ArrowWidth), back - (across * ArrowWidth)], colour);
        }

        /// <summary>The middle of a route, which is where what is worth marking about it is drawn. The
        /// middle of its middle segment: a route that runs round has three of them, and the mark belongs
        /// on the one an eye follows rather than on a corner.</summary>
        private static Vector2 Middle(Vector2[] points) =>
            (points[(points.Length / 2) - 1] + points[points.Length / 2]) / 2f;

        private static Vector2 Right(Rect2 rect) => new(rect.End.X, rect.Position.Y + (rect.Size.Y / 2f));

        private static Vector2 Left(Rect2 rect) => new(rect.Position.X, rect.Position.Y + (rect.Size.Y / 2f));

        private static Vector2 Under(Rect2 rect) => new(rect.Position.X + (rect.Size.X / 2f), rect.End.Y);

        /// <summary>One route as the canvas draws it: the corners it runs through, its colour, whether it
        /// is the way a lost roll goes, and whether a clause stands on it.</summary>
        private sealed record Drawn(Vector2[] Points, Color Colour, bool Dashed, bool Guarded);
    }
}
