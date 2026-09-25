namespace LastBreath.World
{
    using Core.World.DualGrid;
    using Godot;

    /// <summary>
    /// One terrain of a <see cref="TerrainRoot"/>: the name of the invisible data layer it reads, the 4x4
    /// transition sheet its ground is painted from, the optional material of that generated surface, and — when
    /// a prop texture is given — the scatter that grows over the same cells. Everything a new terrain needs is
    /// here, so adding one is a data edit plus an empty layer, never a new scene.
    /// </summary>
    [GlobalClass]
    public partial class TerrainEntry : Resource
    {
        /// <summary>Name of the child layer holding the painted cells. The match is exact, case included.</summary>
        [Export] public string Layer { get; set; } = "";

        /// <summary>4x4 transition sheet using the selected atlas layout.</summary>
        [Export] public Texture2D? Transitions { get; set; }

        /// <summary>RowMajor: masks 0–15; Grouped: corners, edges and remaining shapes in the authored sheet.</summary>
        [Export] public TransitionAtlasLayout AtlasLayout { get; set; }

        /// <summary>Material applied to the generated display layer, never to the painted data layer.</summary>
        [Export] public Material? SurfaceMaterial { get; set; }

        /// <summary>Prop grown on every painted cell. Leave empty for a terrain that grows nothing.</summary>
        [ExportGroup("Scatter")]
        [Export] public Texture2D? Prop { get; set; }

        /// <summary>Height of an unscaled prop in world pixels; the texture is fitted to it.</summary>
        [Export] public float PropHeight { get; set; } = 96f;

        /// <summary>Change to reroll the whole scatter without touching a single cell.</summary>
        [Export] public int Seed { get; set; }

        [Export] public int MinPropsPerCell { get; set; } = 2;

        [Export] public int MaxPropsPerCell { get; set; } = 4;

        /// <summary>Keep-out band along the cell border, in pixels, so roots never line up on the cell seams.</summary>
        [Export] public float EdgeMargin { get; set; } = 24f;

        [Export] public float MinScale { get; set; } = 0.9f;

        [Export] public float MaxScale { get; set; } = 1.1f;

        [Export] public float TintJitter { get; set; } = 0.08f;

        /// <summary>A prop texture is the switch: no texture, no scatter layer is built at all.</summary>
        public bool Scatters => Prop is not null;

        /// <summary>
        /// The scatter knobs as the Godot-free layout reads them. <see cref="PropScatterSettings.CellSize"/> is
        /// left at its default on purpose: the layer measures the cell off the data layer's own tileset.
        /// </summary>
        public PropScatterSettings ScatterSettings() => new()
        {
            Seed = Seed,
            MinPropsPerCell = MinPropsPerCell,
            MaxPropsPerCell = MaxPropsPerCell,
            EdgeMargin = EdgeMargin,
            MinScale = MinScale,
            MaxScale = MaxScale,
            TintJitter = TintJitter
        };
    }
}
