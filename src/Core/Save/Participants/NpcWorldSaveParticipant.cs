namespace Core.Save.Participants
{
    using System.Collections.Generic;
    using System.Linq;
    using Ai.World;
    using Ai.World.Skirmish;
    using Data;
    using Data.NpcData;
    using Data.SaveData;
    using Entity;
    using Godot;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>World NPC deltas: lying bodies (rise timer kept), wild risen undead, and wild LIVING
    /// NPCs — the facts nothing else recreates. Skipped: spawn-point-owned living NPCs (point re-rolls
    /// its own roster), summons (battle-scoped), fighters (checkpoint saves happen out of battle).
    /// Concrete NPC classes are project-private; instantiation goes through <see cref="INpcWorldSpawner"/>.
    /// Ownership ("wild") is stored in the record, not derived at restore: a quest NPC stays nobody's,
    /// a body returns to the point's roster it still counts — points refill independently, so that pair
    /// must not be written down forever. A restored body's ending follows the CURRENT definition's
    /// lifecycle, not the kind written in the file — one now built as a peaceful resident just stands
    /// back up, no rise/burn events fire. Identity re-rolls from the record everywhere except a wild
    /// living NPC's modifiers, restated one by one (a quest trial needs its exact set); bodies re-roll
    /// theirs. Not carried: vitals, rolled ability set, instance id (so past-life reputation resets) —
    /// identity here is the record plus modifiers only.</summary>
    public class NpcWorldSaveParticipant(
        INpcWorldRegistry registry,
        INpcProvider npcProvider,
        INpcModifierProvider modifierProvider,
        INpcPopulationService population,
        INpcWorldSpawner spawner) : ISaveParticipant
    {
        public string SectionId => "npcWorld";

        /// <summary>2 — wild LIVING NPCs joined the section (with their modifier ids). A version 1
        /// file simply holds none, so its bodies read exactly as they always did.</summary>
        public int Version => 2;

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
                    // Nobody's living NPC: no spawn point will put it back, so the file is its only
                    // way home. A summon is battle-scoped even when it belongs to no point.
                    else if (npc is { IsWild: true, IsSummon: false }) bodies.Add(ToBodyData(npc, NpcBodySaveData.AliveKind));
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
                bool alive = body.Kind == NpcBodySaveData.AliveKind;

                // A living wild NPC reserved its slot outside the limit when it first spawned; the load
                // repeats that promise, so a world fuller than at save time pauses the spawn points
                // instead of dropping the target.
                if (alive) population.ReserveOutsideLimit();
                else if (!population.TryReserve()) continue; // cap reached: fresh spawns won, the body is dropped

                var definition = npcProvider.CreateDefinition(body.NpcId, new NpcDefinitionOverrides
                {
                    Level = body.Level,
                    Rarity = body.Rarity,
                    Stance = body.Stance
                });
                if (alive) definition = definition with { Modifiers = ReadModifiers(body) };

                var npc = spawner.Spawn(definition, new Vector2(body.X, body.Y));
                if (npc == null) break; // no world to spawn into — every later body would fail too

                // Wildness is a fact of the record, not of the restore (see class doc).
                if (body.Wild) npc.MarkAsWild();

                switch (body.Kind)
                {
                    case NpcBodySaveData.AliveKind:
                        break; // a fresh spawn already stands there with full vitals: nothing to lay down

                    case NpcBodySaveData.RisenKind:
                        // Current definition decides the ending, not the file (see class doc).
                        if (npc.Lifecycle is IUndeadRiseLifecycle) npc.RestoreAsRisen(body.RisingBonus);
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

        /// <summary>Modifier ids from the record that the catalog still knows; a stripped modifier is
        /// dropped rather than failing the whole restore.</summary>
        private List<INpcModifier> ReadModifiers(NpcBodySaveData body)
        {
            var known = modifierProvider.GetAllModifierIds();
            var modifiers = new List<INpcModifier>();
            foreach (string id in body.Modifiers)
            {
                if (!known.Contains(id))
                {
                    Tracker.TrackNotFound($"npcWorld restore of '{body.NpcId}': modifier '{id}'", this);
                    continue;
                }

                modifiers.Add(modifierProvider.GetModifier(id));
            }

            return modifiers;
        }

        private static NpcBodySaveData ToBodyData(IFightableNpc npc, string kind) => new()
        {
            Kind = kind,
            Wild = npc.IsWild,
            NpcId = npc.Id,
            Level = npc.Level,
            Rarity = npc.Rarity,
            Stance = npc.AbilityBook.CurrentStance,
            X = npc.Position.X,
            Y = npc.Position.Y,
            ResurrectDelay = npc.Lifecycle?.ResurrectDelay ?? 0f,
            Elapsed = npc.Lifecycle?.Elapsed ?? 0f,
            RisingBonus = npc.RisingBonus,
            // Only the living wild NPC restates them; a body's re-roll needs no list in the file.
            Modifiers = kind == NpcBodySaveData.AliveKind
                ? [.. npc.NpcModifiers.AllModifiers.Select(modifier => modifier.Id)]
                : []
        };
    }
}
