namespace LastBreath.World
{
    using Core.Ai.World.Recovery;
    using Godot;
    using Services;

    /// <summary>
    /// A campfire: registers a recovery zone around itself — any live, non-fighting participant
    /// (the player included) standing inside regains health, mana and barrier at the configured
    /// rates. The visual is authored in the scene; the node itself is a pure anchor.
    /// </summary>
    [GlobalClass]
    public partial class CampfireNode : Node2D
    {
        /// <summary>0 = the Recovery config's default radius.</summary>
        [Export] private float _zoneRadius;

        private IRestRecoveryService? _recovery;

        public override void _Ready()
        {
            _recovery = GameServiceProvider.Instance.GetService<IRestRecoveryService>();
            float radius = _zoneRadius > 0
                ? _zoneRadius
                : GameServiceProvider.Instance.GetService<IRecoveryConfigProvider>().Config.DefaultZoneRadius;
            _recovery?.RegisterZone(this, () => GlobalPosition, radius);
        }

        public override void _ExitTree() => _recovery?.UnregisterZone(this);
    }
}
