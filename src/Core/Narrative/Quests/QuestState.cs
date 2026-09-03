namespace Core.Narrative.Quests
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Runtime state of one quest. Objective completion is NOT stored — it is always re-derived
    /// from facts and inventory (that is the retroactivity contract); only counter baselines and
    /// bookkeeping the world can't reproduce live here. Times are world-clock total minutes.
    /// </summary>
    public class QuestState(string questId)
    {
        /// <summary>Separates the stage from the fact key in a baseline key; a save written before
        /// stages had ids puts the stage position in front of it.</summary>
        public const char BaselineSeparator = ':';

        public string QuestId { get; } = questId;
        public QuestStatus Status { get; set; } = QuestStatus.Active;

        /// <summary>Stage the quest stands on, by id — branching makes the position in the list
        /// meaningless. Empty reads as "the first stage".</summary>
        public string StageId { get; set; } = string.Empty;

        /// <summary>Name of the ending the quest reached; null while it is still walking its stages.</summary>
        public string? OutcomeId { get; set; }

        /// <summary>Fact snapshots for non-retroactive counters, keyed by <see cref="BaselineKey"/>.</summary>
        public Dictionary<string, int> CounterBaselines { get; } = [];

        /// <summary>The one-candidate-left warning fired; never repeat it.</summary>
        public bool GhostHintShown { get; set; }

        /// <summary>One-of-a-kind reward items this quest has already handed over. Belongs to the QUEST
        /// and not to the attempt: a repeatable quest pays everything repeatable on every turn-in, and
        /// an artefact exactly once, so a fresh attempt inherits this list.</summary>
        public HashSet<string> GrantedUniqueRewards { get; } = new(StringComparer.Ordinal);

        /// <summary>World-clock minutes at accept; deadline = this + timeLimitHours × 60.</summary>
        public int AcceptedAtMinutes { get; set; }

        /// <summary>Declined quests may be offered again from this world-clock minute on.</summary>
        public int NextOfferAtMinutes { get; set; }

        /// <summary>Key of one counter snapshot: the stage owns what it saw when the player entered it.</summary>
        public static string BaselineKey(string stageId, string factKey) => $"{stageId}{BaselineSeparator}{factKey}";
    }
}
