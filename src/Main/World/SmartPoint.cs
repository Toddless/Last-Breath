namespace LastBreath.World
{
    using Core.Ai.World.SmartPoints;
    using Godot;
    using Services;

    /// <summary>
    /// A claimable spot of interest in the world scene (campfire, tent, ore vein, trade stall):
    /// activities find and claim it through <see cref="ISmartPointRegistry"/> by tag. Pure anchor —
    /// no physics, the visual is authored in the scene.
    /// </summary>
    [GlobalClass]
    public partial class SmartPoint : Node2D, ISmartPoint
    {
        [Export] private string _tag = SmartPointTags.Campfire;
        [Export] private int _capacity = 1;

        private ISmartPointRegistry? _registry;

        public string Tag => _tag;

        public int Capacity => Mathf.Max(1, _capacity);

        Vector2 ISmartPoint.Position => GlobalPosition;

        public override void _Ready()
        {
            _registry = GameServiceProvider.Instance.GetService<ISmartPointRegistry>();
            _registry?.Register(this);
        }

        public override void _ExitTree() => _registry?.Unregister(this);
    }
}
