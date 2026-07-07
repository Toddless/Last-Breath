namespace Battle.Internal.Npc
{
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Services;
    using Godot;
    using GameServiceProvider = Services.GameServiceProvider;

    /// <summary>
    /// Battle-side <see cref="INpcWorldSpawner"/>: every world NPC here is the test BaseNpc and
    /// the world node is the player's parent. Registered in the project bootstrap
    /// (Battle.Services.GameServiceProvider) — the only layer allowed to know Internal classes.
    /// </summary>
    internal class BattleNpcWorldSpawner(IPlayerAccessor playerAccessor) : INpcWorldSpawner
    {
        public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position)
        {
            if (playerAccessor.Player is not Node playerNode || playerNode.GetParent() is not Node2D world) return null;

            var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
            npc.InjectServices(GameServiceProvider.Instance);
            world.AddChild(npc); // _Ready builds the components ApplyDefinition configures
            npc.GlobalPosition = position;
            npc.ApplyDefinition(definition);
            return npc;
        }
    }
}
