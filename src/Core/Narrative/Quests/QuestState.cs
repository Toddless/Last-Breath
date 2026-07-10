namespace Core.Narrative.Quests
{
    using System.Collections.Generic;

    /// <summary>
    /// Runtime state of one quest. Objective completion is NOT stored — it is always re-derived
    /// from facts and inventory (that is the retroactivity contract); only counter baselines and
    /// bookkeeping the world can't reproduce live here. Times are world-clock total minutes.
    /// </summary>
    public class QuestState(string questId)
    {
        public string QuestId { get; } = questId;
        public QuestStatus Status { get; set; } = QuestStatus.Active;
        public int StageIndex { get; set; }

        /// <summary>Fact snapshots for non-retroactive counters, keyed "stageIndex:factKey".</summary>
        public Dictionary<string, int> CounterBaselines { get; } = [];

        /// <summary>The one-candidate-left warning fired; never repeat it.</summary>
        public bool GhostHintShown { get; set; }

        /// <summary>World-clock minutes at accept; deadline = this + timeLimitHours × 60.</summary>
        public int AcceptedAtMinutes { get; set; }

        /// <summary>Declined quests may be offered again from this world-clock minute on.</summary>
        public int NextOfferAtMinutes { get; set; }
    }
}
