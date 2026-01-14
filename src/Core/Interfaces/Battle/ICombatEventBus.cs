namespace Core.Interfaces.Battle
{
    using System;

    public interface ICombatEventBus : IEventBus<ICombatEvent>, IDisposable
    {

    }
}
