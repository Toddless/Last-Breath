namespace Core.Events.GameEvents
{
    using System.Collections.Generic;
    using Entity;

    public record BattleQueueDefinedEvent(List<IFightable> Entities) : IBattleEvent;
}
