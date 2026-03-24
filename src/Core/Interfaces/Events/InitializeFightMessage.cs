namespace Core.Interfaces.Events
{
    using System.Collections.Generic;
    using Entity;

    public record InitializeFightMessage<T>(IEntity Player, IEnumerable<T> Fighters) : IMessage
        where T : IFightable
    {
    }
}
