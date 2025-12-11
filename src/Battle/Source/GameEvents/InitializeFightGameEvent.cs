namespace Battle.Source.GameEvents
{
    using Core.Interfaces;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;

    public record InitializeFightGameEvent(IEntity Player, List<IEntity> Entities) : IGameEvent
    {

    }
}
