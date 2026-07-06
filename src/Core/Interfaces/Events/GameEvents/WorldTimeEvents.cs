namespace Core.Interfaces.Events.GameEvents
{
    using Ai.World.Time;

    /// <summary>The day phase flipped (published by the world heartbeat at presentation time).</summary>
    public record WorldPhaseChangedEvent(DayPhase Phase, int Day) : IGameEvent;

    /// <summary>A game hour passed — for spawn points, traders and other hourly consumers.</summary>
    public record WorldHourChangedEvent(int Day, int Hour) : IGameEvent;
}
