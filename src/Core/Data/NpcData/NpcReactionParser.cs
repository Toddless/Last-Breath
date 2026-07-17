namespace Core.Data.NpcData
{
    using System;
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// Strict parse of the "reactions" section: enums via <see cref="EnumParser"/>, sane numbers.
    /// A broken entry is reported to the Tracker and skipped — one typo must not silence the
    /// whole NPC, and a half-parsed reaction must never fire.
    /// </summary>
    public static class NpcReactionParser
    {
        public static List<NpcReactionConfig> Parse(string npcId, IReadOnlyList<NpcReactionData> entries)
        {
            List<NpcReactionConfig> parsed = [];
            foreach (var entry in entries)
            {
                try
                {
                    parsed.Add(ParseEntry(entry));
                }
                catch (Exception e)
                {
                    Tracker.TrackException($"Broken reaction '{entry.AbilityId}' of npc '{npcId}' skipped", e);
                }
            }

            return parsed;
        }

        private static NpcReactionConfig ParseEntry(NpcReactionData entry)
        {
            if (string.IsNullOrEmpty(entry.AbilityId)) throw new FormatException("reaction has no abilityId");
            if (entry.Chance is <= 0f or > 1f) throw new FormatException($"chance {entry.Chance} is outside (0..1]");
            if (entry.MaxPerTurn <= 0) throw new FormatException($"maxPerTurn {entry.MaxPerTurn} must be positive");

            return new NpcReactionConfig(
                entry.AbilityId,
                EnumParser.ParseEnum<ReactionTrigger>(entry.Trigger),
                entry.Chance,
                entry.MaxPerTurn,
                string.IsNullOrEmpty(entry.BlockedByFinalDeathOf) ? null : entry.BlockedByFinalDeathOf);
        }
    }
}
