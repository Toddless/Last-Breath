namespace LastBreath.World
{
    using Core.Ai.World;
    using Core.Events;
    using Core.Events.GameEvents;
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
        private float _pulseCooldown = NoisePulseSeconds;

        public void Setup(IGameEventBus gameEventBus)
        {
            _gameEventBus = gameEventBus;
            var area = new Area2D { CollisionMask = uint.MaxValue };
            var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = ContactRadius } };
            area.AddChild(shape);
            area.BodyEntered += OnBodyEntered;
            // Battle start happens inside a physics callback (BodyEntered): adding a collision
            // object to the tree mid-flush is forbidden — "can't change state while flushing queries".
            CallDeferred(Node.MethodName.AddChild, area);
        }

        public override void _Process(double delta)
        {
            if (_gameEventBus == null) return;
            _pulseCooldown -= (float)delta;
            if (_pulseCooldown > 0) return;

            _pulseCooldown = NoisePulseSeconds;
            // The clash stays audible for its whole duration: latecomers in hearing range keep coming.
            _gameEventBus.Publish(new WorldStimulusEvent(new Stimulus(StimulusType.Noise, GlobalPosition)));
        }

        private void OnBodyEntered(Node2D body)
        {
            if (_gameEventBus == null) return;
            if (body is not BaseNpc npc || !npc.IsAlive || npc.IsFighting) return;
            if (!npc.ConsidersPlayerAnEnemy()) return;

            npc.StopMoving(); // records the world position the fighter returns to after the battle
            _gameEventBus.Publish(new BattleJoinRequestEvent(npc, AlliedWithPlayer: false));
        }
    }
}
