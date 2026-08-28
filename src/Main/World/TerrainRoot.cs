namespace LastBreath.World
{
    using System.Collections.Generic;
    using Core;
    using Core.World.DualGrid;
    using Godot;

    /// <summary>
    /// Holds every dual-grid terrain of a location and builds their machinery from data. Its children are the
    /// invisible data layers the owner paints on, one per terrain, each named after an entry of the config; on
    /// <see cref="_Ready"/> each named layer gets a display layer (its tileset assembled from the entry's
    /// transition sheet), a <see cref="DualGridTerrainLayer"/> wiring the pair, and a
    /// <see cref="PropScatterLayer"/> where the entry carries a prop. A new terrain is therefore an empty layer
    /// and a row of data — no scene, no hand-wired exports, nothing to lose on the next edit.
    /// </summary>
    /// <remarks>
    /// Terrains are drawn in child order, so the layer list is the layer stack: earlier children lie under later
    /// ones. The container is expected to sit below the character sort pool (z_index -1), which is why scattered
    /// props are parented outside it — see <see cref="_propContainer"/>.
    /// </remarks>
    [GlobalClass]
    public partial class TerrainRoot : Node2D
    {
        /// <summary>Tile source the assembled display tilesets put their transition atlas at.</summary>
        private const int DisplaySourceId = 0;

        [Export] private TerrainRootConfig? _config;

        /// <summary>
        /// Node scattered props are parented to. Empty falls back to this container's parent, which is the
        /// Y-sorted world root: props have to share the sort pool with the characters that wade through them,
        /// and the terrain container itself is buried under it.
        /// </summary>
        [Export] private Node2D? _propContainer;

        public override void _Ready()
        {
            if (_config is null)
            {
                Fail("TerrainRoot has no config assigned: none of its layers can be painted");
                return;
            }

            List<TileMapLayer> dataLayers = DataLayers();
            Report(TerrainRoster.Match(NamesOf(dataLayers), _config.Keys()));

            Dictionary<string, TerrainEntry> byLayer = _config.ByLayer();
            foreach (TileMapLayer data in dataLayers)
            {
                if (byLayer.TryGetValue(data.Name.ToString(), out TerrainEntry? entry)) Build(data, entry);
            }
        }

        /// <summary>Children painted on by the owner — read before anything is built, so only they are seen.</summary>
        private List<TileMapLayer> DataLayers()
        {
            List<TileMapLayer> layers = [];
            foreach (Node child in GetChildren())
            {
                if (child is TileMapLayer layer) layers.Add(layer);
            }

            return layers;
        }

        private static List<string> NamesOf(List<TileMapLayer> layers)
        {
            List<string> names = new(layers.Count);
            foreach (TileMapLayer layer in layers) names.Add(layer.Name.ToString());

            return names;
        }

        /// <summary>Every break of the naming convention is shouted about: a silent one is a missing terrain.</summary>
        private void Report(TerrainRosterReport report)
        {
            if (report.IsClean) return;

            foreach (string layer in report.LayersWithoutEntry)
                Fail($"TerrainRoot layer '{layer}' has no entry in its config: the cells painted on it stay invisible");

            foreach (string key in report.EntriesWithoutLayer)
                Fail($"TerrainRoot config names terrain '{key}', but no child layer carries that name: there is nothing to read");

            foreach (string layer in report.DuplicateLayers)
                Fail($"TerrainRoot has more than one child layer named '{layer}': the entry cannot say which one it means");

            foreach (string key in report.DuplicateEntries)
                Fail($"TerrainRoot config claims terrain '{key}' twice: the first entry is used and the rest are ignored");

            if (report.UnnamedEntries > 0)
                Fail($"TerrainRoot config holds {report.UnnamedEntries} entry(ies) with no layer name: they can never match a layer");
        }

        private void Build(TileMapLayer data, TerrainEntry entry)
        {
            if (entry.Transitions is null)
            {
                Fail($"Terrain '{entry.Layer}' has no transition sheet: its ground cannot be painted");
                return;
            }

            if (BuildTransitionTileSet(entry.Transitions) is not TileSet tileSet) return;

            TileMapLayer display = new()
            {
                Name = $"{data.Name}Display",
                TileSet = tileSet
            };

            // The display layer goes in as a child of the container, next to the data layers and after every
            // display built before it: the draw order of the terrains is the order of the entries.
            AddChild(display);

            DualGridTerrainLayer painter = new() { Name = $"{data.Name}Painter" };
            // Bound before it enters the tree: its _Ready aligns the pair and paints what is already drawn.
            painter.Bind(data, display, DisplaySourceId);
            AddChild(painter);

            if (!entry.Scatters) return;

            PropScatterLayer scatter = new() { Name = $"{data.Name}Scatter" };
            scatter.Bind(data, PropContainer(), entry.Prop!, entry.PropHeight, entry.ScatterSettings());
            AddChild(scatter);
        }

        /// <summary>
        /// The 4x4 transition sheet as a tileset: one source holding all sixteen mask tiles, the cell measured
        /// off the sheet so a future atlas of another resolution needs no code change.
        /// </summary>
        private TileSet? BuildTransitionTileSet(Texture2D atlas)
        {
            int width = atlas.GetWidth();
            int height = atlas.GetHeight();
            int tileWidth = width / DualGridAtlas.Columns;
            int tileHeight = height / DualGridAtlas.Rows;

            if (tileWidth <= 0 || tileHeight <= 0
                || tileWidth * DualGridAtlas.Columns != width || tileHeight * DualGridAtlas.Rows != height)
            {
                Fail($"Transition sheet '{atlas.ResourcePath}' is {width}x{height}: a {DualGridAtlas.Columns}x{DualGridAtlas.Rows} sheet of equal tiles is expected");
                return null;
            }

            TileSetAtlasSource source = new()
            {
                Texture = atlas,
                TextureRegionSize = new Vector2I(tileWidth, tileHeight)
            };

            for (int row = 0; row < DualGridAtlas.Rows; row++)
            {
                for (int column = 0; column < DualGridAtlas.Columns; column++) source.CreateTile(new Vector2I(column, row));
            }

            TileSet tileSet = new() { TileSize = new Vector2I(tileWidth, tileHeight) };
            tileSet.AddSource(source, DisplaySourceId);

            return tileSet;
        }

        private Node2D PropContainer() => _propContainer ?? GetParent() as Node2D ?? this;

        private void Fail(string message)
        {
            Tracker.TrackError(message, this);
            GD.PrintErr(message);
        }
    }
}
