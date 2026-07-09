namespace LastBreath.World
{
    using Core.Ai.World.Time;
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

        public override void _Ready() => _clock = GameServiceProvider.Instance.GetService<IWorldClock>();

        public override void _Process(double delta)
        {
            if (_clock == null) return;
            Color = Color.Lerp(PhaseColor(_clock.Phase), (float)delta * _transitionSpeed);
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
