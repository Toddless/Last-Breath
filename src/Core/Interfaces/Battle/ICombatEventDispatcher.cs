namespace Core.Interfaces.Battle
{
    using System;

    public interface ICombatEventDispatcher
    {
        void Publish<T>(T evnt);
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
    }
}
