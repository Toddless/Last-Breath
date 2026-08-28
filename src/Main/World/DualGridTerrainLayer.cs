namespace LastBreath.World
{
    using System.Collections.Generic;
    using Core;
    using Core.World.DualGrid;
    using Godot;
    using Helpers.Extensions;

    /// <summary>
    /// Paints a display <see cref="TileMapLayer"/> from terrain drawn on an invisible world layer. The display
    /// layer is shifted half a cell up and left of the world layer, so each of its tiles covers the corners of
    /// four world cells and is picked by their 4-bit mask: borders run between world cells instead of through
    /// them, and the gameplay grid stays the honest world one. Both layers must sit under the same parent
    /// transform.
    /// </summary>
    /// <remarks>
    /// The component also runs in the editor, where two things it does at runtime are off limits: the world
    /// layer keeps whatever visibility its author gave it, and a display layer that belongs to the saved scene
    /// is left untouched — cells written into it would be serialised on the next save.
    /// </remarks>
    [Tool]
    [GlobalClass]
    public partial class DualGridTerrainLayer : Node2D
    {
        [Export] private TileMapLayer? _world;
        [Export] private TileMapLayer? _display;

        /// <summary>Tile source painted into the world layer by <see cref="SetWorldCell"/>.</summary>
        [Export] private int _worldSourceId;

        [Export] private Vector2I _worldAtlasCoords;

        /// <summary>Tile source of the 4x4 transition atlas the display layer draws from.</summary>
        [Export] private int _displaySourceId;

        private readonly DualGridProjection _projection = new();

        private bool _repaintQueued;

        /// <summary>Whether the world layer's change signal is actually hooked up — a pair that never started
        /// painting must not try to unhook on the way out.</summary>
        private bool _watchingWorld;

        /// <summary>
        /// Wires the pair from code, for a container that builds the display layer at runtime instead of
        /// carrying it in a scene. Call it before the node enters the tree: <see cref="_Ready"/> is where the
        /// pair is aligned and first painted. Scene-authored layers keep using the exports untouched.
        /// </summary>
        public void Bind(TileMapLayer world, TileMapLayer display, int displaySourceId = 0)
        {
            _world = world;
            _display = display;
            _displaySourceId = displaySourceId;
        }

        public override void _Ready()
        {
            if (!LayersAssigned() || !MayPaint()) return;

            // Only outside the editor: a hidden world layer would be written into the scene as hidden, and the
            // author paints on that layer.
            if (!Engine.IsEditorHint()) _world!.Visible = false;

            AlignDisplay();
            _display!.Clear();
            Repaint();
            _world!.Changed += QueueRepaint;
            _watchingWorld = true;
        }

        public override void _ExitTree()
        {
            if (!_watchingWorld || !IsInstanceValid(_world)) return;

            _watchingWorld = false;
            // The flag says this pair once started watching; whether the connection still exists is a separate
            // question. An editor assembly reload kills the subscribed delegate with its unloaded ALC, and a
            // fresh Callable no longer matches it — unsubscribing then only raises "nonexistent connection".
            if (!_world!.IsConnected(TileMapLayer.SignalName.Changed, Callable.From(QueueRepaint))) return;

            _world.Changed -= QueueRepaint;
        }

        private bool LayersAssigned()
        {
            if (_world is null) Tracker.TrackError("DualGridTerrainLayer has no world layer assigned: there is no terrain data to read", this);
            if (_display is null) Tracker.TrackError("DualGridTerrainLayer has no display layer assigned: there is nothing to paint", this);

            return _world is not null && _display is not null;
        }

        /// <summary>
        /// The editor may paint only into a display layer built from code. A layer carried by the scene is
        /// saved with everything written into it, so a preview would leave its tiles behind in the .tscn.
        /// </summary>
        private bool MayPaint() => !Engine.IsEditorHint() || _display!.Owner is null;

        /// <summary>Half-cell shift, measured from the world layer so a moved world layer takes the display with it.</summary>
        private void AlignDisplay()
        {
            if (_display!.TileSet is not TileSet tileSet)
            {
                Tracker.TrackError("DualGridTerrainLayer display layer has no TileSet: the half-cell shift cannot be measured", this);
                return;
            }

            _display.Position = _world!.Position + new Vector2(
                DualGridLayout.DisplayPixelOffset(tileSet.TileSize.X),
                DualGridLayout.DisplayPixelOffset(tileSet.TileSize.Y));
        }

        /// <summary>
        /// The world layer's change signal names no coordinates and fires once per brush step, so the repaint
        /// is coalesced to the end of the frame and then reconciles against the previous snapshot: the display
        /// writes stay proportional to the edit, not to the map.
        /// </summary>
        private void QueueRepaint()
        {
            if (_repaintQueued) return;

            _repaintQueued = true;
            Callable.From(Repaint).CallDeferred();
        }

        private void Repaint()
        {
            _repaintQueued = false;
            // The deferred call outlives the node: an edit and a QueueFree in the same frame land here on
            // freed layers.
            if (!IsInstanceValid(_world) || !IsInstanceValid(_display)) return;

            foreach (GridCoordinate cell in _projection.Reconcile(ReadWorldCells())) Paint(cell);
        }

        private IEnumerable<GridCoordinate> ReadWorldCells()
        {
            foreach (Vector2I cell in _world!.GetUsedCells()) yield return cell.ToGridCoordinate();
        }

        private void Paint(GridCoordinate display)
        {
            TerrainCorner mask = _projection.MaskAt(display);
            Vector2I coords = display.ToVector2I();

            if (mask == TerrainCorner.None)
            {
                _display!.EraseCell(coords);
                return;
            }

            _display!.SetCell(coords, _displaySourceId, DualGridAtlas.CoordinateOf(mask).ToVector2I());
        }

        /// <summary>Writes one world cell and repaints only the four display tiles that cover it.</summary>
        public void SetWorldCell(Vector2I cell, bool present)
        {
            if (!IsInstanceValid(_world) || !IsInstanceValid(_display))
            {
                Tracker.TrackError($"DualGridTerrainLayer cannot write world cell {cell}: its layers are missing or already freed", this);
                return;
            }

            if (present) _world!.SetCell(cell, _worldSourceId, _worldAtlasCoords);
            else _world!.EraseCell(cell);

            foreach (GridCoordinate dirty in _projection.SetCell(cell.ToGridCoordinate(), present)) Paint(dirty);
        }
    }
}
