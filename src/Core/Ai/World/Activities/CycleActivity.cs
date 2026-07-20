namespace Core.Ai.World.Activities
{
    using System.Collections.Generic;

    /// <summary>
    /// The routine composite: runs its steps in order, advancing when the current task completes,
    /// looping forever. A step that is not an <see cref="IWorldTask"/> is terminal — the cycle
    /// stays on it. The current index is progress: interruptions resume the same step.
    /// </summary>
    public class CycleActivity(IReadOnlyList<IWorldActivity> steps) : IWorldActivity
    {
        private int _index;

        private IWorldActivity? Current => steps.Count == 0 ? null : steps[_index % steps.Count];

        public void Enter(WorldBrain brain) => Current?.Enter(brain);

        public void Tick(WorldBrain brain, float delta)
        {
            if (Current is null) return;

            if (Current is IWorldTask { IsCompleted: true } finished)
            {
                finished.Exit(brain);
                finished.Restart();
                _index = (_index + 1) % steps.Count;
                Current.Enter(brain);
                return; // the fresh step starts ticking next frame
            }

            Current.Tick(brain, delta);
        }

        public void Exit(WorldBrain brain) => Current?.Exit(brain);
    }
}
