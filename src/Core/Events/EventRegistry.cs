namespace Core.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Typed subscription store shared by the event buses (game/battle/combat). Handlers register under
    /// their concrete event type; publishing keys off the event's RUNTIME type (<c>evnt.GetType()</c>),
    /// so a publish made through a base-typed reference still reaches the concrete subscribers — the
    /// static type of the Publish argument no longer decides routing. Each handler is wrapped once, at
    /// subscribe time, in a closure that closes over its concrete <typeparamref name="T"/>, so delivery
    /// needs no reflection and no per-publish cast. Routing stays exact-match BY DESIGN: a subscriber of
    /// a base type does not receive derived events. Actual invocation goes through
    /// <see cref="EventDispatch"/>, so a throwing handler is isolated the same way here.
    /// </summary>
    public sealed class EventRegistry<TBase>
        where TBase : class
    {
        private readonly Dictionary<Type, List<Entry>> _handlers = new();

        private readonly record struct Entry(Delegate Original, Action<TBase> Invoke);

        public void Add<T>(Action<T> handler)
            where T : TBase
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
                _handlers[typeof(T)] = list = [];

            // The closure is only ever invoked for events whose runtime type is exactly T (publish
            // looks them up by GetType()), so the downcast can never fail.
            list.Add(new Entry(handler, evnt => handler((T)evnt)));
        }

        public void Remove<T>(Action<T> handler)
            where T : TBase
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                Tracker.TrackNotFound($"Handler for type of Action: {typeof(T)}", this);
                return;
            }

            list.RemoveAll(entry => entry.Original.Equals(handler));
            if (list.Count == 0) _handlers.Remove(typeof(T));
        }

        public void Publish(TBase evnt, object? source)
        {
            if (evnt is null || !_handlers.TryGetValue(evnt.GetType(), out var list))
                return;

            EventDispatch.Dispatch(list.Select(entry => entry.Invoke), evnt, source);
        }

        public void Clear() => _handlers.Clear();
    }
}
