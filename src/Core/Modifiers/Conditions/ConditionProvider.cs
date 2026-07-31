namespace Core.Modifiers.Conditions
{
    using System;
    using System.Collections.Generic;
    using Data.GameData;
    using Interfaces;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Reads the Conditions catalog and keeps one original per id, handing out copies. Entry-level
    /// error policy, in the shape the rest of the data layer uses: a file that is not a catalog at all
    /// fails the file, everything smaller costs its own entry — a nameless or broken record is reported
    /// and skipped while the rest of the catalog loads.
    /// </summary>
    public class ConditionProvider(ConditionParser parser) : IConditionProvider, IGameDataParticipant
    {
        /// <summary>Ids are matched ignoring case, deliberately looser than the id registries next to it
        /// (item, quest, deed and passive node ids all compare ordinal). A condition id is hand-written
        /// twice — once as the entry, once in every line that names it — and the two sides are authored
        /// in different files by different tools. A miss here is not the cheap answer it is elsewhere:
        /// an unresolved id drops the whole line rather than leaving it unconditional. The same comparer
        /// settles the duplicate check below, so one id can never become two definitions.</summary>
        private readonly Dictionary<string, ICondition> _conditions = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> Catalogs => [DataCatalog.Conditions];

        public void Apply(string catalog, GameDataFile file)
        {
            var entries = JObject.Parse(file.Json)[ConditionFields.Entries] as JArray
                          ?? throw new InvalidOperationException(
                              $"'{file.FileName}' holds no '{ConditionFields.Entries}' array of condition definitions");

            foreach (var entry in entries)
                Read(entry, file.FileName);
        }

        public bool TryResolve(string? id, out ICondition? condition)
        {
            condition = null;
            if (string.IsNullOrWhiteSpace(id)) return true;

            if (!_conditions.TryGetValue(id, out var original))
            {
                Tracker.TrackError($"Condition '{id}' is in no {DataCatalog.Conditions} catalog — dropping the line that asks for it");
                return false;
            }

            condition = original.Copy();
            return true;
        }

        /// <summary>An id claimed twice is settled on the first claim: whoever asks for it gets one
        /// definition rather than whichever file happened to be read last.</summary>
        private void Read(JToken entry, string fileName)
        {
            if (entry is not JObject record)
            {
                Tracker.TrackError($"Skipping a condition of '{fileName}': an entry is a json object, got {entry.Type}");
                return;
            }

            string id = record.Value<string>(ConditionFields.Id) ?? string.Empty;
            if (id.Length == 0)
            {
                Tracker.TrackError($"Skipping a condition of '{fileName}': an entry without an '{ConditionFields.Id}' is reachable by nobody");
                return;
            }

            var condition = parser.Parse(record);
            if (condition == null) return;

            if (!_conditions.TryAdd(id, condition))
                Tracker.TrackError($"Condition '{id}' of '{fileName}' is defined more than once — keeping the definition read first");
        }
    }
}
