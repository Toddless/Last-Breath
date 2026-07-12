namespace LootGeneration.Internal
{
    using System;
    using Core.Events;

    internal class GameEventBus : IGameEventBus
    {
        private readonly EventRegistry<IGameEvent> _registry = new();

        public void Publish<T>(T evnt) where T : IGameEvent => _registry.Publish(evnt, this);

        public void Subscribe<T>(Action<T> handler) where T : IGameEvent => _registry.Add(handler);

        public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent => _registry.Remove(handler);
    }
}
