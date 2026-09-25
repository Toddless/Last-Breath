namespace Core.Data.NpcModifiersData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Newtonsoft.Json;
    using NpcSpawnRollsData;

    /// <summary>
    /// Parses NpcModifiers.json into typed data grouped by modifier kind. Lives in Core so every
    /// project shares one parser (moved out of Utilities.DataParser — it needed no item factory).
    /// <para>How far "unique" reaches in a section is not written in this file: it is a rule about how a
    /// spawn is composed, and it stands with the rest of them in the spawn-rolls document. Reading it is
    /// here all the same, because this is the only place that knows which sections there are to name.</para>
    /// </summary>
    public static class NpcModifiersParser
    {
        /// <summary>The modifiers of one file, by the section that wrote them, each entry stamped with the
        /// reach its section's uniqueness has. A section the scopes do not name reaches the whole group,
        /// which is what most sections mean.</summary>
        public static Dictionary<string, List<NpcModifierData>> Parse(
            string json, IReadOnlyDictionary<string, NpcUniqueScope> scopes)
        {
            ArgumentNullException.ThrowIfNull(scopes);

            var data = JsonConvert.DeserializeObject<ModifiersData>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC modifiers");

            return data.Sections().ToDictionary(
                section => section.Key,
                section => Scoped(section.Value, scopes.GetValueOrDefault(section.Key)),
                StringComparer.Ordinal);
        }

        /// <summary>How far "unique" reaches in each section, read off the spawn-rolls document. A section
        /// no modifier file holds is refused rather than passed over: a rule written for a name nobody
        /// answers to is a rule that never fires, and the misspelling would be invisible.</summary>
        public static IReadOnlyDictionary<string, NpcUniqueScope> Scopes(string spawnRollsJson)
        {
            var data = JsonConvert.DeserializeObject<SpawnRollsData>(spawnRollsJson)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC spawn roll data");

            Dictionary<string, NpcUniqueScope> scopes = new(StringComparer.Ordinal);

            foreach ((string section, NpcUniqueScope scope) in data.UniqueScope)
            {
                if (!ModifiersData.SectionKeys.Contains(section))
                    throw new FormatException(
                        $"The uniqueScope map names '{section}', which is no section of the NPC modifier catalog "
                        + $"({string.Join(", ", ModifiersData.SectionKeys)}).");

                scopes[section] = scope;
            }

            return scopes;
        }

        private static List<NpcModifierData> Scoped(List<NpcModifierData> entries, NpcUniqueScope scope) =>
            [.. entries.Select(entry => entry with { UniqueScope = scope })];
    }
}
