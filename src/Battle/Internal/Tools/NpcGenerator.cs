namespace Battle.Internal.Tools
{
    using System;
    using System.Collections.Generic;
    using Npc;
    using Source;
    using Core.Entity;
    using Core.Modifiers;
    using Godot;
    using Services;

    /// <summary>
    /// Dev helper: instantiates configured <see cref="BaseNpc"/>s, applies stat modifiers and puts them in
    /// one <see cref="EntityGroup"/> so they fight the player together (1 vs N). NPCs are added to the world
    /// node so the usual encounter flow (BaseNpc.OnBodyEnter) starts the battle when the player walks in.
    /// </summary>
    public static class NpcGenerator
    {
        private const string ModifierSource = "DevTool_NpcGenerator";

        public static List<BaseNpc> SpawnGroup(Node2D world, Vector2 origin, int count, IReadOnlyList<NpcStatSpec>? stats, float spacing,
            IReadOnlyList<string>? npcIds = null, IReadOnlyList<Vector2>? patrolRoute = null)
        {
            List<BaseNpc> spawned = [];
            if (count <= 0) return spawned;

            // maxMembers gates the group; size it to the requested count so all NPCs fit in one group.
            var group = new EntityGroup(maxMembers: count);
            for (int i = 0; i < count; i++)
            {
                var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
                // Position BEFORE AddChild: entering the tree at (0,0) and teleporting afterwards
                // drags bodies overlapping the origin (the player) via MoveAndSlide's platform logic.
                npc.Position = world.ToLocal(origin + new Vector2(i * spacing, 0f));
                world.AddChild(npc); // enters the tree -> _Ready builds the components we configure below
                ApplyDefinition(npc, npcIds, i, patrolRoute);
                ApplyStats(npc, stats);
                group.TryAddToGroup(npc);
                spawned.Add(npc);
            }

            return spawned;
        }

        /// <summary>Data-driven spawn: rolls a definition from Npc.json; ids cycle when count exceeds them.</summary>
        private static void ApplyDefinition(BaseNpc npc, IReadOnlyList<string>? npcIds, int index, IReadOnlyList<Vector2>? patrolRoute)
        {
            if (npcIds == null || npcIds.Count == 0) return;

            string npcId = npcIds[index % npcIds.Count];
            try
            {
                var provider = GameServiceProvider.Instance.GetService<INpcProvider>();
                if (patrolRoute is { Count: > 0 }) npc.SetPatrolRoute(patrolRoute); // before ApplyDefinition: the brain takes the route at construction
                npc.ApplyDefinition(provider.CreateDefinition(npcId));
            }
            catch (Exception e)
            {
                GD.PrintErr($"NpcGenerator: failed to apply definition '{npcId}': {e.Message}");
            }
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
