namespace Core.Ai.World.Activities
{
    using Time;

    /// <summary>
    /// Turns any activity into a routine step: run it for a budget of GAME minutes, then report
    /// completion. Elapsed time is progress — an interruption (battle, alert) pauses the budget
    /// instead of restarting it; without a clock one real second approximates one game minute
    /// (the same convention the spawn points use).
    /// </summary>
    public class TimedTask(IWorldActivity inner, float minutes, IWorldClock? clock) : IWorldTask
    {
        private double _elapsedMinutes;
        private double _lastAbsoluteMinute = -1;

        public bool IsCompleted => _elapsedMinutes >= minutes;

        public void Restart() => _elapsedMinutes = 0;

        public void Enter(WorldBrain brain)
        {
            // The interruption gap must not count as work done.
            _lastAbsoluteMinute = AbsoluteMinute();
            inner.Enter(brain);
        }

        public void Tick(WorldBrain brain, float delta)
        {
            Advance(delta);
            inner.Tick(brain, delta);
        }

        public void Exit(WorldBrain brain)
        {
            _lastAbsoluteMinute = -1;
            inner.Exit(brain);
        }

        private void Advance(float delta)
        {
            if (clock == null)
            {
                _elapsedMinutes += delta;
                return;
            }

            double now = AbsoluteMinute();
            if (_lastAbsoluteMinute >= 0 && now > _lastAbsoluteMinute)
                _elapsedMinutes += now - _lastAbsoluteMinute;
            _lastAbsoluteMinute = now;
        }

        private double AbsoluteMinute() => clock == null ? 0 : clock.Day * 1440 + clock.MinuteOfDay;
    }
}
