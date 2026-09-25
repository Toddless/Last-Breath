namespace Battle.Internal.Npc
{
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Services;
    using Core.World.Spaces;
    using Godot;

    /// <summary>
    /// Battle-side <see cref="INpcWorldSpawner"/>: every world NPC here is the test BaseNpc and
    /// the world node is the player's parent. Registered in the project bootstrap
    /// (Battle.Services.GameServiceProvider) — the only layer allowed to know Internal classes.
    /// </summary>
    internal class BattleNpcWorldSpawner(IPlayerAccessor playerAccessor, IGameServiceProvider provider) : INpcWorldSpawner
    {
        public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position)
        {
            if (playerAccessor.Player is not { IsFighting: false }) return null;
            return playerAccessor.Player is not Node2D playerNode ? null : SpawnAt(definition, position, playerNode);
        }

        public IFightableNpc? SpawnAt(NpcDefinition definition, Vector2 position, object source)
        {
            if (source is not Node2D node || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree() || node.IsQueuedForDeletion()) return null;
            var world = SpatialAccess.GetSpaceRoot(node);

            var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
            npc.InjectServices(provider);
            // Position BEFORE AddChild: entering the tree at (0,0) and teleporting afterwards
            // drags bodies overlapping the origin (the player) via MoveAndSlide's platform logic.
            npc.Position = world.ToLocal(position);
            world.AddChild(npc); // _Ready builds the components ApplyDefinition configures
            npc.ApplyDefinition(definition, provider);
            return npc;
        }

        public void Despawn(IFightableNpc npc)
        {
            if (npc is Node node) node.QueueFree();
        }
    }
}
