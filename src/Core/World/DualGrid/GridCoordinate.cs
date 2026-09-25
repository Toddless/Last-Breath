namespace Core.World.DualGrid
{
    /// <summary>Integer cell address on either grid — the Godot-free twin of Vector2I.</summary>
    public readonly record struct GridCoordinate(int X, int Y)
    {
        public static GridCoordinate operator +(GridCoordinate first, GridCoordinate second) =>
            new(first.X + second.X, first.Y + second.Y);

        public static GridCoordinate operator -(GridCoordinate first, GridCoordinate second) =>
            new(first.X - second.X, first.Y - second.Y);

        public override string ToString() => $"({X}, {Y})";
    }
}
