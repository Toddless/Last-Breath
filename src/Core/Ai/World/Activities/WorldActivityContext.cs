namespace Core.Ai.World.Activities
{
    using System.Collections.Generic;
    using Entity;
    using Godot;
    using Narrative.Facts;
    using Recovery;
    using Skirmish;
    using SmartPoints;
    using Time;

    /// <summary>
    /// Everything an activity may need from the world, gathered by the body's adapter. All fields
    /// are optional — an activity missing its dependency degrades to Idle-like behavior, so
    /// sandbox scenes without the full service stack keep working.
    /// </summary>
    public record WorldActivityContext
    {
        public IReadOnlyList<Vector2>? PatrolRoute { get; init; }
        public IWorldClock? Clock { get; init; }
        public INpcWorldRegistry? Npcs { get; init; }
        public IFactionRelationService? Relations { get; init; }
        public ISmartPointRegistry? Points { get; init; }
        public IWorldFactsService? Facts { get; init; }
        public RecoveryConfig? Recovery { get; init; }

        /// <summary>The owner as the skirmish layer sees it — Hunt targeting and smart-point claims key off it.</summary>
        public ISkirmishParticipant? Self { get; init; }
    }
}
