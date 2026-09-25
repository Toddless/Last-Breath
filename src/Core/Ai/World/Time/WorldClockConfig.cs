namespace Core.Ai.World.Time
{
    /// <summary>WorldClock.json: pacing of the game day and the phase boundaries.</summary>
    public class WorldClockConfig
    {
        /// <summary>One full game day passes in this many real minutes (24 = one real minute per game hour).</summary>
        public float RealMinutesPerGameDay { get; init; } = 24f;

        public int StartDay { get; init; } = 1;
        public int StartHour { get; init; } = 8;

        // Phase boundaries (agreed defaults): Night 22–06, Morning 06–10, Day 10–18, Evening 18–22.
        public int MorningStartHour { get; init; } = 6;
        public int DayStartHour { get; init; } = 10;
        public int EveningStartHour { get; init; } = 18;
        public int NightStartHour { get; init; } = 22;
    }
}
