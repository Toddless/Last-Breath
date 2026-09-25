namespace LastBreath.World
{
    using Core.Ai.World;
    using Core.Events;
    using Core.World.Spaces;
    using Godot;
    using Npc;

    /// <summary>
    /// World-side presence of an ongoing battle: stands where the player was engaged, periodically
    /// emits noise (aggressive brains investigate it) and turns hostile arrivals into join requests.
    /// Created by Main on battle start, freed when the battle ends.
    /// </summary>
    public partial class BattleSiteMarker : Node2D
    {
        private const float NoisePulseSeconds = 5f;
        private const float ContactRadius = 120f;

        private IGameEventBus? _gameEventBus;
        private BattleSiteRegistry? _sites;
        public System.Guid BattleId { get; } = System.Guid.NewGuid();
        private float _pulseCooldown = NoisePulseSeconds;

        public void Setup(IGameEventBus gameEventBus, BattleSiteRegistry sites)
        {
            _gameEventBus = gameEventBus;
            _sites = sites;
            sites.Register(new BattleSite(BattleId, NativeSpatialQuery.Instance.GetSpace(this), GlobalPosition));
            var area = new Area2D { CollisionMask = uint.MaxValue };
            var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = ContactRadius } };
            area.AddChild(shape);
            area.BodyEntered += OnBodyEntered;
            // Battle start happens inside a physics callback (BodyEntered): adding a collision
            // object to the tree mid-flush is forbidden — "can't change state while flushing queries".
            CallDeferred(Node.MethodName.AddChild, area);
        }

        public void Close()
        {
            _sites?.Remove(BattleId);
            _gameEventBus = null;
        }

        public override void _ExitTree() => Close();

        public override void _Process(double delta)
        {
            if (_gameEventBus == null) return;
            _pulseCooldown -= (float)delta;
            if (_pulseCooldown > 0) return;

            _pulseCooldown = NoisePulseSeconds;
            // The clash stays audible for its whole duration: latecomers in hearing range keep coming.
            _gameEventBus.Publish(new WorldStimulusEvent(new Stimulus(StimulusType.Noise, GlobalPosition, NativeSpatialQuery.Instance.GetSpace(this))));
        }

        private void OnBodyEntered(Node2D body)
        {
            if (_gameEventBus == null) return;
            if (body is not BaseNpc npc || !npc.IsAlive || npc.IsFighting) return;
            if (!npc.ConsidersPlayerAnEnemy()) return;

            npc.StopMoving(); // BattleContext captures placement when admission succeeds.
            _gameEventBus.Publish(new BattleJoinRequestEvent(npc, AlliedWithPlayer: false, BattleId: BattleId));
        }
    }
}
