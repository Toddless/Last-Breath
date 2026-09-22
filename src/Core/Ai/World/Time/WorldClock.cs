namespace Core.Ai.World.Time
{
    using System;

    /// <summary>
    /// Pure game-time keeper: real seconds scale into game seconds by the configured day length.
    /// A tick or jump spanning several game minutes or hours fires each event once with the final value —
    /// consumers read the current state, they don't replay the skipped minutes and hours.
    /// </summary>
    public class WorldClock : IWorldClock, Session.ISessionResettable
    {
        private const double GameDaySeconds = 24 * 3600;

        private WorldClockConfig _config;
        private double _secondsOfDay;

        /// <summary>The whole game minutes elapsed, the unit <see cref="MinutePassed"/> announces.</summary>
        private long WholeMinute => (long)Math.Floor(TotalMinutes);
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
        public event Action<double>? MinutePassed;

        public WorldClock(WorldClockConfig? config = null)
        {
            _config = config ?? new WorldClockConfig();
            ResetToStart();
        }

        public void Tick(float realDelta)
        {
            var previous = Read();

            double gameSecondsPerRealSecond = GameDaySeconds / (_config.RealMinutesPerGameDay * 60f);
            _secondsOfDay += realDelta * gameSecondsPerRealSecond;
            while (_secondsOfDay >= GameDaySeconds)
            {
                _secondsOfDay -= GameDaySeconds;
                Day++;
            }

            AnnounceChangesSince(previous);
        }

        public void RestoreState(int day, int minuteOfDay) => RestoreTime(Math.Max(0, day) * 1440.0 + Math.Clamp(minuteOfDay, 0, 1439));

        public void RestoreTime(double totalMinutes)
        {
            if (!double.IsFinite(totalMinutes) || totalMinutes < 0) throw new ArgumentOutOfRangeException(nameof(totalMinutes));
            var previous = Read();

            Day = (int)(totalMinutes / 1440);
            _secondsOfDay = totalMinutes % 1440 * 60.0;

            AnnounceChangesSince(previous);
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

        /// <summary>The announced values as they stand, taken before a change to compare against.</summary>
        private Reading Read() => new(WholeMinute, Hour, Phase);

        /// <summary>Fires each event whose value differs from <paramref name="previous"/>, once and with the current value.</summary>
        private void AnnounceChangesSince(Reading previous)
        {
            if (Hour != previous.Hour) HourPassed?.Invoke(Hour);
            if (Phase != previous.Phase) PhaseChanged?.Invoke(Phase);
            if (WholeMinute != previous.WholeMinute) MinutePassed?.Invoke(TotalMinutes);
        }

        private readonly record struct Reading(long WholeMinute, int Hour, DayPhase Phase);
    }
}
