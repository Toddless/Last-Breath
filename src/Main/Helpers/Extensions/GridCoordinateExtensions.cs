namespace LastBreath.Helpers.Extensions
{
    using Core.World.DualGrid;
    using Godot;

    /// <summary>Bridges Godot cell addresses and the Godot-free grid addresses world logic works with.</summary>
    public static class GridCoordinateExtensions
    {
        public static GridCoordinate ToGridCoordinate(this Vector2I cell) => new(cell.X, cell.Y);

        public static Vector2I ToVector2I(this GridCoordinate cell) => new(cell.X, cell.Y);
    }
}
