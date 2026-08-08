namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Enums;
    using Core.PassiveTree;
    using Godot;

    /// <summary>
    /// Everything the wheel looks like, in one resource an artist edits without a compiler: the palette,
    /// the backdrop geometry, the per-class node looks and the two screen floors every size in the
    /// wheel is derived from.
    /// <para>The authoring tool has a look of its own and keeps it. The tool has to write good json,
    /// not to look good, so the two drifting apart is not a risk — what they share is the geometry
    /// (<see cref="Core.PassiveTree.View.NodeGeometry"/>) and nothing else.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveWheelStyle : Resource
    {
        [Export] private Godot.Collections.Array<PassiveNodeVisual> _visuals = [];

        /// <summary>Ring radii of the backdrop, in document units — the same units node coordinates
        /// are authored in, so a ring named after a design radius passes through the nodes on it.</summary>
        [Export] private float[] _ringRadii = [];

        [Export] private Godot.Collections.Array<WheelSector> _sectors = [];

        [Export] private float _sectorRadius = 548f;
        [Export] private float _sectorHalfAngleDegrees = 30f;
        [Export] private float _coreGlowRadius = 112f;
        [Export] private float _ringWidth = 2f;

        [Export] private Color _void = new(0.043f, 0.035f, 0.024f);
        [Export] private Color _ringLine = new(0.227f, 0.188f, 0.125f);
        [Export] private Color _coreGlow = new(0.227f, 0.173f, 0.082f);

        [Export] private Color _edgeIdle = new(0.169f, 0.141f, 0.094f);
        [Export] private Color _edgeTaken = new(0.941f, 0.831f, 0.537f);
        [Export] private Color _edgePath = new(0.941f, 0.831f, 0.537f);

        [Export] private Color _frontier = new(0.847f, 0.706f, 0.369f);
        [Export] private Color _hover = new(0.945f, 0.906f, 0.827f);
        [Export] private Color _select = new(1f, 0.965f, 0.875f);

        [Export] private Color _nodeIdleFill = new(0.086f, 0.067f, 0.039f);
        [Export] private Color _nodeIdleOutline = new(0.298f, 0.259f, 0.192f);

        [Export] private Color _strength = new(0.776f, 0.325f, 0.251f);
        [Export] private Color _dexterity = new(0.435f, 0.620f, 0.333f);
        [Export] private Color _intelligence = new(0.302f, 0.518f, 0.741f);
        [Export] private Color _neutral = new(0.847f, 0.706f, 0.369f);

        [Export] private Color _wedgeStrengthDexterity = new(0.690f, 0.518f, 0.196f);
        [Export] private Color _wedgeDexterityIntelligence = new(0.184f, 0.561f, 0.541f);
        [Export] private Color _wedgeIntelligenceStrength = new(0.541f, 0.388f, 0.678f);

        /// <summary>Authored edge widths in document units; the floors below keep them visible when
        /// the whole wheel is on screen.</summary>
        [Export] private float _idleEdgeWidth = 1.6f;

        [Export] private float _takenEdgeWidth = 3f;

        [Export] private float _minEdgeScreenWidth = 1f;

        /// <summary>How small a node may get on screen before it stops shrinking. One of the two
        /// numbers every size in the wheel is derived from.</summary>
        [Export] private float _minNodeScreenRadius = 1.5f;

        [Export] private float _pickScreenSlack = 6f;

        [Export] private float _labelZoomThreshold = 0.55f;

        [Export] private int _labelFontSize = 13;

        /// <summary>Put on a socket node whose slot is occupied. The augment's own icon lives in the
        /// socket panel, where there is room to read it; on the wheel the question is only whether the
        /// slot is full.</summary>
        [Export] private Texture2D? _socketMark;

        private Dictionary<PassiveNodeKind, PassiveNodeVisual>? _byKind;

        public Color Void => _void;

        public Color RingLine => _ringLine;

        public Color CoreGlow => _coreGlow;

        public Color EdgeIdle => _edgeIdle;

        public Color EdgeTaken => _edgeTaken;

        public Color EdgePath => _edgePath;

        public Color Frontier => _frontier;

        public Color Hover => _hover;

        public Color Select => _select;

        public Color NodeIdleFill => _nodeIdleFill;

        public Color NodeIdleOutline => _nodeIdleOutline;

        public IReadOnlyList<float> RingRadii => _ringRadii;

        public IReadOnlyList<WheelSector> Sectors => _sectors;

        public float SectorRadius => _sectorRadius;

        public float SectorHalfAngleDegrees => _sectorHalfAngleDegrees;

        public float CoreGlowRadius => _coreGlowRadius;

        public float RingWidth => _ringWidth;

        public float IdleEdgeWidth => _idleEdgeWidth;

        public float TakenEdgeWidth => _takenEdgeWidth;

        public float MinNodeScreenRadius => _minNodeScreenRadius;

        public float PickScreenSlack => _pickScreenSlack;

        public float LabelZoomThreshold => _labelZoomThreshold;

        public int LabelFontSize => _labelFontSize;

        public Texture2D? SocketMark => _socketMark;

        /// <summary>The widest class on the board. Computed from the table, never a second export that
        /// could disagree with it.</summary>
        public float MaxRadius
        {
            get
            {
                float largest = 0f;
                foreach (PassiveNodeVisual visual in ByKind.Values)
                    if (visual.Radius > largest)
                        largest = visual.Radius;

                return largest;
            }
        }

        public PassiveNodeVisual Visual(PassiveNodeKind kind) => ByKind[kind];

        /// <summary>The authored radii, the way <see cref="Core.PassiveTree.View.NodeGeometry"/> wants
        /// them. The only channel from the look to the geometry, so the size a node is drawn at and the
        /// size a click is measured against come from one table.</summary>
        public IReadOnlyDictionary<PassiveNodeKind, float> Radii()
        {
            var radii = new Dictionary<PassiveNodeKind, float>();
            foreach (KeyValuePair<PassiveNodeKind, PassiveNodeVisual> entry in ByKind)
                radii[entry.Key] = entry.Value.Radius;

            return radii;
        }

        /// <summary>Hue of the node: its ray, the wedge it bridges when it is a hybrid, or the neutral
        /// tone when it belongs to no ray at all.</summary>
        public Color ColorOf(PassiveNode node) =>
            node.IsHybrid
                ? WedgeColor(node.Stance!.Value, node.HybridStance!.Value)
                : RayColor(node.Stance);

        public Color RayColor(Stance? stance) => stance switch
        {
            Stance.Strength => _strength,
            Stance.Dexterity => _dexterity,
            Stance.Intelligence => _intelligence,
            _ => _neutral
        };

        /// <summary>An authored width in document units, floored so it stays visible on screen. Written
        /// once and read by both edge layers: an idle edge that vanished while a taken one held would
        /// be a wheel that looks like it lost its links.</summary>
        public float DocumentEdgeWidth(float authoredWidth, float zoom) =>
            zoom <= 0f ? authoredWidth : MathF.Max(authoredWidth, _minEdgeScreenWidth / zoom);

        /// <summary>Built once and reported rather than thrown: an artist half way through filling the
        /// resource must still get a wheel he can look at, and a class with no row gets a placeholder
        /// that is visible enough to be noticed.</summary>
        private Dictionary<PassiveNodeKind, PassiveNodeVisual> ByKind
        {
            get
            {
                if (_byKind != null) return _byKind;

                _byKind = [];
                foreach (PassiveNodeVisual visual in _visuals) _byKind[visual.Kind] = visual;

                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                {
                    if (_byKind.ContainsKey(kind)) continue;

                    Tracker.TrackNotFound($"Passive wheel style has no visual for node class '{kind}'", this);
                    _byKind[kind] = new PassiveNodeVisual { Kind = kind, HasView = false, Labelled = false };
                }

                return _byKind;
            }
        }

        private Color WedgeColor(Stance first, Stance second)
        {
            bool strength = first == Stance.Strength || second == Stance.Strength;
            bool dexterity = first == Stance.Dexterity || second == Stance.Dexterity;
            bool intelligence = first == Stance.Intelligence || second == Stance.Intelligence;

            if (strength && dexterity) return _wedgeStrengthDexterity;
            if (dexterity && intelligence) return _wedgeDexterityIntelligence;
            if (intelligence && strength) return _wedgeIntelligenceStrength;

            return _neutral;
        }
    }
}
