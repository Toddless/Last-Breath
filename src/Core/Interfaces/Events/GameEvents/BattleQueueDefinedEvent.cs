namespace Core.Interfaces.Events.GameEvents
{
    using System.Collections.Generic;
    using Entity;

    public record BattleQueueDefinedEvent(List<IEntity> Entities) : IBattleEvent;
}
