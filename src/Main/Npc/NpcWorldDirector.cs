namespace LastBreath.Npc
{
    using Core.Ai.World.Raids;
    using Core.Ai.World.Time;
    using Core.Events;
    using Core.Events.GameEvents;
    using Godot;
    using Services;

    /// <summary>
    /// World-level AI heartbeat: drop ONE into the world scene. Ticks the world clock and the
    /// NPC skirmishes, republishes clock milestones to the game bus; future home for offline
    /// A-Life ticks and skirmish presentation. Pausing the game pauses the world time with it.
    /// </summary>
    [GlobalClass]
    public partial class NpcWorldDirector : Node
    {
        private INpcSkirmishService? _skirmishes;
        private IRaidService? _raids;
        private IWorldClock? _clock;
        private IGameEventBus? _gameEventBus;

        public override void _Ready()
        {
            _skirmishes = GameServiceProvider.Instance.GetService<INpcSkirmishService>();
            _raids = GameServiceProvider.Instance.GetService<IRaidService>();
            _clock = GameServiceProvider.Instance.GetService<IWorldClock>();
            _gameEventBus = GameServiceProvider.Instance.GetService<IGameEventBus>();

            if (_clock == null) return;
            _clock.HourPassed += OnHourPassed;
            _clock.PhaseChanged += OnPhaseChanged;
        }

        public override void _ExitTree()
        {
            if (_clock == null) return;
            _clock.HourPassed -= OnHourPassed;
            _clock.PhaseChanged -= OnPhaseChanged;
        }

        public override void _Process(double delta)
        {
            _clock?.Tick((float)delta);
            _skirmishes?.Tick((float)delta);
            _raids?.Tick((float)delta);
        }

        private void OnHourPassed(int hour) =>
            _gameEventBus?.Publish(new WorldHourChangedEvent(_clock!.Day, hour));

        private void OnPhaseChanged(DayPhase phase) =>
            _gameEventBus?.Publish(new WorldPhaseChangedEvent(phase, _clock!.Day));
    }
}
