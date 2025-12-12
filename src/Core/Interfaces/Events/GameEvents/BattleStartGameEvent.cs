namespace Core.Interfaces.Events.GameEvents
{
    using Entity;
    using Interfaces;
    using System.Collections.Generic;

    public record BattleStartGameEvent(IEntity Player, List<IEntity> Entities) : IGameEvent
    {

    }
}
