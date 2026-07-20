namespace Core.Ai.World.Activities
{
    /// <summary>
    /// What the NPC does while Calm. Activities move the body through the brain's agent.
    /// RESUMABILITY CONTRACT: Enter runs again every time the brain returns to Calm (after an
    /// alert, a battle, a schedule switch) — fields are PROGRESS and must survive Exit/Enter;
    /// Enter only recomputes positional state (paths, claims). An interrupted activity picks up
    /// where it left off, it does not start over.
    /// </summary>
    public interface IWorldActivity
    {
        void Enter(WorldBrain brain);
        void Tick(WorldBrain brain, float delta);
        void Exit(WorldBrain brain);
    }
}
