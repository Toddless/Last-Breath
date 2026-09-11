namespace Core.Ai.World.Time
{
    using System;

    /// <summary>
    /// The game world's clock. Ticked by the world heartbeat (NpcWorldDirector) with real delta;
    /// keeps running during battles (the world lives on) and stops with the game pause.
    /// Consumers: NPC schedules, spawn points, day/night visuals, future quests and traders.
    /// </summary>
    public interface IWorldClock
    {
        double TotalMinutes => Day * 1440.0 + MinuteOfDay;
        void RestoreTime(double totalMinutes) => RestoreState((int)(totalMinutes / 1440), (int)(totalMinutes % 1440));

        int Day { get; }
        int Hour { get; }
        int Minute { get; }

        /// <summary>Minutes since midnight (0..1439) — the schedule slots compare against this.</summary>
        int MinuteOfDay { get; }

        /// <summary>Time of day as 0..1 (midnight to midnight) — handy for visuals.</summary>
        float NormalizedTimeOfDay { get; }

        /// <summary>How many real seconds one game minute lasts — UI countdowns render game-time
        /// deadlines in wall-clock terms through this. Defaults to 1:1 for hosts (test fakes)
        /// that never configure a day length.</summary>
        float RealSecondsPerGameMinute => 1f;

        DayPhase Phase { get; }
        DayPhase PhaseAt(double totalMinutes) => Phase;

        event Action<int>? HourPassed;
        event Action<DayPhase>? PhaseChanged;

        void Tick(float realDelta);

        /// <summary>Save-load path: jumps to the stored moment; fires HourPassed/PhaseChanged when they differ.</summary>
        void RestoreState(int day, int minuteOfDay);
    }
}
