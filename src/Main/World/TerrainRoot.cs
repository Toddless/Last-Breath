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
    /// transition sheet and surface material), a <see cref="DualGridTerrainLayer"/> wiring the pair, and a
    /// <see cref="PropScatterLayer"/> where the entry carries a prop. A new terrain is therefore an empty layer
    /// and a row of data — no scene, no hand-wired exports, nothing to lose on the next edit.
    /// </summary>
    /// <remarks>
    /// Terrains are drawn in child order, so the layer list is the layer stack: earlier children lie under later
    /// ones. The container is expected to sit below the character sort pool (z_index -1), which is why scattered
    /// props are parented outside it — see <see cref="_propContainer"/>.
    /// <para>
    /// The same machinery runs in the editor, so a brush stroke on a data layer shows the assembled terrain at
    /// once. Nothing it builds is owned by the scene and nothing it builds is saved; the data layers themselves
    /// are read and never written, their visibility included.
    /// </para>
    /// <para>
    /// Editor preview is disabled (2026-09-01): a live [Tool] instance keeps the editor from unloading the C#
    /// assembly on rebuild (godotengine/godot#78513). Terrain textures are previewed by the GDScript editor
    /// plugin; these C# components remain runtime-only.
    /// </para>
    /// </remarks>
    [GlobalClass]
    public partial class TerrainRoot : Node2D
    {
        /// <summary>Tile source the assembled display tilesets put their transition atlas at.</summary>
        private const int DisplaySourceId = 0;

        /// <summary>
        /// Declared as the base <see cref="Resource"/> on purpose: after an editor assembly reload, a
        /// [Tool]-script's exported custom resource can arrive as a live instance wrapped by a stale, unloaded
        /// ALC, and Godot-mono's generated property setter throws <see cref="System.InvalidCastException"/>
        /// casting it to <see cref="TerrainRootConfig"/> even though the object is a real config. The base type
        /// always marshals; <see cref="ReadEntries"/> below is where the actual reading happens, with a duck-typed
        /// fallback for exactly this situation.
        /// </summary>
        [Export] private Resource? _config;

        /// <summary>
        /// Node scattered props are parented to. Empty falls back to this container's parent, which is the
        /// Y-sorted world root: props have to share the sort pool with the characters that wade through them,
        /// and the terrain container itself is buried under it.
        /// </summary>
        [Export] private Node2D? _propContainer;

        /// <summary>
        /// Whether the editor assembles the terrains over the data layers. Off leaves the editor with the bare
        /// painted layers and nothing else; the running game is not affected either way.
        /// </summary>
        [Export]
        public bool PreviewInEditor
        {
            get;
            set
            {
                field = value;
                if (Engine.IsEditorHint() && IsNodeReady()) QueueRebuildPreview();
            }
        } = true;

        public override void _EnterTree()
        {
            // A tool node can leave the tree and come back on a scene or assembly reload without _Ready firing
            // again; asking for it makes every entry a full sweep-and-build instead of half a preview.
            if (Engine.IsEditorHint()) RequestReady();
        }

        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                QueueRebuildPreview();
                return;
            }

            BuildTerrains();
        }

        private bool _rebuildQueued;

        /// <summary>
        /// Editor rebuilds are coalesced to the end of the frame instead of running where they were asked for.
        /// The <see cref="PreviewInEditor"/> setter fires in the middle of things: an assembly reload restores
        /// the exports one at a time, and a synchronous rebuild from the setter would run before
        /// <see cref="_config"/> is back — the purge would sweep the old preview and the build would then fail
        /// on a config that is only null for the rest of the restoration. One deferred pass sees every export
        /// restored, and the flag keeps several triggers in one frame down to a single sweep-and-build.
        /// </summary>
        private void QueueRebuildPreview()
        {
            if (_rebuildQueued) return;

            _rebuildQueued = true;
            Callable.From(RebuildPreviewDeferred).CallDeferred();
        }

        private void RebuildPreviewDeferred()
        {
            _rebuildQueued = false;
            // The deferred call can outlive its moment: the node may have been freed or pulled off the tree
            // since it was queued, and a rebuild would then purge and parent against nothing.
            if (!IsInstanceValid(this) || !IsInsideTree()) return;

            RebuildPreview();
        }

        /// <summary>Editor pass: whatever the last build left is swept away first, so a reload never doubles it.</summary>
        private void RebuildPreview()
        {
            GeneratedTerrainNodes.Purge(this);

            Node2D props = PropContainer();
            if (props != this) GeneratedTerrainNodes.Purge(props);

            if (PreviewInEditor) BuildTerrains();
        }

        private void BuildTerrains()
        {
            if (_config is null)
            {
                Fail("TerrainRoot has no config assigned: none of its layers can be painted");
                return;
            }

            List<TerrainEntryData>? entries = ReadEntries(_config);
            if (entries is null)
            {
                Fail("TerrainRoot config is assigned but could not be read, neither as TerrainRootConfig nor through its Entries property: none of its layers can be painted");
                return;
            }

            List<TileMapLayer> dataLayers = DataLayers();
            List<string> keys = new(entries.Count);
            foreach (TerrainEntryData entry in entries) keys.Add(entry.Layer);
            Report(TerrainRoster.Match(NamesOf(dataLayers), keys));

            Dictionary<string, TerrainEntryData> byLayer = [];
            foreach (TerrainEntryData entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Layer)) byLayer.TryAdd(entry.Layer, entry);
            }

            foreach (TileMapLayer data in dataLayers)
            {
                if (byLayer.TryGetValue(data.Name.ToString(), out TerrainEntryData entry)) Build(data, entry);
            }
        }

        /// <summary>
        /// One terrain's data, read off <see cref="_config"/> through whichever path reaches it: a plain
        /// <see cref="TerrainEntry"/> in the healthy case, or Variant-level duck typing when the editor handed
        /// back a stale-ALC wrapper that fails the C# cast. The rest of <see cref="TerrainRoot"/> only ever sees
        /// this DTO.
        /// </summary>
        private readonly record struct TerrainEntryData(
            string Layer,
            Texture2D? Transitions,
            Material? SurfaceMaterial,
            Texture2D? Prop,
            float PropHeight,
            PropScatterSettings ScatterSettings,
            TransitionAtlasLayout AtlasLayout = TransitionAtlasLayout.RowMajor)
        {
            public bool Scatters => Prop is not null;
        }

        /// <summary>Blank slot: an empty array element, same as a null <see cref="TerrainEntry"/> reference.</summary>
        private static readonly TerrainEntryData BlankEntry = new("", null, null, null, 96f, new PropScatterSettings());

        /// <summary>
        /// Reads the table behind <paramref name="config"/>. Null means the resource is assigned but unreadable
        /// by either path — a genuine mismatch, not the missing-config case, which the caller already handles.
        /// </summary>
        private static List<TerrainEntryData>? ReadEntries(Resource config)
        {
            // Healthy editor and every runtime load: the cast just works.
            if (config is TerrainRootConfig typed) return FromTyped(typed);

            // Godot-mono tool bug: after an assembly reload, a live TerrainRootConfig instance can arrive here
            // wrapped by a stale, unloaded ALC, so `as`/`is` against the current type fails. Variant access goes
            // through Godot's own property table rather than the C# type system, so it still reaches the values.
            Variant entriesVariant = config.Get("Entries");
            if (entriesVariant.VariantType != Variant.Type.Array) return null;

            Godot.Collections.Array raw = entriesVariant.AsGodotArray();
            List<TerrainEntryData> entries = new(raw.Count);
            foreach (Variant item in raw) entries.Add(ReadEntryDuck(item));

            return entries;
        }

        private static List<TerrainEntryData> FromTyped(TerrainRootConfig config)
        {
            List<TerrainEntryData> entries = new(config.Entries.Count);
            foreach (TerrainEntry? entry in config.Entries) entries.Add(FromTypedEntry(entry));

            return entries;
        }

        private static TerrainEntryData FromTypedEntry(TerrainEntry? entry)
        {
            if (entry is null) return BlankEntry;

            return new TerrainEntryData(entry.Layer, entry.Transitions, entry.SurfaceMaterial, entry.Prop, entry.PropHeight, entry.ScatterSettings(), entry.AtlasLayout);
        }

        /// <summary>
        /// Duck-reads one array element by property name, matching <see cref="TerrainEntry"/>'s own [Export]
        /// names and defaults exactly. A property that Get cannot find comes back as a nil Variant, so every
        /// read is type-checked and falls back to the same default the healthy path would have used.
        /// </summary>
        private static TerrainEntryData ReadEntryDuck(Variant item)
        {
            GodotObject? obj = item.AsGodotObject();
            if (obj is TerrainEntry typed) return FromTypedEntry(typed);
            if (obj is null) return BlankEntry;

            string layer = VariantString(obj, "Layer", "");
            Texture2D? transitions = VariantTexture(obj, "Transitions");
            Material? surfaceMaterial = VariantMaterial(obj, "SurfaceMaterial");
            Texture2D? prop = VariantTexture(obj, "Prop");
            float propHeight = VariantFloat(obj, "PropHeight", 96f);

            PropScatterSettings settings = new()
            {
                Seed = VariantInt(obj, "Seed", 0),
                MinPropsPerCell = VariantInt(obj, "MinPropsPerCell", 2),
                MaxPropsPerCell = VariantInt(obj, "MaxPropsPerCell", 4),
                EdgeMargin = VariantFloat(obj, "EdgeMargin", 24f),
                MinScale = VariantFloat(obj, "MinScale", 0.9f),
                MaxScale = VariantFloat(obj, "MaxScale", 1.1f),
                TintJitter = VariantFloat(obj, "TintJitter", 0.08f)
            };

            return new TerrainEntryData(layer, transitions, surfaceMaterial, prop, propHeight, settings,
                (TransitionAtlasLayout)VariantInt(obj, nameof(TerrainEntry.AtlasLayout), (int)TransitionAtlasLayout.RowMajor));
        }

        private static string VariantString(GodotObject obj, string property, string fallback)
        {
            Variant value = obj.Get(property);
            return value.VariantType == Variant.Type.String ? value.AsString() : fallback;
        }

        private static float VariantFloat(GodotObject obj, string property, float fallback)
        {
            Variant value = obj.Get(property);
            return value.VariantType is Variant.Type.Float or Variant.Type.Int ? (float)value.AsDouble() : fallback;
        }

        private static int VariantInt(GodotObject obj, string property, int fallback)
        {
            Variant value = obj.Get(property);
            return value.VariantType is Variant.Type.Int or Variant.Type.Float ? value.AsInt32() : fallback;
        }

        private static Texture2D? VariantTexture(GodotObject obj, string property)
        {
            Variant value = obj.Get(property);
            return value.VariantType == Variant.Type.Object ? value.As<Texture2D>() : null;
        }

        private static Material? VariantMaterial(GodotObject obj, string property)
        {
            Variant value = obj.Get(property);
            return value.VariantType == Variant.Type.Object ? value.As<Material>() : null;
        }

        /// <summary>Children painted on by the owner: scene layers only, never a display built by a past pass.</summary>
        private List<TileMapLayer> DataLayers()
        {
            List<TileMapLayer> layers = [];
            foreach (Node child in GetChildren())
            {
                if (child is TileMapLayer layer && !GeneratedTerrainNodes.IsGenerated(layer)) layers.Add(layer);
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

        private void Build(TileMapLayer data, TerrainEntryData entry)
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
                TileSet = tileSet,
                Material = entry.SurfaceMaterial
            };

            // Marked and left without an Owner: the editor neither saves it into the scene nor mistakes it for
            // a data layer on the next build.
            GeneratedTerrainNodes.Mark(display);
            // The display layer goes in as a child of the container, next to the data layers and after every
            // display built before it: the draw order of the terrains is the order of the entries.
            AddChild(display);

            DualGridTerrainLayer painter = new() { Name = $"{data.Name}Painter", AtlasLayout = entry.AtlasLayout };
            // Bound before it enters the tree: its _Ready aligns the pair and paints what is already drawn.
            painter.Bind(data, display, DisplaySourceId);
            GeneratedTerrainNodes.Mark(painter);
            AddChild(painter);

            if (!entry.Scatters) return;

            PropScatterLayer scatter = new() { Name = $"{data.Name}Scatter" };
            scatter.Bind(data, PropContainer(), entry.Prop!, entry.PropHeight, entry.ScatterSettings);
            GeneratedTerrainNodes.Mark(scatter);
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

        private void Fail(string message) => Tracker.TrackError(message, this);
    }
}
