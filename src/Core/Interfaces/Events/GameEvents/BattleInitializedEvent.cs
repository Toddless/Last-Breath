namespace Core.Interfaces.Events.GameEvents
{
    using Entity;
    using System.Collections.Generic;

    public record BattleInitializedEvent(IEntity Player, List<IEntity> Entities) : IGameEvent;
}
