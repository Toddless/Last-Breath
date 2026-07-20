namespace Core.Ai.World.Activities
{
    using Godot;

    /// <summary>
    /// Rest and Sleep are configurations of one activity: claim a smart point of the tag (fallback
    /// — home), walk there, hold the pose. Sleep-style configurations dampen the senses while the
    /// pose holds (sneaking up on a sleeper works); the multipliers reset on any interruption.
    /// </summary>
    public class PointPoseActivity(string pose, string tag, WorldActivityContext context, bool dampenSenses = false) : IWorldActivity
    {
        private Vector2? _spot;
        private bool _posed;

        public void Enter(WorldBrain brain) => _spot = ResolveSpot(brain);

        public void Tick(WorldBrain brain, float delta)
        {
            var spot = _spot ?? brain.Agent.HomePosition;
            if (!brain.IsNear(spot))
            {
                DropPose(brain);
                brain.Agent.MoveTo(spot, brain.Config.MoveSpeed);
                return;
            }

            brain.Agent.StopMoving();
            if (_posed) return;

            _posed = true;
            brain.Agent.SetActivityPose(pose);
            if (!dampenSenses) return;
            brain.VisionMultiplier = brain.Config.SleepVisionMultiplier;
            brain.HearingMultiplier = brain.Config.SleepHearingMultiplier;
        }

        public void Exit(WorldBrain brain)
        {
            DropPose(brain);
            if (context.Self != null) context.Points?.Release(context.Self.InstanceId);
            _spot = null;
        }

        private void DropPose(WorldBrain brain)
        {
            if (!_posed) return;
            _posed = false;
            brain.Agent.ClearActivityPose();
            brain.VisionMultiplier = 1f;
            brain.HearingMultiplier = 1f;
        }

        /// <summary>Positional state — recomputed on every Enter. Everything taken → rest at home.</summary>
        private Vector2? ResolveSpot(WorldBrain brain)
        {
            if (context.Points == null || context.Self == null) return null;
            return context.Points.TryClaim(tag, context.Self.InstanceId, brain.Agent.Position)?.Position;
        }
    }
}
