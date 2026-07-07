namespace Core.Save.Participants
{
    using System.Collections.Generic;
    using System.Linq;
    using Ai.World;
    using Ai.World.Skirmish;
    using Data.NpcData;
    using Data.SaveData;
    using Entity;
    using Godot;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>
    /// World NPC deltas. Captured: lying bodies (rise timer preserved) and wild risen undead —
    /// the irreversible facts. Skipped: regular alive NPCs (spawn points re-roll them on load)
    /// and fighters (checkpoint saves happen out of battle). Restored bodies are WILD — no spawn
    /// point owns them (points refill independently; a conscious simplification), so rising and
    /// burning still flow through the global events and the population cap. Concrete NPC classes
    /// are project-private: instantiation goes through <see cref="INpcWorldSpawner"/>.
    /// </summary>
    public class NpcWorldSaveParticipant(
        INpcWorldRegistry registry,
        INpcProvider npcProvider,
        INpcPopulationService population,
        INpcWorldSpawner spawner) : ISaveParticipant
    {
        public string SectionId => "npcWorld";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Npc;

        public JToken Capture()
        {
            var bodies = new List<NpcBodySaveData>();
            foreach (var participant in registry.All)
            {
                if (participant is not IFightableNpc npc || npc.IsFighting) continue;

                if (npc.IsAlive)
                {
                    if (npc.IsRisen) bodies.Add(ToBodyData(npc, NpcBodySaveData.RisenKind));
                    continue;
                }

                if (npc.Lifecycle is not { } lifecycle) continue;
                string? kind = lifecycle.Stage switch
                {
                    NpcLifeStage.Defeated => NpcBodySaveData.DefeatedKind,
                    NpcLifeStage.Dormant => NpcBodySaveData.DormantKind,
                    _ => null
                };
                if (kind != null) bodies.Add(ToBodyData(npc, kind));
            }

            return JToken.FromObject(new NpcWorldSaveData { Bodies = bodies });
        }

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<NpcWorldSaveData>();
            if (saved == null) return;

            foreach (var body in saved.Bodies)
            {
                if (!npcProvider.KnownNpcIds.Contains(body.NpcId)) continue; // NPC removed from the data
                if (!population.TryReserve()) continue; // cap reached: fresh spawns won, the body is dropped

                var definition = npcProvider.CreateDefinition(body.NpcId, new NpcDefinitionOverrides
                {
                    Level = body.Level,
                    Rarity = body.Rarity,
                    Stance = body.Stance
                });

                var npc = spawner.Spawn(definition, new Vector2(body.X, body.Y));
                if (npc == null) break; // no world to spawn into — every later body would fail too

                switch (body.Kind)
                {
                    case NpcBodySaveData.RisenKind:
                        npc.RestoreAsRisen(body.RisingBonus);
                        break;
                    case NpcBodySaveData.DormantKind:
                        npc.RestoreAsBody(NpcLifeStage.Dormant, body.ResurrectDelay, body.Elapsed);
                        break;
                    default:
                        npc.RestoreAsBody(NpcLifeStage.Defeated, body.ResurrectDelay, body.Elapsed);
                        break;
                }
            }
        }

        private static NpcBodySaveData ToBodyData(IFightableNpc npc, string kind) => new()
        {
            Kind = kind,
            NpcId = npc.Id,
            Level = npc.Level,
            Rarity = npc.Rarity,
            Stance = npc.AbilityBook.CurrentStance,
            X = npc.Position.X,
            Y = npc.Position.Y,
            ResurrectDelay = npc.Lifecycle?.ResurrectDelay ?? 0f,
            Elapsed = npc.Lifecycle?.Elapsed ?? 0f,
            RisingBonus = npc.RisingBonus
        };
    }
}
