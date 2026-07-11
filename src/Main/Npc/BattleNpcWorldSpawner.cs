namespace LastBreath.Npc
{
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Services;
    using Godot;

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
            // Position BEFORE AddChild: entering the tree at (0,0) and teleporting afterwards
            // drags bodies overlapping the origin (the player) via MoveAndSlide's platform logic.
            npc.Position = world.ToLocal(position);
            world.AddChild(npc); // _Ready builds the components ApplyDefinition configures
            npc.ApplyDefinition(definition);
            return npc;
        }

        public void Despawn(IFightableNpc npc)
        {
            if (npc is Node node) node.QueueFree();
        }
    }
}
