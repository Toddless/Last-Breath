namespace Core.Narrative.Facts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Flat registry of what this playthrough has already seen: discovered locations, kill
    /// counters, dialogue flags. Trackers write here unconditionally — quests and dialogues read
    /// facts as predicates, which is what lets an objective complete retroactively (a battlefield
    /// found BEFORE the quest asked for it still counts). A fact is an int: flags are 1, counters grow.
    /// </summary>
    public interface IWorldFactsService
    {
        event Action<string, int>? FactChanged;

        IReadOnlyDictionary<string, int> Snapshot { get; }

        bool IsSet(string key);
        int GetCount(string key);

        /// <summary>Raises the flag to 1; idempotent — an already-set fact does not fire FactChanged.</summary>
        void SetFact(string key);

        void Add(string key, int amount = 1);

        /// <summary>Overwrites the value outright — for facts that store a quantity (timestamps,
        /// cached roll outcomes), not a history.</summary>
        void SetCount(string key, int value);

        /// <summary>Load-time restore: replaces the whole registry without firing FactChanged.</summary>
        void RestoreState(IReadOnlyDictionary<string, int> facts);
    }
}
