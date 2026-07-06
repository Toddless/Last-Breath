namespace Core.Ai.World.Activities
{
    using System.Collections.Generic;
    using Godot;

    /// <summary>One place mapping activity types to instances (the brain and schedule slots share it).</summary>
    public static class WorldActivityFactory
    {
        public static IWorldActivity Create(WorldActivityType type, IReadOnlyList<Vector2>? patrolRoute, float? wanderRadius = null) => type switch
        {
            WorldActivityType.Wander => new WanderActivity(wanderRadius),
            WorldActivityType.Patrol when patrolRoute is { Count: > 0 } => new PatrolActivity(patrolRoute),
            _ => new IdleActivity()
        };
    }
}
