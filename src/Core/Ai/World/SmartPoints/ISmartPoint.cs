namespace Core.Ai.World.SmartPoints
{
    using Godot;

    /// <summary>Well-known smart point tags; scene nodes may introduce new ones freely.</summary>
    public static class SmartPointTags
    {
        public const string Campfire = "Campfire";
        public const string Tent = "Tent";
        public const string OreVein = "OreVein";
        public const string TradeStall = "TradeStall";
        public const string Forge = "Forge";
    }

    /// <summary>A claimable spot of interest in the world (campfire, tent, ore vein, trade stall, forge).</summary>
    public interface ISmartPoint
    {
        string Tag { get; }

        /// <summary>How many claimants may hold the point at once.</summary>
        int Capacity { get; }

        Vector2 Position { get; }
    }
}
