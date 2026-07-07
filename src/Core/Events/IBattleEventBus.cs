namespace Core.Events
{
    using System;
    using Interfaces;

    public interface IBattleEventBus : IEventBus<IBattleEvent>, IDisposable
    {
    }
}
