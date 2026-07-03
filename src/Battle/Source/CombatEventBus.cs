namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Battle;
    using Utilities;

    public class CombatEventBus : ICombatEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly List<Action<ICombatEvent>> _catchAllHandlers = [];

        public void Publish<T>(T evnt)
            where T : ICombatEvent
        {
            NotifyCatchAll(evnt);
            NotifyTyped(evnt);
        }

        public void Subscribe<T>(Action<T> handler)
            where T : ICombatEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = [];
                _handlers[typeof(T)] = handlers;
            }

            handlers.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
            where T : ICombatEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
            {
                Tracker.TrackNotFound($"Handler for type of Action: {typeof(T)}", this);
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
                _handlers.Remove(typeof(T));
        }

        public void SubscribeAll(Action<ICombatEvent> handler) => _catchAllHandlers.Add(handler);

        public void UnsubscribeAll(Action<ICombatEvent> handler) => _catchAllHandlers.Remove(handler);

        public void Dispose()
        {
            _handlers.Clear();
            _catchAllHandlers.Clear();
            GC.SuppressFinalize(this);
        }

        /// <summary>Runs before typed handlers so recorders capture the event before reactions cascade.</summary>
        private void NotifyCatchAll(ICombatEvent evnt)
        {
            foreach (var handler in _catchAllHandlers.ToList())
                handler(evnt);
        }

        private void NotifyTyped<T>(T evnt)
            where T : ICombatEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
                return;

            foreach (var handler in handlers.Cast<Action<T>>().ToList())
                handler(evnt);
        }
    }
}
