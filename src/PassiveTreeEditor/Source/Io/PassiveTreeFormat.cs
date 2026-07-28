namespace PassiveTreeEditor.Source.Io
{
    /// <summary>Format-wide constants shared by the writer, the reader and the game-side provider.</summary>
    public static class PassiveTreeFormat
    {
        /// <summary>Bumped only when a change stops old files from loading correctly.</summary>
        public const int Version = 1;

        /// <summary>
        /// The data catalog (subfolder of a project's data root) the tree lives in. The game side
        /// mirrors this as a constant in <c>Core.Data.GameData.DataCatalog</c> when the tree is wired
        /// into the real bootstrap; the editor keeps its own copy so it depends on nothing unreleased.
        /// </summary>
        public const string Catalog = "PassiveTree";

        public const string DefaultFileName = "PassiveTree.json";

        /// <summary>Positions are rounded to this many decimals on save: a drag produces a one-line
        /// diff instead of a wall of floating-point noise, and the rounding is idempotent.</summary>
        public const int PositionDecimals = 2;

        /// <summary>Modifier values are fractions, same as everywhere else in the game data.</summary>
        public const int ValueDecimals = 4;
    }
}
