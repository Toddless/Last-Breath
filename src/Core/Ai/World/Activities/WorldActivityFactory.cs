namespace Core.Ai.World.Activities
{
    using SmartPoints;

    /// <summary>One place mapping activity types to instances (the brain, schedule slots and
    /// routine steps share it). Dependencies come through <see cref="WorldActivityContext"/> —
    /// a missing dependency degrades the activity toward Idle instead of failing.</summary>
    public static class WorldActivityFactory
    {
        public static IWorldActivity Create(WorldActivityType type, WorldActivityContext context, float? wanderRadius = null, string? pointTag = null) => type switch
        {
            WorldActivityType.Wander => new WanderActivity(wanderRadius),
            WorldActivityType.Patrol when context.PatrolRoute is { Count: > 0 } => new PatrolActivity(context.PatrolRoute),
            WorldActivityType.Rest => new PointPoseActivity(ActivityPoses.Rest, pointTag ?? SmartPointTags.Campfire, context),
            WorldActivityType.Sleep => new PointPoseActivity(ActivityPoses.Sleep, pointTag ?? SmartPointTags.Tent, context, dampenSenses: true),
            WorldActivityType.Hunt => new HuntActivity(context),
            WorldActivityType.Harvest => new HarvestActivity(pointTag ?? SmartPointTags.OreVein, context),
            WorldActivityType.Work => new PointPoseActivity(ActivityPoses.Work, pointTag ?? SmartPointTags.Forge, context),
            _ => new IdleActivity()
        };
    }
}
