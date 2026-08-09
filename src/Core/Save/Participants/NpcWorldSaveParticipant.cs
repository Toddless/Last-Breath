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

    /// <summary>
    /// World NPC deltas. Captured: lying bodies (rise timer preserved), wild risen undead and wild
    /// LIVING NPCs — the facts nothing else re-creates. Skipped: living NPCs a spawn point owns (the
    /// point re-rolls its own roster on load), summons (battle-scoped, no world return) and fighters
    /// (checkpoint saves happen out of battle). Whether nobody owns an NPC travels IN THE RECORD, so a
    /// restored quest NPC stays nobody's while a restored body returns to the roster its point still
    /// counts it in — points refill independently, and a pair that the world used to heal by itself
    /// once the body stood up again must not be written down forever. Rising and burning still flow
    /// through the global events and the population cap. Concrete NPC classes are project-private:
    /// instantiation goes through <see cref="INpcWorldSpawner"/>.
    /// <para>
    /// How a restored body ends is the business of the cycle the DEFINITION builds, never of the kind
    /// written in the file: a body on a cycle that gets up alive (a peaceful resident) is laid down by
    /// the very same path, and then it neither burns nor publishes anything — it simply stands back up
    /// as itself, so none of the global events above are its story.
    /// </para>
    /// <para>
    /// Identity is re-rolled from the record everywhere EXCEPT the modifiers of a wild living NPC:
    /// a quest's trial target fought against a different set is a different trial, so its ids are
    /// restated one by one. Bodies keep re-rolling theirs — an accepted fidelity loss for the many.
    /// </para>
    /// <para>
    /// What a living record deliberately does NOT carry: vitals (a wounded target stands up whole),
    /// the rolled ability set (it re-rolls with the definition) and the instance id (a new one, so the
    /// personal reputation the player earned with THAT body is forgotten). Identity here is the record
    /// plus its modifiers; the rest is the state of a session, not of the world.
    /// </para>
    /// </summary>
    public class NpcWorldSaveParticipant(
        INpcWorldRegistry registry,
        INpcProvider npcProvider,
        INpcModifierProvider modifierProvider,
        INpcPopulationService population,
        INpcWorldSpawner spawner) : ISaveParticipant
    {
        public string SectionId => "npcWorld";

        /// <summary>2 — wild LIVING NPCs joined the section (with the modifier ids they wear). A
        /// version 1 file simply holds none, which is what a save written before quests could put a
        /// nobody's NPC into the world means; its bodies read exactly as they always did.</summary>
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

                // A living wild NPC was named by hand (a quest's trial target) and reserved its slot
                // outside the limit when it first spawned; the load repeats that promise, so a world
                // fuller than at save time pauses the spawn points instead of dropping the target.
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

                // Wildness is a fact of the RECORD, not of the restore. A quest's NPC comes back
                // nobody's and the next save carries it again; a body a point still counts in its
                // roster comes back the point's, so once it is on its feet the world is back to one
                // resident instead of writing the load-time pair down forever.
                if (body.Wild) npc.MarkAsWild();

                switch (body.Kind)
                {
                    case NpcBodySaveData.AliveKind:
                        break; // a fresh spawn already stands there with full vitals: nothing to lay down

                    case NpcBodySaveData.RisenKind:
                        // The cycle the definition built decides the ending, not the kind in the file:
                        // a record written while this NPC still rose undead must not hand the fate to
                        // today's peaceful resident. A resident that was up and about at save time is
                        // simply alive — the fresh spawn already stands there, nothing to restore.
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

        /// <summary>The modifier ids of the record the catalog still knows. A trial the data has since
        /// stripped a modifier from comes back a modifier short: a weaker target the quest can still
        /// finish beats a target the load drops and a stage that never ends.</summary>
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
