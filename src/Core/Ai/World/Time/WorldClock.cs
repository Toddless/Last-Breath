namespace Core.Ai.World.Time
{
    using System;

    /// <summary>
    /// Pure game-time keeper: real seconds scale into game seconds by the configured day length.
    /// A tick spanning several game hours fires each event once with the final value —
    /// consumers read the current state, they don't replay the skipped hours.
    /// </summary>
    public class WorldClock : IWorldClock, Session.ISessionResettable
    {
        private const double GameDaySeconds = 24 * 3600;

        private WorldClockConfig _config;
        private double _secondsOfDay;

        public double TotalMinutes => Day * 1440.0 + _secondsOfDay / 60.0;
        public int Day { get; private set; }
        public int Hour => (int)(_secondsOfDay / 3600);
        public int Minute => (int)(_secondsOfDay % 3600 / 60);
        public int MinuteOfDay => (int)(_secondsOfDay / 60);
        public float NormalizedTimeOfDay => (float)(_secondsOfDay / GameDaySeconds);
        public float RealSecondsPerGameMinute => _config.RealMinutesPerGameDay * 60f / (24f * 60f);
        public DayPhase Phase => PhaseOf(Hour);
        public DayPhase PhaseAt(double totalMinutes) => PhaseOf((int)(totalMinutes % 1440 / 60));

        public event Action<int>? HourPassed;
        public event Action<DayPhase>? PhaseChanged;

        public WorldClock(WorldClockConfig? config = null)
        {
            _config = config ?? new WorldClockConfig();
            ResetToStart();
        }

        public void Tick(float realDelta)
        {
            int previousHour = Hour;
            var previousPhase = Phase;

            double gameSecondsPerRealSecond = GameDaySeconds / (_config.RealMinutesPerGameDay * 60f);
            _secondsOfDay += realDelta * gameSecondsPerRealSecond;
            while (_secondsOfDay >= GameDaySeconds)
            {
                _secondsOfDay -= GameDaySeconds;
                Day++;
            }

            if (Hour != previousHour) HourPassed?.Invoke(Hour);
            if (Phase != previousPhase) PhaseChanged?.Invoke(Phase);
        }

        public void RestoreState(int day, int minuteOfDay) => RestoreTime(Math.Max(0, day) * 1440.0 + Math.Clamp(minuteOfDay, 0, 1439));

        public void RestoreTime(double totalMinutes)
        {
            if (!double.IsFinite(totalMinutes) || totalMinutes < 0) throw new ArgumentOutOfRangeException(nameof(totalMinutes));
            int previousHour = Hour;
            var previousPhase = Phase;

            Day = (int)(totalMinutes / 1440);
            _secondsOfDay = totalMinutes % 1440 * 60.0;

            if (Hour != previousHour) HourPassed?.Invoke(Hour);
            if (Phase != previousPhase) PhaseChanged?.Invoke(Phase);
        }

        /// <summary>Silent rewind to the configured start; the fresh world reads the state in its own _Ready.</summary>
        public void ResetSession() => ResetToStart();

        /// <summary>Applies a freshly loaded config and rewinds to its start time (startup only).</summary>
        public void Configure(WorldClockConfig config)
        {
            _config = config;
            ResetToStart();
        }

        private void ResetToStart()
        {
            Day = _config.StartDay;
            _secondsOfDay = _config.StartHour * 3600.0;
        }

        private DayPhase PhaseOf(int hour)
        {
            if (hour >= _config.NightStartHour || hour < _config.MorningStartHour) return DayPhase.Night;
            if (hour < _config.DayStartHour) return DayPhase.Morning;
            return hour < _config.EveningStartHour ? DayPhase.Day : DayPhase.Evening;
        }
    }
}
