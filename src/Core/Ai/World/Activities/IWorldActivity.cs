namespace Core.Ai.World.Activities
{
    /// <summary>
    /// What the NPC does while Calm. Activities move the body through the brain's agent and
    /// must be resumable: Enter runs again every time the brain returns to Calm.
    /// </summary>
    public interface IWorldActivity
    {
        void Enter(WorldBrain brain);
        void Tick(WorldBrain brain, float delta);
        void Exit(WorldBrain brain);
    }
}
