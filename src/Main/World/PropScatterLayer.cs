namespace LastBreath.World
{
    using System.Collections.Generic;
    using Core;
    using Core.World.DualGrid;
    using Godot;
    using Helpers.Extensions;

    /// <summary>
    /// Grows sprite props over the cells painted on an invisible world <see cref="TileMapLayer"/> — the same
    /// data a <see cref="DualGridTerrainLayer"/> reads to paint its ground. Painting a meadow makes tufts sprout
    /// on it and erasing one takes them away, with no prop data stored anywhere: the layout is a pure function
    /// of the cell address, so the meadow comes back identical every load.
    /// </summary>
    /// <remarks>
    /// Props are anchored at their root and placed in a Y-sorted container, which is what lets a character wade
    /// through them: a tuft rooted lower on the screen than the character's feet draws over them, one rooted
    /// higher draws behind. The container therefore has to be the same Y-sort pool the characters live in.
    /// <para>
    /// The scatter also grows in the editor. Its props are marked and unowned, so the scene never saves them,
    /// and the container — a scene node the preview only borrows — is not written to there.
    /// </para>
    /// </remarks>
    [Tool]
    [GlobalClass]
    public partial class PropScatterLayer : Node2D
    {
        /// <summary>Invisible layer holding the painted cells. Read only — this node never writes to it.</summary>
        [Export] private TileMapLayer? _world;

        /// <summary>
        /// Node the props are parented to. Leave empty to use this node. Y-sorting is switched on here, so the
        /// props merge into the sort pool of whatever Y-sorted ancestor the container sits under.
        /// </summary>
        [Export] private Node2D? _container;

        [Export] private Texture2D? _prop;

        /// <summary>Height of an unscaled prop in world pixels; the texture is fitted to it.</summary>
        [Export] private float _propHeight = 96f;

        /// <summary>Layer-wide seed. Change it to reroll the whole meadow without touching a single cell.</summary>
        [Export] private int _seed;

        [Export] private int _minPropsPerCell = 2;

        [Export] private int _maxPropsPerCell = 4;

        /// <summary>Keep-out band along the cell border, in pixels, so roots never line up on the cell seams.</summary>
        [Export] private float _edgeMargin = 24f;

        [Export] private float _minScale = 0.9f;

        [Export] private float _maxScale = 1.1f;

        [Export] private float _tintJitter = 0.08f;

        /// <summary>The props standing right now, by the cell that grew them — the snapshot the delta runs against.</summary>
        private readonly Dictionary<GridCoordinate, Sprite2D[]> _grown = [];

        private bool _rescatterQueued;

        /// <summary>
        /// Wires the scatter from code, for a container that builds it at runtime instead of carrying it in a
        /// scene. Call it before the node enters the tree: <see cref="_Ready"/> is where the first scatter runs.
        /// <see cref="PropScatterSettings.CellSize"/> is ignored — the cell is measured off the world layer's
        /// own tileset. Scene-authored layers keep using the exports untouched.
        /// </summary>
        public void Bind(TileMapLayer world, Node2D? container, Texture2D prop, float propHeight, PropScatterSettings settings)
        {
            _world = world;
            _container = container;
            _prop = prop;
            _propHeight = propHeight;
            _seed = settings.Seed;
            _minPropsPerCell = settings.MinPropsPerCell;
            _maxPropsPerCell = settings.MaxPropsPerCell;
            _edgeMargin = settings.EdgeMargin;
            _minScale = settings.MinScale;
            _maxScale = settings.MaxScale;
            _tintJitter = settings.TintJitter;
        }

        public override void _Ready()
        {
            if (!SourcesAssigned()) return;

            _container ??= this;
            EnableSorting();
            // Deferred, not synchronous: this _Ready can fire while the container's own parent (e.g. TerrainRoot
            // being added under MainWorld) is still busy setting up its children, and a synchronous AddChild of
            // a grown prop into it would fail. QueueRescatter already exists for the same reason on Changed.
            QueueRescatter();
            _world!.Changed += QueueRescatter;
        }

        public override void _ExitTree()
        {
            if (_world is not null) _world.Changed -= QueueRescatter;
        }

        /// <summary>
        /// Switches the props' pool into Y-sorting. A borrowed container belongs to the scene, so in the editor
        /// the setting stays its author's to make: writing it there would edit the scene the preview only draws
        /// over.
        /// </summary>
        private void EnableSorting()
        {
            if (Engine.IsEditorHint() && _container != this) return;

            _container!.YSortEnabled = true;
        }

        private bool SourcesAssigned()
        {
            if (_world is null) Tracker.TrackError("PropScatterLayer has no world layer assigned: there are no painted cells to scatter over", this);
            if (_prop is null) Tracker.TrackError("PropScatterLayer has no prop texture assigned: there is nothing to grow", this);

            return _world is not null && _prop is not null;
        }

        /// <summary>
        /// The change signal names no coordinates and fires once per brush step, so the rescatter is coalesced
        /// to the end of the frame and then only touches the cells that appeared or vanished.
        /// </summary>
        private void QueueRescatter()
        {
            if (_rescatterQueued) return;

            _rescatterQueued = true;
            Callable.From(Rescatter).CallDeferred();
        }

        private void Rescatter()
        {
            _rescatterQueued = false;
            if (!CanGrow()) return;

            HashSet<GridCoordinate> painted = [];
            foreach (Vector2I cell in _world!.GetUsedCells()) painted.Add(cell.ToGridCoordinate());

            List<GridCoordinate> cleared = [];
            foreach (GridCoordinate cell in _grown.Keys)
            {
                if (!painted.Contains(cell)) cleared.Add(cell);
            }

            foreach (GridCoordinate cell in cleared) Wither(cell);

            foreach (GridCoordinate cell in painted)
            {
                if (!_grown.ContainsKey(cell)) Grow(cell);
            }
        }

        /// <summary>
        /// The deferred rescatter outlives the layer: an edit and a QueueFree in the same frame land on freed
        /// nodes, and a preview swept away by a rebuild is off the tree while its call is still in the queue —
        /// growing then would leave props behind with nobody to take them away.
        /// </summary>
        private bool CanGrow() =>
            IsInstanceValid(this) && IsInsideTree() && IsInstanceValid(_world) && IsInstanceValid(_container);

        private void Grow(GridCoordinate cell)
        {
            IReadOnlyList<PropPlacement> placements = PropScatter.PlacementsFor(cell, ReadSettings());
            Vector2 corner = CellCorner(cell);
            float fit = FitScale();
            Sprite2D[] props = new Sprite2D[placements.Count];

            for (int index = 0; index < placements.Count; index++)
            {
                PropPlacement placement = placements[index];
                float scale = fit * placement.Scale;
                Sprite2D prop = new()
                {
                    Texture = _prop,
                    // Origin at the root: the drawn rect ends up spanning half a texture left and right of the
                    // node and a full texture above it, so the node's own Y is the Y the sort compares.
                    Centered = true,
                    Offset = new Vector2(0f, -_prop!.GetHeight() / 2f),
                    // Mirroring rides on a negative X scale rather than FlipH, which would fight the offset.
                    Scale = new Vector2(placement.Mirrored ? -scale : scale, scale),
                    Modulate = Shade(placement.TintShift),
                    // Position before AddChild: a body entering the tree already placed never teleports.
                    Position = corner + new Vector2(placement.OffsetX, placement.OffsetY)
                };

                // Marked and left without an Owner: the editor neither saves the prop into the borrowed
                // container's scene nor loses track of it when the next build sweeps the previous one away.
                GeneratedTerrainNodes.Mark(prop);
                _container!.AddChild(prop);
                props[index] = prop;
            }

            _grown[cell] = props;
        }

        private void Wither(GridCoordinate cell)
        {
            if (!_grown.Remove(cell, out Sprite2D[]? props)) return;

            foreach (Sprite2D prop in props)
            {
                if (IsInstanceValid(prop)) prop.QueueFree();
            }
        }

        /// <summary>Top-left corner of a world cell, expressed in the container's space.</summary>
        private Vector2 CellCorner(GridCoordinate cell)
        {
            Vector2 half = CellSize() / 2f;

            return _container!.ToLocal(_world!.ToGlobal(_world.MapToLocal(cell.ToVector2I()) - half));
        }

        private Vector2 CellSize() =>
            _world!.TileSet is TileSet tileSet ? tileSet.TileSize : new Vector2(128f, 128f);

        /// <summary>Shrinks the texture to the configured world height; a prop asset is authored much larger.</summary>
        private float FitScale()
        {
            int height = _prop!.GetHeight();

            return height > 0 ? _propHeight / height : 1f;
        }

        private static Color Shade(float shift)
        {
            float channel = Mathf.Max(0f, 1f + shift);

            return new Color(channel, channel, channel);
        }

        private PropScatterSettings ReadSettings() => new()
        {
            CellSize = CellSize().X,
            Seed = _seed,
            MinPropsPerCell = _minPropsPerCell,
            MaxPropsPerCell = _maxPropsPerCell,
            EdgeMargin = _edgeMargin,
            MinScale = _minScale,
            MaxScale = _maxScale,
            TintJitter = _tintJitter
        };
    }
}
