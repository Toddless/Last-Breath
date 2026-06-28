namespace Core.Interfaces.Events.GameEvents
{
    using System.Collections.Generic;
    using Entity;

    public record BattleInitializedEvent(IEntity Player, List<IEntity> Entities) : IGameEvent;
}
