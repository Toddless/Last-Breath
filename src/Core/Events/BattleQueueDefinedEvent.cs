namespace Core.Events
{
    using System.Collections.Generic;
    using Entity;

    public record BattleQueueDefinedEvent(List<IFightable> Entities) : IBattleEvent;
}
