namespace Core.Interfaces.Events
{
    using System;

    public interface IBattleEventBus : IEventBus<IBattleEvent>, IDisposable
    {
    }
}
