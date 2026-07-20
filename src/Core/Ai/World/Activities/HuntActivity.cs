namespace Core.Ai.World.Activities
{
    using Skirmish;

    /// <summary>
    /// Seeks the nearest live hostile NPC (faction matrix) within the leash of home and walks
    /// into contact — the skirmish scan takes over from there. No prey (or no registries in this
    /// scene) → waits at home like Idle. Aggressive-only by data convention.
    /// </summary>
    public class HuntActivity(WorldActivityContext context) : IWorldActivity
    {
        public void Enter(WorldBrain brain)
        {
        }

        public void Tick(WorldBrain brain, float delta)
        {
            var prey = FindPrey(brain);
            if (prey == null)
            {
                if (brain.IsNear(brain.Agent.HomePosition)) brain.Agent.StopMoving();
                else brain.Agent.MoveTo(brain.Agent.HomePosition, brain.Config.MoveSpeed);
                return;
            }

            brain.Agent.MoveTo(prey.Position, brain.Config.MoveSpeed);
        }

        public void Exit(WorldBrain brain)
        {
        }

        private ISkirmishParticipant? FindPrey(WorldBrain brain)
        {
            if (context.Npcs == null || context.Relations == null || context.Self == null) return null;

            ISkirmishParticipant? nearest = null;
            float best = float.MaxValue;
            foreach (var candidate in context.Npcs.All)
            {
                if (ReferenceEquals(candidate, context.Self) || !candidate.IsAlive || candidate.IsFighting) continue;
                if (!context.Relations.IsHostile(context.Self.Fraction, candidate.Fraction)) continue;
                // The leash anchors hunting to home: prey beyond it would only ping-pong the chase.
                if (brain.Agent.HomePosition.DistanceTo(candidate.Position) > brain.Config.LeashRadius) continue;

                float distance = brain.Agent.Position.DistanceSquaredTo(candidate.Position);
                if (distance >= best) continue;
                best = distance;
                nearest = candidate;
            }

            return nearest;
        }
    }
}
