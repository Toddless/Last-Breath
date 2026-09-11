namespace Core.Ai.World.Skirmish
{
    using System.Collections.Generic;
    public sealed class NpcSkirmishState
    {
        public List<string> SideA { get; init; } = [];
        public List<string> SideB { get; init; } = [];
        public int RoundsPlayed { get; init; }
        public int SideAWins { get; init; }
        public float NextRollIn { get; init; }
    }
}
