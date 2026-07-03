namespace Core.Interfaces.Battle
{
    using System;
    using System.Collections.Generic;
    using Data;

    /// <summary>
    /// The single ordered stream of everything that happened in a battle.
    /// Logic resolves instantly and only appends here; the presentation layer replays
    /// entries with its own timings. Order of entries = causal order of events.
    /// </summary>
    public interface IBattleTimeline
    {
        IReadOnlyList<TimelineEntry> Entries { get; }

        /// <summary>Fired synchronously for every appended entry — the live feed for the presentation player.</summary>
        event Action<TimelineEntry>? EntryRecorded;

        /// <summary>Manual append for battle-level events that never pass through an entity bus.</summary>
        void Record(object evnt);

        /// <summary>Starts recording every event the bus publishes. The timeline remembers the subscription.</summary>
        void Attach(ICombatEventBus bus);

        /// <summary>Unsubscribes from all attached buses; recorded entries are kept.</summary>
        void DetachAll();

        void Clear();
    }
}
