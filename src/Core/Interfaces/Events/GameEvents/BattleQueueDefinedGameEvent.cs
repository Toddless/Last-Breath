namespace Core.Interfaces.Events.GameEvents
{
    using System.Collections.Generic;
    using Entity;

    public record BattleQueueDefinedGameEvent(List<IEntity> Entities) : IBattleEvent;
}
