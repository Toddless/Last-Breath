namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Events;

    public class CombatEventBus : ICombatEventBus
    {
        private readonly EventRegistry<ICombatEvent> _registry = new();
        private readonly List<Action<ICombatEvent>> _catchAllHandlers = [];

        public void Publish<T>(T evnt)
            where T : ICombatEvent
        {
            // Catch-all runs BEFORE typed handlers so recorders capture the event before reactions cascade.
            EventDispatch.Dispatch<ICombatEvent>(_catchAllHandlers, evnt, this);
            _registry.Publish(evnt, this);
        }

        public void Subscribe<T>(Action<T> handler) where T : ICombatEvent => _registry.Add(handler);

        public void Unsubscribe<T>(Action<T> handler) where T : ICombatEvent => _registry.Remove(handler);

        public void SubscribeAll(Action<ICombatEvent> handler) => _catchAllHandlers.Add(handler);

        public void UnsubscribeAll(Action<ICombatEvent> handler) => _catchAllHandlers.Remove(handler);

        public void Dispose()
        {
            _registry.Clear();
            _catchAllHandlers.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
