namespace Core.Narrative.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ai.World.Raids;
    using Data;
    using Data.GameData;
    using Entity;
    using Godot;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>
    /// Puts a named NPC into the world: the quest vocabulary's way of producing a trial target.
    /// The place is named by the id of a spawn point standing in the scene — the world already
    /// addresses its own spots that way, and an empty point (radius 0, no roster) is how a fixed
    /// position is authored. The spawned NPC belongs to NOBODY: the point only lends its
    /// coordinates, keeps its own roster untouched and never respawns the target. Death is the
    /// normal lifecycle of the record — a trial target may well get back up as undead.
    /// <para>
    /// A marker point placed only to be addressed by name MUST be authored with the recovery zone
    /// off: a spawn point registers one unconditionally, and an empty point resolves to no faction,
    /// so everyone — the player included — would heal inside its radius. Point ids are expected to
    /// be unique; a duplicate resolves to whichever point registered first, exactly as the spawn
    /// point save participant resolves it.
    /// </para>
    /// <para>
    /// Nothing here is idempotent, exactly like every other narrative action: the entry fires once
    /// per stage entry, and no path re-enters a stage or replays a quest transition on load.
    /// </para>
    /// </summary>
    public class SpawnNpcAction(
        INpcProvider npcs,
        INpcModifierProvider modifierProvider,
        INpcWorldSpawner spawner,
        INpcPopulationService population,
        ISpawnPointRegistry points,
        string npcId,
        string pointId,
        IReadOnlyList<string>? modifierIds) : INarrativeAction
    {
        /// <summary>What the refusal says instead of a list when the scene registered no point at all —
        /// a world loaded without its spawn points is a different fault from a mistyped id.</summary>
        private const string NoPointsRegistered = "none registered";

        public void Execute(NarrativeContext context)
        {
            if (!npcs.KnownNpcIds.Contains(npcId))
            {
                Tracker.TrackNotFound($"SpawnNpc action: npc '{npcId}'", this);
                return;
            }

            // All-or-nothing: a trial fought against a shorter modifier list is a different trial.
            if (UnknownModifierId() is { } unknown)
            {
                Tracker.TrackNotFound($"SpawnNpc action '{npcId}': modifier '{unknown}'", this);
                return;
            }

            if (ResolvePosition() is not { } position) return;

            Spawn(position);
        }

        /// <summary>Guarded like every other spawn site in the project: rolling a definition and
        /// building the node still throw past the id checks (a stance with no behavior archetype,
        /// a scene that fails to load). Neither the quest log nor the dialogue service catches, so
        /// an escaping exception would abandon the remaining actions of the same entry and leave
        /// the stage half applied.</summary>
        private void Spawn(Vector2 position)
        {
            try
            {
                var definition = npcs.CreateDefinition(npcId);
                if (modifierIds != null)
                    definition = definition with { Modifiers = modifierIds.Select(modifierProvider.GetModifier).ToList() };

                if (spawner.Spawn(definition, position) is not { } spawned)
                {
                    Tracker.TrackError($"SpawnNpc action '{npcId}': no world to spawn into", this);
                    return;
                }

                // The point lent coordinates and nothing else: nobody re-rolls this NPC on load, so
                // the save file is the only thing that can bring the trial target back.
                spawned.MarkAsWild();

                // Raids and bosses reserve the same way: a named target must never be lost to a full
                // world, so it takes a slot outside the limit and releases it on final death — regular
                // spawn points simply pause until the world thins out again.
                population.ReserveOutsideLimit();
            }
            catch (Exception exception)
            {
                Tracker.TrackException($"SpawnNpc action '{npcId}': spawn failed", exception, this);
            }
        }

        /// <summary>The first named modifier the catalog does not know, or null when all are known.</summary>
        private string? UnknownModifierId()
        {
            if (modifierIds == null) return null;

            var known = modifierProvider.GetAllModifierIds();
            return modifierIds.FirstOrDefault(id => !known.Contains(id));
        }

        /// <summary>Where the named point stands. Points carry their world position as raid spawn
        /// sites; one that does not (a point outside that contract) cannot be addressed by data.
        /// A miss names every point the scene did register: the id is authored by hand in two places
        /// at once, and a singular against a plural is the whole of the mistake.</summary>
        private Vector2? ResolvePosition()
        {
            var point = points.All.FirstOrDefault(entry => entry.PointId == pointId);
            if (point == null)
            {
                Tracker.TrackNotFound($"SpawnNpc action '{npcId}': spawn point '{pointId}' ({KnownPointIds()})", this);
                return null;
            }

            if (point is not IRaidSpawnSite site)
            {
                Tracker.TrackError($"SpawnNpc action '{npcId}': spawn point '{pointId}' reports no world position", this);
                return null;
            }

            return site.Position;
        }

        /// <summary>The ids the scene registered, for the refusal above — parenthesised there, because
        /// the tracker closes every miss with "not found" and a bare list would hand that negation to
        /// its last id. Sorted, because the answer is read by eye against an id typed in a quest file;
        /// an empty world says so in words rather than leaving the reader to wonder whether the list was
        /// simply left off.</summary>
        private string KnownPointIds()
        {
            string[] ids = [.. points.All.Select(entry => entry.PointId).OrderBy(id => id, StringComparer.Ordinal)];

            return $"known points: {(ids.Length == 0 ? NoPointsRegistered : string.Join(", ", ids))}";
        }
    }

    public class SpawnNpcActionFactory(
        INpcProvider npcs,
        INpcModifierProvider modifierProvider,
        INpcWorldSpawner spawner,
        INpcPopulationService population,
        ISpawnPointRegistry points) : INarrativeActionFactory
    {
        private const string TypeName = "SpawnNpc";
        private const string NpcIdKey = "npcId";
        private const string PointIdKey = "pointId";
        private const string ModifiersKey = "modifiers";

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Text(NpcIdKey, required: true, DataCatalog.Npc),
            NarrativeParameterSchema.FreeText(PointIdKey, required: true),
            NarrativeParameterSchema.References(ModifiersKey, DataCatalog.NpcModifiers));

        public string Type => TypeName;

        /// <summary>The point is named by a scene's own spawn point and no catalog holds those ids. The
        /// modifiers list carries no default: absent leaves the record's own roll standing, while the
        /// property present names the exact set, an empty array included.</summary>
        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string npcId = json.Value<string>(NpcIdKey) ?? string.Empty;
            if (npcId.Length == 0)
            {
                Tracker.TrackError($"{TypeName} action: {NpcIdKey} is missing");
                return null;
            }

            string pointId = json.Value<string>(PointIdKey) ?? string.Empty;
            if (pointId.Length == 0)
            {
                Tracker.TrackError($"{TypeName} action '{npcId}': {PointIdKey} is missing");
                return null;
            }

            var modifierIds = ReadModifierIds(json);
            if (modifierIds != null && modifierIds.Any(id => id.Length == 0))
            {
                Tracker.TrackError($"{TypeName} action '{npcId}': the {ModifiersKey} list holds an empty id");
                return null;
            }

            return new SpawnNpcAction(npcs, modifierProvider, spawner, population, points, npcId, pointId, modifierIds);
        }

        /// <summary>No "modifiers" property = the record's own roll stands. The property present =
        /// the EXACT set the entry names, an empty array included: a bare target is an authored
        /// answer (the weakest rung of a trial ladder), not a missing one.</summary>
        private static IReadOnlyList<string>? ReadModifierIds(JObject json) =>
            json[ModifiersKey] is not JArray array
                ? null
                : array.Select(token => token.Value<string>() ?? string.Empty).ToList();
    }
}
