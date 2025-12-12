namespace Core.Interfaces
{
    using System;

    public interface IEventBus<in TEvent>
    {
        void Publish<T>(T evnt) where T : notnull, TEvent;
        void Subscribe<T>(Action<T> handler) where T : notnull, TEvent;
        void Unsubscribe<T>(Action<T> handler) where T : notnull, TEvent;
    }
}
