namespace Core.Data.NpcData
{
    using System.Collections.Generic;
    using Ai;
    using Enums;
    using Interfaces.Abilities;
    using Interfaces.Entity;

    /// <summary>
    /// A fully rolled NPC ready to be applied to an entity: the provider resolved the stance,
    /// level, rarity, ability instances and behavior; parameters are already level-scaled.
    /// </summary>
    public record NpcDefinition
    {
        public required string NpcId { get; init; }
        public int Level { get; init; }
        public Rarity Rarity { get; init; }
        public EntityType EntityType { get; init; }
        public Fractions Fraction { get; init; }
        public Stance Stance { get; init; }
        public required IReadOnlyDictionary<EntityParameter, float> Parameters { get; init; }
        public required IReadOnlyList<IAbility> Abilities { get; init; }

        /// <summary>Rolled NPC modifiers (count = type × rarity); copies, ready to attach.</summary>
        public IReadOnlyList<INpcModifier> Modifiers { get; init; } = [];
        public BehaviorProfile? Behavior { get; init; }

        /// <summary>World-mode brain tuning; null = the NPC stands still like before.</summary>
        public Ai.World.WorldBrainConfig? World { get; init; }

        /// <summary>Post-defeat rules (resurrection/burning). Always present — defaults if not authored.</summary>
        public required Ai.World.NpcLifecycleConfig Lifecycle { get; init; }
    }
}
