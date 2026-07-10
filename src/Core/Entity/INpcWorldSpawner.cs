namespace Core.Entity
{
    using Data.NpcData;
    using Godot;

    /// <summary>
    /// Project-side factory of world NPC nodes. Concrete NPC classes are project-private
    /// (Battle's test BaseNpc today; Main's regular/boss/quest NPCs tomorrow), so shared code
    /// (the save system, future A-Life) spawns through this seam. The implementation picks the
    /// concrete class/scene from the definition and owns placing the node into the world.
    /// </summary>
    public interface INpcWorldSpawner
    {
        /// <summary>Spawns a data-driven NPC at the world position; null when no world is available.</summary>
        IFightableNpc? Spawn(NpcDefinition definition, Vector2 position);

        /// <summary>Removes a spawned NPC from the world (raid survivors leaving). Bookkeeping —
        /// population, personal memory — is the caller's concern via the final-death event.</summary>
        void Despawn(IFightableNpc npc);
    }
}
