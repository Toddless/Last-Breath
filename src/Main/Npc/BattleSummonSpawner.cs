namespace LastBreath.Npc
{
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Entity;
    using Godot;

    /// <summary>
    /// Main-side <see cref="IBattleNpcSpawner"/>: spawns summon bodies straight into the arena
    /// (never the world) with the summon flag raised. Registered in the project bootstrap — the
    /// only layer allowed to know project NPC classes; the arena consumes the interface.
    /// </summary>
    internal class BattleSummonSpawner(IGameServiceProvider provider) : IBattleNpcSpawner
    {
        public IFightableNpc Spawn(NpcDefinition definition, Node2D parent, Vector2 globalPosition)
        {
            var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
            npc.InjectServices(provider);
            // Position BEFORE AddChild: entering the tree at (0,0) and teleporting afterwards
            // drags bodies overlapping the origin via MoveAndSlide's platform logic.
            npc.Position = parent.ToLocal(globalPosition);
            parent.AddChild(npc); // _Ready builds the components ApplyDefinition configures
            npc.ApplyDefinition(definition, provider);
            npc.MarkAsSummon();
            return npc;
        }

        public void Despawn(IFightableNpc npc)
        {
            if (npc is Node node) node.QueueFree();
        }
    }
}
