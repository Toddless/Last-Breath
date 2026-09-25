namespace Core.Ai.World.Time
{
    /// <summary>Phases of the game day; hour boundaries live in <see cref="WorldClockConfig"/>.</summary>
    public enum DayPhase : byte
    {
        Night,
        Morning,
        Day,
        Evening
    }
}
