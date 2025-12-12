namespace Battle.Services
{
    using System;
    using System.Linq;
    using Core.Interfaces;
    using System.Collections.Generic;

    internal class GameEventBus : IGameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public void Publish<T>(T evnt)
            where T : IGameEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
                return;

            foreach (var handler in handlers.Cast<Action<T>>())
                handler(evnt);
        }

        public void Subscribe<T>(Action<T> handler)
            where T : IGameEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = [];
                _handlers[typeof(T)] = handlers;
            }

            handlers.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
            where T : IGameEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
            {
                // TODO: Tracker
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
                _handlers.Remove(typeof(T));
        }
    }
}
