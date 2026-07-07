namespace Core.Events
{
    using System.Collections.Generic;
    using Entity;

    public record InitializeFightMessage<T>(IFightable Player, IEnumerable<T> Fighters) : IMessage
        where T : IFightable
    {
    }
}
