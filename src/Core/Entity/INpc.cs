namespace Core.Entity
{
    using Ai.World;
    using Data.NpcData;
    using Enums;
    using Godot;

    public interface INpc : IEntity
    {
        int Level { get; }
        Rarity Rarity { get; }
        EntityType EntityType { get; }
        Fractions Fraction { get; }
        INpcLifecycle? Lifecycle { get; }

        Vector2 Position { get; set; }

        /// <summary>
        /// Turns the randomly-initialized NPC into a data-driven one: overrides the rolled base
        /// parameters, fixes the stance, learns the rolled abilities (Learn auto-equips them)
        /// and attaches the combat behavior. Call after _Ready has built the components.
        /// </summary>
        void ApplyDefinition(NpcDefinition definition);

        /// <summary>Save-load path: rebuilds a lying body. Health drops through the normal property —
        /// in Battle the game-bus death event has no subscribers; a future loot orchestrator in Main
        /// must check ILoadScope before reacting to deaths.</summary>
        void RestoreAsBody(NpcLifeStage stage, float resurrectDelay, float elapsed);

        /// <summary>Save-load path: rebuilds a wild risen undead. No NpcFactionChangedEvent —
        /// the original spawn point already replaced this NPC before the save.</summary>
        void RestoreAsRisen(float parameterBonus);
    }
}
