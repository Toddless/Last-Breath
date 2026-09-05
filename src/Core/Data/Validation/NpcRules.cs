namespace Core.Data.Validation
{
    using System;
    using System.Collections.Generic;
    using GameData;
    using NpcData;

    /// <summary>
    /// What an npc record has to say about itself beyond its shape. The one fact of the kind today: a
    /// fighter has to fight in some stance, and which one is either named in the authored section or
    /// rolled from the list of them.
    /// <para>A record that writes neither is not refused by anything — the provider falls back to
    /// Dexterity and the npc spawns fighting a style nobody chose for it, which is the sort of thing a
    /// shape rule cannot see and a play-through would never point at.</para>
    /// </summary>
    public static class NpcRules
    {
        private const string NoStanceFormat =
            "'{0}' names no stance and writes no '{1}' to roll one from: the spawn falls back to the "
            + "default stance without a word, and the behaviour archetype follows it.";

        /// <summary>The json name of the list a stance is rolled from — what
        /// <see cref="NpcData.Stances"/> is written as.</summary>
        private const string StancesKey = "stances";

        /// <summary>What every npc record of a run comes to, in the order they are written.</summary>
        public static IReadOnlyList<DataFinding> Check(IReadOnlyList<NpcData> npcs)
        {
            ArgumentNullException.ThrowIfNull(npcs);

            List<DataFinding> found = [];

            foreach (NpcData npc in npcs)
                if (Stanceless(npc))
                    found.Add(new DataFinding(
                        DataFindingKind.EmptyRequiredList,
                        DataCatalog.Npc,
                        npc.Id,
                        StancesKey,
                        Where: string.Empty,
                        string.Format(NoStanceFormat, npc.Id, StancesKey)));

            return found;
        }

        /// <summary>Whether nothing in the record says which stance the npc fights in. An authored stance
        /// answers it whole — such a record ignores the list entirely and is expected to leave it empty —
        /// and a list written as null is a key json takes and hands the reader nothing for.</summary>
        private static bool Stanceless(NpcData npc) =>
            npc.Authored?.Stance is not { Length: > 0 } && (npc.Stances is null || npc.Stances.Count == 0);
    }
}
