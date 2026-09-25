namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Data;

    public class BattleTimeline : IBattleTimeline
    {
        private readonly List<TimelineEntry> _entries = [];
        private readonly List<ICombatEventBus> _attachedBuses = [];
        private int _nextSequence;

        public IReadOnlyList<TimelineEntry> Entries => _entries;

        public event Action<TimelineEntry>? EntryRecorded;

        public void Record(object evnt)
        {
            var entry = new TimelineEntry(_nextSequence++, evnt);
            _entries.Add(entry);
            EntryRecorded?.Invoke(entry);
        }

        public void Attach(ICombatEventBus bus)
        {
            if (_attachedBuses.Contains(bus)) return;

            bus.SubscribeAll(OnCombatEvent);
            _attachedBuses.Add(bus);
        }

        public void DetachAll()
        {
            foreach (var bus in _attachedBuses)
                bus.UnsubscribeAll(OnCombatEvent);

            _attachedBuses.Clear();
        }

        public void Clear()
        {
            _entries.Clear();
            _nextSequence = 0;
        }

        private void OnCombatEvent(ICombatEvent evnt) => Record(evnt);
    }
}
