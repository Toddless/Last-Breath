namespace Battle.Source
{
    using System;
    using Core.Events;

    public class BattleEventBus : IBattleEventBus
    {
        private readonly EventRegistry<IBattleEvent> _registry = new();

        public void Publish<T>(T evnt) where T : IBattleEvent => _registry.Publish(evnt, this);

        public void Subscribe<T>(Action<T> handler) where T : IBattleEvent => _registry.Add(handler);

        public void Unsubscribe<T>(Action<T> handler) where T : IBattleEvent => _registry.Remove(handler);

        public void Dispose()
        {
            _registry.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
