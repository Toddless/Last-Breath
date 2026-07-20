namespace Core.Ai.World.Activities
{
    using SmartPoints;

    /// <summary>
    /// Claims a resource smart point of the tag, works it in the Work pose; every full cycle adds
    /// one unit to the WorldFacts counter <c>Npc_Harvested_&lt;tag&gt;</c> — the cheap seed for
    /// economy and quests. Accumulated work is progress (an interrupted miner finishes the ore
    /// later); the claimed point is positional and re-resolved on Enter. No free point → idles at home.
    /// </summary>
    public class HarvestActivity(string tag, WorldActivityContext context) : IWorldActivity
    {
        /// <summary>Real seconds of standing at the point per one harvested unit.</summary>
        private const float SecondsPerYield = 10f;

        private ISmartPoint? _point;
        private float _progressSeconds;
        private bool _posed;

        public void Enter(WorldBrain brain) => _point = Claim(brain);

        public void Tick(WorldBrain brain, float delta)
        {
            if (_point == null)
            {
                if (brain.IsNear(brain.Agent.HomePosition)) brain.Agent.StopMoving();
                else brain.Agent.MoveTo(brain.Agent.HomePosition, brain.Config.MoveSpeed);
                return;
            }

            if (!brain.IsNear(_point.Position))
            {
                DropPose(brain);
                brain.Agent.MoveTo(_point.Position, brain.Config.MoveSpeed);
                return;
            }

            brain.Agent.StopMoving();
            if (!_posed)
            {
                _posed = true;
                brain.Agent.SetActivityPose(ActivityPoses.Work);
            }

            _progressSeconds += delta;
            if (_progressSeconds < SecondsPerYield) return;

            _progressSeconds = 0;
            context.Facts?.Add($"Npc_Harvested_{tag}");
        }

        public void Exit(WorldBrain brain)
        {
            DropPose(brain);
            if (context.Self != null) context.Points?.Release(context.Self.InstanceId);
            _point = null;
        }

        private void DropPose(WorldBrain brain)
        {
            if (!_posed) return;
            _posed = false;
            brain.Agent.ClearActivityPose();
        }

        private ISmartPoint? Claim(WorldBrain brain)
        {
            if (context.Points == null || context.Self == null) return null;
            return context.Points.TryClaim(tag, context.Self.InstanceId, brain.Agent.Position);
        }
    }
}
