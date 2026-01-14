namespace Core.Interfaces.Events.GameEvents
{
    using Entity;
    using System.Collections.Generic;

    public record BattleStartEvent(IEntity Player, List<IEntity> Entities) : IGameEvent;
}
