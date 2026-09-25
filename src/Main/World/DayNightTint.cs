namespace LastBreath.World
{
    using Core.Ai.World.Time;
    using Core.Events;
    using Godot;
    using Services;

    /// <summary>
    /// Placeholder day/night look: a CanvasModulate smoothly tinting toward the phase color.
    /// Drop one into the world scene next to NpcWorldDirector.
    /// </summary>
    [GlobalClass]
    public partial class DayNightTint : CanvasModulate
    {
        /// <summary>How fast the tint chases the phase color (fraction per second).</summary>
        [Export] private float _transitionSpeed = 0.5f;

        private IWorldClock? _clock;
        private IGameEventBus? _gameEventBus;

        public override void _Ready()
        {
            _clock = GameServiceProvider.Instance.GetService<IWorldClock>();
            _gameEventBus = GameServiceProvider.Instance.GetService<IGameEventBus>();
            _gameEventBus?.Subscribe<GameLoadedEvent>(OnGameLoaded);
            SnapToPhase();
        }

        public override void _ExitTree() => _gameEventBus?.Unsubscribe<GameLoadedEvent>(OnGameLoaded);

        public override void _Process(double delta)
        {
            if (_clock == null) return;
            Color = Color.Lerp(PhaseColor(_clock.Phase), (float)delta * _transitionSpeed);
        }

        /// <summary>The snap in _Ready read the clock of the playthrough being left behind — the file's
        /// own time arrives after it, and easing into it would hold that sky on screen for seconds.
        /// The phase is polled, not subscribed to, so this is where the correction has to come from.</summary>
        private void OnGameLoaded(GameLoadedEvent evnt) => SnapToPhase();

        // Snap, don't lerp, into the CURRENT phase: a scene reload recreates this node with the
        // default white Color — easing from it flashed a night world into daylight.
        private void SnapToPhase()
        {
            if (_clock != null) Color = PhaseColor(_clock.Phase);
        }

        private static Color PhaseColor(DayPhase phase) => phase switch
        {
            DayPhase.Night => new Color(0.45f, 0.5f, 0.72f),
            DayPhase.Morning => new Color(1f, 0.95f, 0.85f),
            DayPhase.Evening => new Color(1f, 0.85f, 0.7f),
            _ => Colors.White
        };
    }
}
