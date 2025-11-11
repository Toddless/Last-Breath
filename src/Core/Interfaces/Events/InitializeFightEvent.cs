namespace Core.Interfaces.Events
{
    using System.Collections.Generic;
    using Core.Interfaces.Entity;

    public record InitializeFightEvent<T>(IEnumerable<T> Fighters) : IEvent
        where T : IFightable
    {
    }
}
