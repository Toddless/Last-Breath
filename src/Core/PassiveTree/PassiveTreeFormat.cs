namespace Core.PassiveTree
{
    /// <summary>Format-wide constants shared by the writer, the reader and the data participant.</summary>
    public static class PassiveTreeFormat
    {
        /// <summary>Bumped only when a change stops old files from loading correctly.</summary>
        public const int Version = 1;

        public const string DefaultFileName = "PassiveTree.json";

        /// <summary>Positions are rounded to this many decimals on save: a drag produces a one-line
        /// diff instead of a wall of floating-point noise, and the rounding is idempotent.</summary>
        public const int PositionDecimals = 2;

        /// <summary>Modifier values are fractions, same as everywhere else in the game data.</summary>
        public const int ValueDecimals = 4;
    }
}
