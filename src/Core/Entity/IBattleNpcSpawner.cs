namespace Core.Entity
{
    using Data.NpcData;
    using Godot;

    /// <summary>
    /// Spawns NPC bodies for battle-scoped use (summons): the node lands under the given parent
    /// with the definition applied and the summon flag raised. Registered by the project bootstrap;
    /// a project without the binding simply fights without summons.
    /// </summary>
    public interface IBattleNpcSpawner
    {
        IFightableNpc? Spawn(NpcDefinition definition, Node2D parent, Vector2 globalPosition);

        void Despawn(IFightableNpc npc);
    }
}
