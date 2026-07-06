namespace Core.Ai.World.Activities
{
    /// <summary>Stands at home. Walks back first if an alert dragged the NPC away.</summary>
    public class IdleActivity : IWorldActivity
    {
        public void Enter(WorldBrain brain)
        {
            if (!brain.IsNear(brain.Agent.HomePosition))
                brain.Agent.MoveTo(brain.Agent.HomePosition, brain.Config.MoveSpeed);
        }

        public void Tick(WorldBrain brain, float delta)
        {
            if (brain.IsNear(brain.Agent.HomePosition))
                brain.Agent.StopMoving();
        }

        public void Exit(WorldBrain brain)
        {
        }
    }
}
