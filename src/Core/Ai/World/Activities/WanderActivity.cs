namespace Core.Ai.World.Activities
{
    using Godot;

    /// <summary>Strolls between random points around home, pausing at each.</summary>
    public class WanderActivity : IWorldActivity
    {
        private Vector2? _destination;
        private float _pauseLeft;

        public void Enter(WorldBrain brain) => PickDestination(brain);

        public void Tick(WorldBrain brain, float delta)
        {
            if (_destination == null || brain.IsNear(_destination.Value))
            {
                brain.Agent.StopMoving();
                _pauseLeft -= delta;
                if (_pauseLeft > 0) return;
                PickDestination(brain);
            }

            if (_destination != null)
                brain.Agent.MoveTo(_destination.Value, brain.Config.MoveSpeed);
        }

        public void Exit(WorldBrain brain) => _destination = null;

        private void PickDestination(WorldBrain brain)
        {
            float radius = brain.Config.WanderRadius;
            var offset = new Vector2(
                brain.Rnd.RandFloatRange(-radius, radius),
                brain.Rnd.RandFloatRange(-radius, radius));
            _destination = brain.Agent.HomePosition + offset;
            _pauseLeft = brain.Config.ActivityPauseSeconds;
        }
    }
}
