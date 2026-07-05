namespace Battle.Internal.Tools
{
    using System.Collections.Generic;
    using Battle.Internal.Npc;
    using Battle.Source;
    using Core.Modifiers;
    using Godot;

    /// <summary>
    /// Dev helper: instantiates configured <see cref="BaseNpc"/>s, applies stat modifiers and puts them in
    /// one <see cref="EntityGroup"/> so they fight the player together (1 vs N). NPCs are added to the world
    /// node so the usual encounter flow (BaseNpc.OnBodyEnter) starts the battle when the player walks in.
    /// </summary>
    public static class NpcGenerator
    {
        private const string ModifierSource = "DevTool_NpcGenerator";

        public static List<BaseNpc> SpawnGroup(Node2D world, Vector2 origin, int count, IReadOnlyList<NpcStatSpec>? stats, float spacing)
        {
            List<BaseNpc> spawned = [];
            if (count <= 0) return spawned;

            // maxMembers gates the group; size it to the requested count so all NPCs fit in one group.
            var group = new EntityGroup(maxMembers: count);
            for (int i = 0; i < count; i++)
            {
                var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
                world.AddChild(npc); // enters the tree -> _Ready builds the components we configure below
                npc.GlobalPosition = origin + new Vector2(i * spacing, 0f);
                ApplyStats(npc, stats);
                group.TryAddToGroup(npc);
                spawned.Add(npc);
            }

            return spawned;
        }

        private static void ApplyStats(BaseNpc npc, IReadOnlyList<NpcStatSpec>? stats)
        {
            if (stats != null)
            {
                foreach (var stat in stats)
                {
                    if (stat == null) continue;
                    var modifier = ModifiersCreator.CreateModifierInstance(stat.Parameter, stat.ValueType, stat.Value, ModifierSource);
                    npc.ParameterModifiers.AddModifier(modifier);
                }
            }

            // _Ready set current resources from the base max; refill so bars reflect the modified max.
            npc.CurrentHealth = npc.Parameters.MaxHealth;
            npc.CurrentMana = npc.Parameters.MaxMana;
        }
    }
}
