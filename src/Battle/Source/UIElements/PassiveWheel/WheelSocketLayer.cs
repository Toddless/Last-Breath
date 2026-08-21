namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>One drawn place of a ring, and the whole of what the canvas needs to resolve a click on
    /// it. Centre and radius are in DOCUMENT units, the screen radius beside them, so drawing and picking
    /// read the same two numbers.</summary>
    /// <param name="NodeId">The ability node the ring belongs to — what hovering or clicking the pip
    /// means, since a ring is part of its own node's presence and not a target of its own.</param>
    /// <param name="OpenerId">The node that has to be taken for this slot to exist.</param>
    public readonly record struct PassiveSocketPip(
        string Address,
        string NodeId,
        string OpenerId,
        SocketSlotState State,
        float X,
        float Y,
        float DocumentRadius,
        float ScreenRadius);

    /// <summary>
    /// The augment slots of every ability the character has unlocked, drawn as a small fan of pips
    /// around the node that handed the ability over. The socket nodes keep their own places on the
    /// wheel — a branch is still bought to reach them and each still costs a point — and the ring does
    /// not replace them: it SHOWS, beside the ability, how many of its slots are lit and what is in
    /// them.
    ///
    /// <para>Two readings that never mix. WHAT A RING IS MADE OF comes from the document
    /// (<see cref="SocketRings"/>) and is collected once per catalog load. WHAT EACH PLACE IS DOING is
    /// one lookup on the board at build time. The board holds a socket exactly while the node behind it
    /// is taken, and a plan never reaches the board, so a node the player has only MARKED lights nothing
    /// — the rule needs no guard of its own.</para>
    ///
    /// <para>A ring is drawn only around a node the character actually HOLDS. A planned purchase is
    /// already said by the node's own body and by the dashed outline, and a second picture of the same
    /// plan in a different vocabulary is one more thing to read and nothing more to learn.</para>
    /// </summary>
    [GlobalClass]
    public partial class WheelSocketLayer : Node2D
    {
        /// <summary>Straight pieces per screen pixel of radius, and the ends of the range — the same
        /// rule the cursor's rings are drawn under, for the same reason.</summary>
        private const float SegmentsPerPixel = 1.6f;

        private const int FewestSegments = 8;
        private const int MostSegments = 24;

        /// <summary>Weight of an outline in screen pixels: a pip is an affordance rather than part of the
        /// map, so it keeps its weight however deep the wheel is zoomed.</summary>
        private const float OutlineWidth = 1.5f;

        /// <summary>How much of a held pip's diameter the strike through it spans.</summary>
        private const float StrikeSpan = 0.7f;

        [Export] private PassiveWheelStyle? _style;

        private readonly Dictionary<string, List<SocketRingSlot>> _rings = new(StringComparer.Ordinal);
        private readonly HashSet<string> _taken = new(StringComparer.Ordinal);
        private readonly List<PassiveSocketPip> _pips = [];

        private HashSet<string>? _dropTargets;
        private PassiveTreeDocument? _document;
        private NodeGeometry? _nodes;
        private SocketRingGeometry? _geometry;
        private IAbilitySocketBoard? _board;
        private ICanvasScale? _scale;
        private PassiveNodeVisualIndex<PassiveNodeVisualConfig>? _visuals;

        /// <summary>Every pip on screen, in the order they were laid out. The canvas reads this to decide
        /// what a point on the wheel means — one table, so what is drawn and what answers a click cannot
        /// come apart.</summary>
        public IReadOnlyList<PassiveSocketPip> Pips => _pips;

        public void SetDocument(PassiveTreeDocument? document)
        {
            _document = document;
            Rebuild();
        }

        public void SetGeometry(NodeGeometry? nodes, SocketRingGeometry? geometry)
        {
            _nodes = nodes;
            _geometry = geometry;
            Rebuild();
        }

        /// <summary>What each ability node's ring is made of, collected off the document.</summary>
        public void SetRings(IReadOnlyDictionary<string, List<SocketRingSlot>> rings)
        {
            _rings.Clear();
            foreach (KeyValuePair<string, List<SocketRingSlot>> ring in rings) _rings[ring.Key] = ring.Value;
            Rebuild();
        }

        public void SetBoard(IAbilitySocketBoard? board)
        {
            _board = board;
            Rebuild();
        }

        /// <summary>What the character HOLDS, and never a projection: a ring is the picture of slots that
        /// exist, and the slots of a planned node do not.</summary>
        public void SetTaken(IReadOnlyCollection<string> taken)
        {
            _taken.Clear();
            foreach (string id in taken) _taken.Add(id);
            Rebuild();
        }

        public void SetScale(ICanvasScale scale)
        {
            _scale = scale;
            Rebuild();
        }

        /// <summary>The authored looks, or null when the wheel carries no library. A ring reads the row of
        /// the NODE it hangs off — the ring is part of that node's presence, so its art is that node's
        /// art and not a thing addressed on its own.</summary>
        public void SetVisuals(PassiveNodeVisualIndex<PassiveNodeVisualConfig>? visuals)
        {
            _visuals = visuals;
            QueueRedraw();
        }

        /// <summary>The addresses that would accept what is being dragged, or null when no drag is in the
        /// air. One pass per drag rather than one per mouse move.</summary>
        public void SetDropPreview(HashSet<string>? addresses)
        {
            _dropTargets = addresses;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_style == null) return;

            foreach (PassiveSocketPip pip in _pips) DrawPip(pip);
        }

        /// <summary>Lays the whole board of pips out again. Cheap by construction — one ring per unlocked
        /// ability and a handful of pips each — and reached only by the four things that can move it: the
        /// document, the board, an allocation pass and a zoom step.</summary>
        private void Rebuild()
        {
            _pips.Clear();
            if (_document == null || _nodes == null || _geometry == null || _scale == null)
            {
                QueueRedraw();
                return;
            }

            foreach (KeyValuePair<string, List<SocketRingSlot>> ring in _rings)
            {
                if (!_taken.Contains(ring.Key)) continue;

                PassiveNode? node = _document.Find(ring.Key);
                if (node == null || !_geometry.Draws(node.Kind, _scale.Zoom)) continue;

                // Which way the fan opens is a property of the NODE, not of any one place on it: read
                // once here rather than re-derived from the graph for every pip.
                SocketRingFan fan = _geometry.Fan(_document, node, ring.Value.Count);

                for (int index = 0; index < ring.Value.Count; index++)
                    _pips.Add(Pip(node, fan, ring.Value[index], index, ring.Value.Count));
            }

            QueueRedraw();
        }

        private PassiveSocketPip Pip(PassiveNode node, SocketRingFan fan, SocketRingSlot slot, int index, int count)
        {
            SocketPipPlacement placement = _geometry!.Place(fan, node, _scale!, index, count);

            return new PassiveSocketPip(
                slot.Address, node.Id, slot.OpenerId, SocketRings.StateOf(_board?.Find(slot.Address)),
                placement.X, placement.Y, placement.DocumentRadius, placement.ScreenRadius);
        }

        private void DrawPip(PassiveSocketPip pip)
        {
            if (_style == null) return;

            var centre = new Vector2(pip.X, pip.Y);
            int segments = Segments(pip.ScreenRadius);
            Color color = ColorOf(pip);

            if (pip.State == SocketSlotState.Filled) DrawFilled(centre, pip, color);
            else
                DrawArc(centre, pip.DocumentRadius, 0f, Mathf.Tau, segments, color, FromScreen(OutlineWidth), true);

            if (pip.State != SocketSlotState.Held) return;

            // A closed slot is remove-only, and that is a different sentence from "empty": struck
            // through, it reads as a place with something stuck in it rather than as a place to fill.
            var reach = new Vector2(pip.DocumentRadius * StrikeSpan, 0f);
            DrawLine(centre - reach, centre + reach, color, FromScreen(OutlineWidth));
        }

        /// <summary>
        /// The picture of a slot that HOLDS something: the authored face of the ability's own row when it
        /// carries one, otherwise the filled dot. Only this state takes a face — a slot with nothing in it
        /// keeps its outline, so the ring goes on telling full from empty by SHAPE and not by colour
        /// alone, and the state colour still modulates whatever is drawn.
        /// </summary>
        private void DrawFilled(Vector2 centre, PassiveSocketPip pip, Color color)
        {
            if (FaceOf(pip) is not { } face)
            {
                DrawCircle(centre, pip.DocumentRadius, color);
                return;
            }

            float span = pip.DocumentRadius * 2f;
            DrawTextureRect(face, new Rect2(centre - new Vector2(pip.DocumentRadius, pip.DocumentRadius),
                new Vector2(span, span)), false, color);
        }

        private Texture2D? FaceOf(PassiveSocketPip pip)
        {
            if (_visuals == null) return null;

            PassiveNode? node = _document?.Find(pip.NodeId);
            return node == null ? null : _visuals.For(node)?.FilledSocketPip;
        }

        /// <summary>A drag in the air overrides the state: while the player is carrying an augment the
        /// only question a pip answers is whether it would take it.</summary>
        private Color ColorOf(PassiveSocketPip pip)
        {
            if (_dropTargets != null && _dropTargets.Contains(pip.Address)) return _style!.SocketDropTarget;

            return pip.State switch
            {
                SocketSlotState.Filled => _style!.SocketFilled,
                SocketSlotState.Open => _style!.SocketOpen,
                SocketSlotState.Held => _style!.SocketHeld,
                _ => _style!.SocketUnopened
            };
        }

        private static int Segments(float screenRadius) =>
            Math.Clamp((int)MathF.Ceiling(screenRadius * SegmentsPerPixel), FewestSegments, MostSegments);

        private float FromScreen(float pixels) => _scale?.DocumentLength(pixels) ?? pixels;
    }
}
